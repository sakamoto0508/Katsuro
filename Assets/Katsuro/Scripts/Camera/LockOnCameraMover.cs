using Unity.Cinemachine;
using UnityEngine;

/// <summary>ロックオン中に視点を手動制御する際のカメラ移動を行う。</summary>
public class LockOnCameraMover
{
    /// <summary>追従対象とカメラ設定を保持し、Brainの有効状態に応じた追従先を準備する。</summary>
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
    private readonly RaycastHit[] _obstacles = new RaycastHit[32];

    /// <summary>有効なLock-On対象を向く位置と角度へ補間する。対象を失った場合はLock-Onを解除する。</summary>
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
        Vector3 focus = Vector3.Lerp(_playerPosition.position, _enemyPosition.position,
            _cameraConfig.LockOnTargetWeight) + Vector3.up * _cameraConfig.LookAtHeight;
        Vector3 desiredPosition = _playerPosition.position - forward * _cameraConfig.CameraDistance
            + Vector3.up * _cameraConfig.CameraHeight
            + Vector3.Cross(Vector3.up, forward) * _cameraConfig.LockOnSideOffset;
        Vector3 nextPosition = Vector3.Lerp(cameraTransform.position, desiredPosition,
            1f - Mathf.Exp(-Time.deltaTime * _cameraConfig.PositionSmooth));
        // 衝突判定の始点はPlayer側に保ち、遠方の敵と壁の位置でカメラが飛ぶことを避ける。
        Vector3 collisionOrigin = _playerPosition.position + Vector3.up * _cameraConfig.LookAtHeight;
        cameraTransform.position = ResolveObstacles(collisionOrigin, nextPosition);
        Vector3 lookDirection = focus - cameraTransform.position;
        if (lookDirection.sqrMagnitude < 0.0001f) return;
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation,
            Quaternion.LookRotation(lookDirection), 1f - Mathf.Exp(-Time.deltaTime * _cameraConfig.RotationSmooth));
    }

    /// <summary>注視点と補間済みカメラ間の環境障害物を調べ、壁の手前へ出力位置を制限する。</summary>
    private Vector3 ResolveObstacles(Vector3 focus, Vector3 position)
    {
        Vector3 direction = position - focus;
        float distance = direction.magnitude;
        if (distance < .001f) return position;
        direction /= distance;
        int count = Physics.SphereCastNonAlloc(focus, _cameraConfig.CameraCollisionRadius, direction,
            _obstacles, distance, _cameraConfig.LockOnObstacleMask, QueryTriggerInteraction.Ignore);
        float nearest = distance;
        for (int i = 0; i < count; i++)
        {
            Transform hit = _obstacles[i].transform;
            if (hit == null || hit.IsChildOf(_playerPosition) || hit.IsChildOf(_enemyPosition)) continue;
            nearest = Mathf.Min(nearest, Mathf.Max(.05f, _obstacles[i].distance - .05f));
        }
        return focus + direction * nearest;
    }
}
