using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>成功時だけHDRPの画面色を屈折させる。判定・スロー・カメラ制御とは独立。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(CameraManager))]
public sealed class JustAvoidScreenDistortion : MonoBehaviour
{
    /// <summary>Just Avoid画面歪みのCustom Pass描画に使うMaterial参照。</summary>
    [UnityEngine.Tooltip("Just Avoid画面歪みのCustom Pass描画に使うMaterial参照。")]
    [SerializeField] private Material _material;
    private CameraManager _manager;
    private DistortionPass _pass;
    private VFXConfig _config;
    private Vector3 _worldCenter, _fallbackCenter;
    private float _started;
    private bool _playing;

    /// <summary>成功地点と設定を保持して実時間の歪みを開始し、AfterPostProcessのCustom Passを登録する。</summary>
    public void Play(Vector3 worldPosition, Vector3 bodyCenter, VFXConfig config)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _material == null ||
            config == null || !config.ScreenDistortionEnable) return;
        if (_manager == null) _manager = GetComponent<CameraManager>();
        _config = config;
        _worldCenter = worldPosition;
        _fallbackCenter = bodyCenter;
        _started = Time.unscaledTime;
        if (_pass == null) _pass = new DistortionPass(this);
        if (_playing) return; // 同じPassを再利用し、連続成功では時刻だけリセットする。
        _playing = true;
        CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.AfterPostProcess, _pass);
    }

    /// <summary>設定が無効化された場合や実時間の寿命終了時に歪みPassを解除する。</summary>
    private void Update()
    {
        if (_playing && (_config == null || !_config.ScreenDistortionEnable ||
            Time.unscaledTime - _started >= Mathf.Max(.01f, _config.DistortionDuration))) Stop();
    }

    /// <summary>自身が登録したGlobal Custom Passを解除し、歪みの再生状態を終了する。</summary>
    private void Stop()
    {
        if (_playing && _pass != null)
            CustomPassVolume.UnregisterGlobalCustomPass(CustomPassInjectionPoint.AfterPostProcess, _pass);
        _playing = false;
    }

    /// <summary>Global Custom Passを解除して画面複製用のRTリソースを解放する。</summary>
    private void OnDisable() { Stop(); _pass?.ReleaseResources(); }
    /// <summary>歪みPassと所有する描画リソースを破棄時に解放する。</summary>
    private void OnDestroy() { Stop(); _pass?.ReleaseResources(); }

    /// <summary>命中点を画面へ投影し、使用できなければ身体中心を代替として使用する。</summary>
    /// <returns>画面内の有効な中心を求められた場合はtrue。</returns>
    public static bool TryProjectCenter(Camera camera, Vector3 hitPoint, Vector3 bodyCenter, out Vector2 center)
    {
        center = default;
        return camera != null && (TryProject(camera, hitPoint, out center) || TryProject(camera, bodyCenter, out center));
    }

    /// <summary>有限なワールド座標をViewportへ投影し、手前や画面外の値を除外する。</summary>
    /// <returns>有効な画面内位置を求められた場合はtrue。</returns>
    private static bool TryProject(Camera camera, Vector3 world, out Vector2 center)
    {
        center = default;
        if (!float.IsFinite(world.x) || !float.IsFinite(world.y) || !float.IsFinite(world.z)) return false;
        var p = camera.WorldToViewportPoint(world);
        if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z) ||
            p.z <= camera.nearClipPlane || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return false;
        center = new Vector2(p.x, p.y);
        return true;
    }

    /// <summary>出力カメラのPost Process後の画面を別RTへ複製し、成功地点から広がる歪みを描画する。</summary>
    private sealed class DistortionPass : CustomPass
    {
        private readonly JustAvoidScreenDistortion _owner;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private RTHandle _source;
        private static readonly int Source = Shader.PropertyToID("_DistortionSource");
        private static readonly int SourceInfo = Shader.PropertyToID("_SourceInfo");
        private static readonly int CenterRadius = Shader.PropertyToID("_CenterRadius");
        private static readonly int Ring = Shader.PropertyToID("_Ring");
        private static readonly int NoiseTint = Shader.PropertyToID("_NoiseTint");
        private static readonly int Tint = Shader.PropertyToID("_EdgeTint");
        protected override bool executeInSceneView => false;

        /// <summary>歪みの所有者と描画バッファ設定を保持し、HDRPへの独立したPassを準備する。</summary>
        public DistortionPass(JustAvoidScreenDistortion owner)
        {
            _owner = owner;
            name = "Just Avoid Screen Distortion";
            targetColorBuffer = TargetBuffer.None;
            targetDepthBuffer = TargetBuffer.None;
        }

        /// <summary>対象カメラの画面を別RTへコピーし、成功点から広がる輪状の歪みを実経過時間に応じて描く。</summary>
        protected override void Execute(CustomPassContext ctx)
        {
            if (_owner == null || !_owner._playing || !Application.isPlaying ||
                _owner._manager == null || ctx.hdCamera.camera != _owner._manager.OutputCamera) return;
            var config = _owner._config;
            float age = (Time.unscaledTime - _owner._started) / Mathf.Max(.01f, config.DistortionDuration);
            if (age < 0 || age >= 1 || !TryProjectCenter(ctx.hdCamera.camera,
                _owner._worldCenter, _owner._fallbackCenter, out var center)) return;
            // AfterPostProcessはTAA解決後。別RTへコピーし、同一RTの読み書きを避ける。
            if (_source == null)
                _source = RTHandles.Alloc(Vector2.one, slices: TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true,
                    name: "Just Avoid Distortion Source");
            var viewport = RTHandles.rtHandleProperties.currentViewportSize;
            float width = Mathf.Max(1, viewport.x), height = Mathf.Max(1, viewport.y);
            float aspect = width / height;
            float farX = Mathf.Max(center.x, 1 - center.x) * aspect;
            float farY = Mathf.Max(center.y, 1 - center.y);
            float maxRadius = Mathf.Max(config.DistortionMaxRadius,
                Mathf.Sqrt(farX * farX + farY * farY) + config.DistortionRingWidth * 2);
            float radius = maxRadius * (1 - Mathf.Pow(1 - age, config.DistortionRadiusSpeed));
            // 約0.035秒でピーク。その後鋭く減衰し、Slow/HitStopに引き延ばされない。
            float fade = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(Mathf.PI * Mathf.Pow(age, .5f))), config.DistortionFadePower);
            CustomPassUtils.Copy(ctx, ctx.cameraColorBuffer, _source);
            _block.SetTexture(Source, _source.rt);
            _block.SetVector(SourceInfo, new Vector4(width / _source.rt.width, height / _source.rt.height, 1 / width, 1 / height));
            _block.SetVector(CenterRadius, new Vector4(center.x, center.y, radius, aspect));
            _block.SetVector(Ring, new Vector4(config.DistortionRingWidth, config.DistortionStrength * fade, config.DistortionNoiseStrength, age));
            _block.SetVector(NoiseTint, new Vector4(config.DistortionNoiseScale, config.DistortionEdgeTintStrength * fade, 0, 0));
            _block.SetColor(Tint, config.DistortionEdgeTint);
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, _owner._material, _block, shaderPassId: 0);
        }

        /// <summary>Passが所有する画面複製用RTHandleを解放する。</summary>
        public void ReleaseResources() { _source?.Release(); _source = null; }
        /// <summary>HDRPがPassを片付ける際に所有するRTHandleを解放する。</summary>
        protected override void Cleanup() => ReleaseResources();
    }
}
