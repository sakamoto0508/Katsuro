using UnityEngine;
using Unity.Cinemachine;

/// <summary>勝利中だけCinemachineのOverrideを所有し、納刀が見える斜め側面へ実時間で構図を移す。</summary>
[DisallowMultipleComponent]
public sealed class VictoryCameraRig : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField, Min(0)] private float _moveDuration = .55f;
    [SerializeField, Range(45, 110)] private float _sideAngle = 85;
    [SerializeField, Min(.1f)] private float _lookHeight = 1.15f;
    [SerializeField, Range(-.5f, .5f)] private float _horizontalFraming = .18f;
    [SerializeField] private LayerMask _obstacles = ~0;
    private Transform _player;
    private Camera _output;
    private CinemachineBrain _brain;
    private bool _brainWasEnabled, _playing;
    private int _overrideId = -1;
    private float _started, _fov;
    private Vector3 _startPosition, _targetPosition, _lookTarget;
    private Quaternion _startRotation, _targetRotation;
    /// <summary>撮影対象と出力カメラを保持する。通常カメラの優先度や追従設定は変更しない。</summary>
    public void Init(Transform player, Camera output) { _player = player; _output = output; _brain = output != null ? output.GetComponent<CinemachineBrain>() : null; }
    /// <summary>現在の出力構図を起点に勝利用カメラへ制御を渡す。Lock-OnがBrainを停止していても一時的に有効にする。</summary>
    /// <param name="delay">構図の移行を始めるまでの実時間秒数。</param>
    public void Begin(float delay)
    {
        if (_playing || !isActiveAndEnabled || _player == null || _output == null || _brain == null || _camera == null) return;
        _brainWasEnabled = _brain.enabled; _startPosition = _output.transform.position; _startRotation = _output.transform.rotation; _fov = _output.fieldOfView;
        _lookTarget = _player.position + Vector3.up * _lookHeight + _player.right * _horizontalFraming;
        float distance = Mathf.Clamp(Vector3.ProjectOnPlane(_startPosition - _player.position, Vector3.up).magnitude, 2.5f, 4.5f);
        Vector3 side = Quaternion.AngleAxis(-_sideAngle, Vector3.up) * -_player.forward;
        _targetPosition = _lookTarget + side * distance + Vector3.up * .35f;
        _targetPosition = AvoidObstacles(_targetPosition);
        _targetRotation = Quaternion.LookRotation(_lookTarget - _targetPosition, Vector3.up);
        _started = Time.unscaledTime + Mathf.Max(0, delay); _playing = true;
        _camera.transform.SetPositionAndRotation(_startPosition, _startRotation);
        var lens = _camera.Lens; lens.FieldOfView = _fov; _camera.Lens = lens;
        _camera.enabled = true;
        _overrideId = _brain.SetCameraOverride(-1, 100, null, _camera, 1, Time.unscaledDeltaTime);
        _brain.enabled = true;
    }
    /// <summary>背景の壁・地形より手前に撮影位置を制限する。キャラクター自身とTriggerは障害物として扱わない。</summary>
    private Vector3 AvoidObstacles(Vector3 desired)
    {
        Vector3 delta = desired - _lookTarget; float distance = delta.magnitude;
        foreach (var hit in Physics.SphereCastAll(_lookTarget, .15f, delta.normalized, distance, _obstacles, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PlayerController>() != null || hit.collider.GetComponentInParent<EnemyController>() != null) continue;
            distance = Mathf.Min(distance, Mathf.Max(.5f, hit.distance - .1f));
        }
        return _lookTarget + delta.normalized * distance;
    }
    /// <summary>開始時に確定した構図へ補間し、通常のCinemachine更新に勝利用仮想カメラを渡す。</summary>
    /// <param name="now">Time.unscaledTimeと同じ基準の実時刻。</param>
    public void Tick(float now)
    {
        if (!_playing || _camera == null || _brain == null) return;
        float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - _started) / Mathf.Max(.01f, _moveDuration)));
        Vector3 position = AvoidObstacles(_lookTarget + Vector3.Slerp(_startPosition - _lookTarget, _targetPosition - _lookTarget, t));
        _camera.transform.SetPositionAndRotation(position, Quaternion.Slerp(_startRotation, _targetRotation, t));
        _overrideId = _brain.SetCameraOverride(_overrideId, 100, null, _camera, 1, Time.unscaledDeltaTime);
    }
    /// <summary>Overrideを解放し、Brainの有効状態と通常カメラの制御権を復元する。</summary>
    public void Stop()
    {
        if (!_playing) return; _playing = false;
        if (_brain != null) { _brain.ReleaseCameraOverride(_overrideId); _brain.enabled = _brainWasEnabled; }
        _overrideId = -1; if (_camera != null) _camera.enabled = false;
        if (!_brainWasEnabled && _output != null) { _output.transform.SetPositionAndRotation(_startPosition, _startRotation); _output.fieldOfView = _fov; }
    }
    /// <summary>勝利Overrideを解放してBrainの有効状態を復元する。</summary>
    private void OnDisable() => Stop();
    /// <summary>所有する勝利Overrideを残さず通常カメラへ制御を返す。</summary>
    private void OnDestroy() => Stop();
}
