using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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
    private Transform _cameraTarget;
    private CinemachineCamera _camera;

    public void Update()
    {
        if (!IsLockOn) return;

        // プレイヤーのYawだけをコピー
        Vector3 euler = _cameraTarget.eulerAngles;
        euler.y = _playerPosition.eulerAngles.y;
        _cameraTarget.eulerAngles = euler;
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
