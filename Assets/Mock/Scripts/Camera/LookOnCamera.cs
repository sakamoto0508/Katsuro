using UnityEngine;

public class LookOnCamera
{
    public LookOnCamera(Transform playerPosition,Transform enemyPosition)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
    }

    public bool IsLockOn { get; private set; }
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
