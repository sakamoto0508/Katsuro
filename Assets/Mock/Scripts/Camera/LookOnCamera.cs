using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class LookOnCamera
{
    public LookOnCamera(Transform playerPosition, Transform enemyPosition
        , CinemachineCamera camera, CinemachineTargetGroup targetGroup)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _targetGroup = targetGroup;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CinemachineTargetGroup _targetGroup;

    public void Update()
    {
        if (!IsLockOn) return;


    }

    public void LockOn()
    {
        IsLockOn = true;
        _targetGroup.AddMember(_enemyPosition, 1f, 0f);
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;

        IsLockOn = false;
        _targetGroup.RemoveMember(_enemyPosition);
    }

    public Vector3 ReturnLockOnDirection()
    {
        if (!IsLockOn) return Vector3.zero;

        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        return direction.normalized;
    }
}
