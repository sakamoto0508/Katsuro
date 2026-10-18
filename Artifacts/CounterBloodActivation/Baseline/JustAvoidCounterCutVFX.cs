using UnityEngine;

/// <summary>追撃各段の接触光・切断線・方向血。段ごとのPrefabを固定プールから再生する。</summary>
[DisallowMultipleComponent]
public sealed class JustAvoidCounterCutVFX : MonoBehaviour
{
    private static readonly int AgeId = Shader.PropertyToID("_Age");
    [SerializeField] private MeshRenderer _cutRenderer;
    [SerializeField, Tooltip("刀軌跡Slashを主役にし、このPrefabの旧Cut Quadを非表示にする。Bloodは維持し、既存ContactPulseで局所光を再生します。")]
    private bool _useLocalContactFlash;
    public bool UsesLocalContactFlash => _useLocalContactFlash;
    [SerializeField] private ParticleSystem[] _blood;
    [SerializeField, Min(.01f), Tooltip("プールを返却するまでの秒数。Bloodの寿命も含むため、Cutだけの減衰は専用Materialで調整します。")]
    private float _duration = .38f;
    [Header("Cut footprint (each strike prefab)")]
    [SerializeField, Tooltip("X: Cutの最大長さ(m)。Y: 発光を収める面の高さ(m)。芯の太さはMaterialのCore widthで調整します。")]
    private Vector2 _cutSize = new Vector2(1.45f, .44f);
    [SerializeField, Range(0f, 1f), Tooltip("奥行き方向の斬りでもCutの投影長が潰れない下限。血とSwordTrailの軌道は変えない。")]
    private float _minimumProjectedLength = .7f;
    [SerializeField, Range(0f, .06f), Tooltip("HitPointから視線側へのCut専用の微小オフセット(m)。深度LEqualで面の干渉を緩和します。大きいと浮くため最小限にします。Bloodとダメージ座標は動かしません。")]
    private float _surfaceOffset = .025f;
    [SerializeField, Range(0f, .5f), Tooltip("血の進行軸だけを攻撃者から離れる側へ少量傾けます。斬撃方向に直交する成分を使用。HitPoint・旧Cut表示方向は変更しません。")]
    private float _bloodOutwardBias;
    private MaterialPropertyBlock _block;
    private float _started;
    private bool _playing;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private JustAvoidCounterConfig _debugConfig;
    private JustAvoidCounterStrike _debugStrike;
    private int _debugSample;
#endif

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void ConfigureDebug(JustAvoidCounterConfig config, JustAvoidCounterStrike strike)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _debugConfig = config;
        _debugStrike = strike;
#endif
    }

    // Allocate with the pool instance, rather than on its first accepted hit.
    private void Awake()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
    }

    public void Play(Vector3 point, Vector3 slash, Vector3 normal, Camera viewingCamera = null)
    {
        Stop();
        if (_block == null) _block = new MaterialPropertyBlock();
        if (slash.sqrMagnitude < .0001f) slash = normal.sqrMagnitude > .0001f ? normal : Vector3.forward;
        slash.Normalize();
        Vector3 bloodDirection = ResolveBloodDirection(slash, normal, _bloodOutwardBias);
        Vector3 bloodUp = Mathf.Abs(Vector3.Dot(bloodDirection, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
        transform.SetPositionAndRotation(point, Quaternion.LookRotation(bloodDirection, bloodUp));
        // Blood follows the physical blade direction. The fine light faces the viewing camera.
        var camera = viewingCamera != null ? viewingCamera : Camera.main;
        Vector3 facing = camera != null ? (camera.transform.position - point).normalized : -normal.normalized;
        if (facing.sqrMagnitude < .0001f) facing = Vector3.back;
        // Cut is a contact accent, not the world-space blade trail. Retain the projected
        // sweep direction while giving depthward cuts a readable, bounded screen-plane length.
        Vector3 projected = Vector3.ProjectOnPlane(slash, facing);
        float projection = projected.magnitude;
        if (projection < .001f) projected = Vector3.ProjectOnPlane(normal, facing);
        if (projected.sqrMagnitude < .000001f) projected = camera != null ? camera.transform.right : Vector3.Cross(facing, Vector3.up);
        if (projected.sqrMagnitude < .000001f) projected = Vector3.right;
        Vector3 right = projected.normalized;
        _cutRenderer.transform.SetPositionAndRotation(point + facing * _surfaceOffset, Quaternion.LookRotation(facing, Vector3.Cross(facing, right)));
        _cutRenderer.transform.localScale = new Vector3(_cutSize.x * Mathf.Max(_minimumProjectedLength, projection), _cutSize.y, 1f);
        _started = FeedbackVisualTime.Now;
        _playing = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _debugSample = 0;
        _cutRenderer.forceRenderingOff = _debugConfig != null && _debugConfig.IsBloodOnlyDebug;
#endif
        gameObject.SetActive(true);
        _cutRenderer.enabled = !_useLocalContactFlash;
        foreach (var ps in _blood) ps.Play(false);
        LogBloodSnapshot("Play");
        Tick();
    }

    public void Tick()
    {
        if (!_playing) return;
        float age = FeedbackVisualTime.Now - _started;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool bloodOnly = _debugConfig != null && _debugConfig.IsBloodOnlyDebug;
        _cutRenderer.forceRenderingOff = bloodOnly;
        if (bloodOnly && _debugSample < 2 && age >= (_debugSample == 0 ? .05f : .18f))
        {
            _debugSample++;
            LogBloodSnapshot(_debugSample == 1 ? "After50ms" : "After180ms");
        }
#endif
        if (age >= _duration) { Stop(); return; }
        _block.SetFloat(AgeId, age);
        _cutRenderer.SetPropertyBlock(_block);
        foreach (var ps in _blood)
        {
            if (FeedbackVisualTime.IsPaused && ps.isPlaying) ps.Pause(false);
            else if (!FeedbackVisualTime.IsPaused && ps.isPaused) ps.Play(false);
        }
    }

    public void Stop()
    {
        _playing = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_cutRenderer != null) _cutRenderer.forceRenderingOff = false;
        _debugSample = 2;
#endif
        if (_blood != null) foreach (var ps in _blood) if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogBloodSnapshot(string phase)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_debugConfig == null || !_debugConfig.IsBloodOnlyDebug) return;
        // Allocations and renderer lookups are restricted to three snapshots per diagnostic hit.
        var text = new System.Text.StringBuilder(1024);
        var camera = Camera.main;
        text.Append($"[CounterBlood] {phase} stage={_debugStrike} instance={name}#{GetInstanceID()} age={FeedbackVisualTime.Now - _started:F3}s position={transform.position:F3} direction={transform.forward:F3}");
        if (camera != null)
            text.Append($" camera={camera.name} viewport={camera.WorldToViewportPoint(transform.position):F3} cameraDistance={Vector3.Distance(camera.transform.position, transform.position):F2}");
        else text.Append(" camera=NULL");
        if (_blood == null) text.Append("\nBlood array=NULL");
        else foreach (var ps in _blood)
        {
            if (ps == null) { text.Append("\nParticleSystem=NULL"); continue; }
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            var shader = material != null ? material.shader : null;
            text.Append($"\n{ps.name}: isPlaying={ps.isPlaying} isPaused={ps.isPaused} particleCount={ps.particleCount} active={ps.gameObject.activeInHierarchy}");
            text.Append($" renderer={(renderer != null ? renderer.enabled.ToString() : "NULL")} forceOff={(renderer != null && renderer.forceRenderingOff)} material={(material != null ? material.name : "NULL")} shader={(shader != null ? shader.name : "NULL")} supported={(shader != null && shader.isSupported)}");
            if (camera != null) text.Append($" cameraLayerVisible={((camera.cullingMask & (1 << ps.gameObject.layer)) != 0)}");
        }
        Debug.Log(text.ToString(), this);
#endif
    }

    // The accepted player counter HitNormal points from the attacker to contact.
    // Add only its cross-sweep component; never reverse the measured slash or move the origin.
    public static Vector3 ResolveBloodDirection(Vector3 slash, Vector3 awayFromAttacker, float bias)
    {
        if (slash.sqrMagnitude < .0001f) return awayFromAttacker.sqrMagnitude > .0001f ? awayFromAttacker.normalized : Vector3.forward;
        slash.Normalize();
        Vector3 outward = Vector3.ProjectOnPlane(awayFromAttacker.normalized, slash);
        return (slash + outward * Mathf.Clamp(bias, 0f, .5f)).normalized;
    }
}
