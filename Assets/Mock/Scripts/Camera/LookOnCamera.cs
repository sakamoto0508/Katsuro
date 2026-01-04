using UnityEngine;

public class LookOnCamera
{
    public LookOnCamera(CameraConfig cameraConfig)
    {
        IsLockOn = false;
        _cameraConfig = cameraConfig;
        _playerPosition = _cameraConfig.PlayerPosition;
        _enemyPosition = _cameraConfig.EnemyPosition;
    }

    public bool IsLockOn { get; private set; }
    private CameraConfig _cameraConfig; 
    private Transform _playerPosition;
    private Transform _enemyPosition;

    public void LockOn()
    {
        IsLockOn = true;
    }

    public void UnLockOn()
    {
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
