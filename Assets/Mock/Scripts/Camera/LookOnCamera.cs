using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera : MonoBehaviour, ICinemachineTargetGroup
{
    public Transform PlayerTarget => _playerTarget;
    public Transform EnemyTarget => _enemyTarget;

    [NoSaveDuringPlay]
    [SerializeField] private Transform _playerTarget;
    [NoSaveDuringPlay]
    [SerializeField] private Transform _enemyTarget;
    [SerializeField,Tooltip("各ターゲットの重み")]
    private float _playerWeight = 1.0f;
    [SerializeField] private float _enemyWeight = 1.0f;
    [SerializeField, Tooltip("バウンディングボックスの余白")]
    private float _boundsPodding = 1.0f;

    public bool IsValid => throw new System.NotImplementedException();

    public Transform Transform => throw new System.NotImplementedException();

    public Bounds BoundingBox => throw new System.NotImplementedException();

    public BoundingSphere Sphere => throw new System.NotImplementedException();

    public bool IsEmpty => throw new System.NotImplementedException();

    public void GetViewSpaceAngularBounds(Matrix4x4 observer, out Vector2 minAngles, out Vector2 maxAngles, out Vector2 zRange)
    {
        throw new System.NotImplementedException();
    }

    public Bounds GetViewSpaceBoundingBox(Matrix4x4 observer, bool includeBehind)
    {
        throw new System.NotImplementedException();
    }
}
