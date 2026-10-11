using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>低解像度の水面反射を一定頻度で更新し、視点の大きな変化には即座に追従する。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlanarReflectionProbe))]
[DefaultExecutionOrder(10000)]
public sealed class PlanarReflectionRefresh : MonoBehaviour
{
    /// <summary>更新要求を送る、このオブジェクトのPlanar Reflection Probe。</summary>
    [SerializeField, Tooltip("一定頻度で更新する水面のPlanar Reflection Probe。")]
    private PlanarReflectionProbe _probe;
    /// <summary>移動・回転・画角の変化を監視する戦闘用の出力Camera。</summary>
    [SerializeField, Tooltip("Cinemachineの出力先となるMain Camera。")]
    private Camera _viewerCamera;
    /// <summary>視点が安定している間の反射更新頻度の上限。</summary>
    [SerializeField, Range(1, 60), Tooltip("視点が安定している間の反射更新上限。水面自体のアニメーション速度は変更しません。")]
    private float _maximumUpdatesPerSecond = 20;
    /// <summary>前回の反射撮影位置からこの距離以上動いたら、更新間隔を待たず撮り直す。</summary>
    [SerializeField, Min(0.01f), Tooltip("この距離以上Cameraが移動した場合、更新上限を待たず反射を更新します（m）。")]
    private float _positionThreshold = 0.35f;
    /// <summary>前回の反射撮影方向からこの角度以上回転したら、即座に撮り直す。</summary>
    [SerializeField, Range(0.1f, 30), Tooltip("この角度以上Cameraが回転した場合、反射を即時更新します（度）。")]
    private float _rotationThreshold = 3;

    private ProbeSettings.RealtimeMode _originalMode;
    private bool _ownsMode;
    private double _nextRefresh;
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private float _lastFieldOfView, _lastAspect;

    /// <summary>元の更新方式を保持してOn Demandへ切り替え、初回の反射撮影を要求する。</summary>
    private void OnEnable()
    {
        if (_probe == null) _probe = GetComponent<PlanarReflectionProbe>();
        if (_probe == null || _probe.mode != ProbeSettings.Mode.Realtime) return;
        _originalMode = _probe.realtimeMode;
        _ownsMode = true;
        _probe.realtimeMode = ProbeSettings.RealtimeMode.OnDemand;
        _nextRefresh = 0;
        Refresh();
    }

    /// <summary>通常のCamera更新後に、経過時間と視点変化を調べて必要な反射撮影だけを要求する。</summary>
    private void LateUpdate()
    {
        if (!_ownsMode || _probe == null || _viewerCamera == null || !_viewerCamera.isActiveAndEnabled) return;
        var viewer = _viewerCamera.transform;
        bool moved = (viewer.position - _lastPosition).sqrMagnitude >= _positionThreshold * _positionThreshold;
        bool turned = Quaternion.Angle(viewer.rotation, _lastRotation) >= _rotationThreshold;
        bool lensChanged = Mathf.Abs(_viewerCamera.fieldOfView - _lastFieldOfView) >= 1
                           || Mathf.Abs(_viewerCamera.aspect - _lastAspect) >= 0.01f;
        if (Time.unscaledTimeAsDouble >= _nextRefresh || moved || turned || lensChanged) Refresh();
    }

    /// <summary>HDRPへ次回描画の更新を要求し、比較に用いるCamera姿勢と次の更新時刻を記録する。</summary>
    private void Refresh()
    {
        if (_probe == null) return;
        _probe.RequestRenderNextUpdate();
        _nextRefresh = Time.unscaledTimeAsDouble + 1.0 / Mathf.Max(1, _maximumUpdatesPerSecond);
        if (_viewerCamera == null) return;
        _lastPosition = _viewerCamera.transform.position;
        _lastRotation = _viewerCamera.transform.rotation;
        _lastFieldOfView = _viewerCamera.fieldOfView;
        _lastAspect = _viewerCamera.aspect;
    }

    /// <summary>コンポーネントを無効にした際は元の更新方式へ戻し、反射が停止したまま残ることを防ぐ。</summary>
    private void OnDisable()
    {
        if (_ownsMode && _probe != null && _probe.realtimeMode == ProbeSettings.RealtimeMode.OnDemand)
        {
            _probe.realtimeMode = _originalMode;
            _probe.RequestRenderNextUpdate();
        }
        _ownsMode = false;
    }
}
