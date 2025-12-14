using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class LookOnCamera : MonoBehaviour
{
    public bool IsLockOn { get; private set; }
    [SerializeField] private CinemachineCamera _lockOnCamera;
    [SerializeField] private Transform _playerTarget;
    [SerializeField] private Transform _enemyTarget;
    [Header("LockOn Camera Distance")]
    [SerializeField] private float _lockOnMinCameraDistance = 5.5f;
    [SerializeField] private float _lockOnMaxCameraDistance = 8.5f;
    [SerializeField] private float _lockOnZoomOutDistance = 12f;
    [Header("LockOn Viewport Safety")]
    [SerializeField] float _lockOnViewportMargin = 0.1f;
    [SerializeField] float _lockOnZoomOutSpeed = 3f;
    private CinemachinePositionComposer _position;

    public void LockOn(Transform enemy)
    {
        IsLockOn = true;
        _lockOnCamera.Target = new CameraTarget
        {
            TrackingTarget = enemy
        };
    }

    public void UnLock()
    {
        IsLockOn = false;
        _lockOnCamera.Target = default;
    }

    private void Awake()
    {
        _position = _lockOnCamera.GetComponent<CinemachinePositionComposer>();
    }

    private void LateUpdate()
    {
        if (_enemyTarget == null) return;

        float dist = Vector3.Distance(_playerTarget.position, _enemyTarget.position);
        float t = Mathf.Clamp01(dist / _lockOnZoomOutDistance);
        _position.CameraDistance = Mathf.Lerp(_lockOnMinCameraDistance, _lockOnMaxCameraDistance, t);
        KeepEnemyInView();
    }
    
    private void KeepEnemyInView()
    {
        Vector3 vp = Camera.main.WorldToScreenPoint(_enemyTarget.position);
        bool outOfView = vp.z < 0 ||
            vp.x < _lockOnViewportMargin ||
            vp.x > 1f - _lockOnViewportMargin ||
            vp.y < _lockOnViewportMargin ||
            vp.y > 1f - _lockOnViewportMargin;
        if (outOfView)
        {
            _position.CameraDistance += Time.deltaTime * _lockOnZoomOutSpeed;
        }
    }
}
