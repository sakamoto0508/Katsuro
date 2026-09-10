using Unity.Cinemachine;
using UnityEngine;

/// <summary>Moves the camera while lock-on has manual control of the view.</summary>
public class LockOnCameraMover
{
    public LockOnCameraMover(LockOnCamera lockOnCamera, Transform playerPosition,
        Transform enemyPosition, CinemachineCamera camera, CameraConfig config)
    {
        _lockOnCamera = lockOnCamera;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _cameraConfig = config;
        _mainCamera = Camera.main;
        if (_mainCamera != null) _brain = _mainCamera.GetComponent<CinemachineBrain>();
    }

    private readonly LockOnCamera _lockOnCamera;
    private readonly Transform _playerPosition;
    private readonly Transform _enemyPosition;
    private readonly CinemachineCamera _camera;
    private readonly CameraConfig _cameraConfig;
    private Camera _mainCamera;
    private CinemachineBrain _brain;

    public void LateUpdate()
    {
        if (_lockOnCamera == null || !_lockOnCamera.IsLockOn) return;
        if (_playerPosition == null || _enemyPosition == null || !_lockOnCamera.HasValidTarget())
        {
            _lockOnCamera.UnLockOn();
            return;
        }
        if (_cameraConfig == null) return;
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null) _brain = _mainCamera.GetComponent<CinemachineBrain>();
        }
        Transform cameraTransform = _mainCamera != null && _brain != null && !_brain.enabled
            ? _mainCamera.transform : _camera != null ? _camera.transform : null;
        if (cameraTransform == null) return;

        Vector3 forward = _enemyPosition.position - _playerPosition.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return;
        forward.Normalize();
        Vector3 desiredPosition = _playerPosition.position - forward * _cameraConfig.CameraDistance
            + Vector3.up * _cameraConfig.CameraHeight;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition,
            Time.deltaTime * _cameraConfig.PositionSmooth);
        Vector3 lookDirection = _enemyPosition.position + Vector3.up * _cameraConfig.LookAtHeight
            - cameraTransform.position;
        if (lookDirection.sqrMagnitude < 0.0001f) return;
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation,
            Quaternion.LookRotation(lookDirection), Time.deltaTime * _cameraConfig.RotationSmooth);
    }
}
