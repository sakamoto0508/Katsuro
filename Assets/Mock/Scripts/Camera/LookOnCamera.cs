using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera
{
    public LookOnCamera(Transform playerPosition, Transform enemyPosition
        , CinemachineCamera camera, CameraConfig config)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _cameraConfig = config;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CameraConfig _cameraConfig;

    public void Update()
    {
        if (!IsLockOn) return;

        Vector3 toEnemy = _enemyPosition.position - _playerPosition.position;
        toEnemy.y = 0;
        if (toEnemy.sqrMagnitude < 0.01f)
        {
            UnLockOn();
            return;
        }
        Vector3 forward = toEnemy.normalized;
        //プレイヤーの後ろにカメラを移動させる
        Vector3 desiredPos = _playerPosition.position - forward
            * _cameraConfig.CameraDistance + Vector3.up * _cameraConfig.CameraHeight;

    }

    public void LockOn()
    {
        IsLockOn = true;
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;

        IsLockOn = false;
    }

    public Vector3 ReturnLockOnDirection()
    {
        if (!IsLockOn) return Vector3.zero;

        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        return direction.normalized;
    }
}
