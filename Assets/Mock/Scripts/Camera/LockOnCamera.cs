using Unity.Cinemachine;
using UnityEngine;

public class LockOnCamera
{
    public LockOnCamera(Transform playerPosition, Transform enemyPosition
        , CinemachineCamera camera, CinemachineBrain brain)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _brain = brain;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CinemachineBrain _brain;

    public void LockOn()
    {
        IsLockOn = true;
        // Cinemachine による制御を止める。
        _camera.enabled = false;
        //_brain.enabled = false;
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;

        IsLockOn = false;
        // Cinemachine を戻す。
        _camera.enabled = true;
        //_brain.enabled = true;
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
