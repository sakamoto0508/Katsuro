using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>成功時だけHDRPの画面色を屈折させる。判定・スロー・カメラ制御とは独立。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(CameraManager))]
public sealed class JustAvoidScreenDistortion : MonoBehaviour
{
    [SerializeField] private Material _material;
    private CameraManager _manager;
    private DistortionPass _pass;
    private VFXConfig _config;
    private Vector3 _worldCenter, _fallbackCenter;
    private float _started;
    private bool _playing;

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

    private void Update()
    {
        if (_playing && (_config == null || !_config.ScreenDistortionEnable ||
            Time.unscaledTime - _started >= Mathf.Max(.01f, _config.DistortionDuration))) Stop();
    }

    private void Stop()
    {
        if (_playing && _pass != null)
            CustomPassVolume.UnregisterGlobalCustomPass(CustomPassInjectionPoint.AfterPostProcess, _pass);
        _playing = false;
    }

    private void OnDisable() { Stop(); _pass?.ReleaseResources(); }
    private void OnDestroy() { Stop(); _pass?.ReleaseResources(); }

    public static bool TryProjectCenter(Camera camera, Vector3 hitPoint, Vector3 bodyCenter, out Vector2 center)
    {
        center = default;
        return camera != null && (TryProject(camera, hitPoint, out center) || TryProject(camera, bodyCenter, out center));
    }

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

        public DistortionPass(JustAvoidScreenDistortion owner)
        {
            _owner = owner;
            name = "Just Avoid Screen Distortion";
            targetColorBuffer = TargetBuffer.None;
            targetDepthBuffer = TargetBuffer.None;
        }

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

        public void ReleaseResources() { _source?.Release(); _source = null; }
        protected override void Cleanup() => ReleaseResources();
    }
}
