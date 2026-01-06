using Unity.Cinemachine;
using UnityEngine;

public class LockOnCamera
{
    public LockOnCamera(Transform playerPosition, Transform enemyPosition
        , CinemachineCamera camera, CinemachineCamera lockOnCamera)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _lockOnCamera = lockOnCamera;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CinemachineCamera _lockOnCamera;
    private CinemachineBrain _brain;

    public void LockOn()
    {
        IsLockOn = true;
        
        _camera.Priority = 0;
        _lockOnCamera.Priority = 10;
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;

        IsLockOn = false;
        _camera.Priority = 10;
        _lockOnCamera.Priority = 0;
    }

    /// <summary>
    /// プレイヤー用：敵を見る方向（移動・回転用）
    /// </summary>
    /// <returns></returns>
    public Vector3 ReturnLockOnDirection()
    {
        if (!IsLockOn) return Vector3.zero;

        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        return direction.normalized;
    }
}
