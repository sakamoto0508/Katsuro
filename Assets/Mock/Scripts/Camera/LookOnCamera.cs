using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera
{
    public LookOnCamera(Transform playerPosition, Transform enemyPosition, CinemachineCamera camera)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;

    public void Update()
    {
        if (!IsLockOn) return;

        Vector3 forward = _playerPosition.forward;
        forward.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(forward);
        _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation
            , targetRotation, Time.deltaTime * 8f);
    }

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
