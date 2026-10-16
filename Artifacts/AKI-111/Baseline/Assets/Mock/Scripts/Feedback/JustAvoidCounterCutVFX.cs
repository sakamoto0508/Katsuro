using UnityEngine;

/// <summary>追撃各段の接触光・切断線・方向血。段ごとのPrefabを固定プールから再生する。</summary>
[DisallowMultipleComponent]
public sealed class JustAvoidCounterCutVFX : MonoBehaviour
{
    [SerializeField] private MeshRenderer _cutRenderer;
    [SerializeField] private ParticleSystem[] _blood;
    [SerializeField, Min(.01f)] private float _duration = .38f;
    [SerializeField] private Vector2 _cutSize = new Vector2(1.45f, .44f);
    [SerializeField, Range(0f, 1f), Tooltip("奥行き方向の斬りでもCutの投影長が潰れない下限。血とSwordTrailの軌道は変えない。")]
    private float _minimumProjectedLength = .7f;
    [SerializeField, Range(0f, .06f), Tooltip("接触面との重なりを避けるCutだけの視線方向オフセット（m）。")]
    private float _surfaceOffset = .025f;
    private MaterialPropertyBlock _block;
    private float _started;
    private bool _playing;

    public void Play(Vector3 point, Vector3 slash, Vector3 normal, Camera viewingCamera = null)
    {
        Stop();
        if (_block == null) _block = new MaterialPropertyBlock();
        if (slash.sqrMagnitude < .0001f) slash = normal.sqrMagnitude > .0001f ? normal : Vector3.forward;
        slash.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(slash, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
        transform.SetPositionAndRotation(point, Quaternion.LookRotation(slash, up));
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
        gameObject.SetActive(true);
        foreach (var ps in _blood) ps.Play(false);
        Tick();
    }

    public void Tick()
    {
        if (!_playing) return;
        float age = FeedbackVisualTime.Now - _started;
        if (age >= _duration) { Stop(); return; }
        _block.SetFloat("_Age", age);
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
        if (_blood != null) foreach (var ps in _blood) if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }
}
