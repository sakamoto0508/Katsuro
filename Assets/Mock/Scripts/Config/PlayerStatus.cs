using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatus", menuName = "ScriptableObjects/Player/PlayerStatus", order = 1)]
public class PlayerStatus : ScriptableObject
{
    public int Life => _life;
    public int MaxHealth => _maxHealth;
    public float AttackPower => _attackPower;
    public float NoWeaponMoveSpeed => _noWeaponMoveSpeed;
    public float NoWeaponSprintSpeed => _noWeaponSprintSpeed;
    public float UnLockWalkSpeed => _unLockWalkSpeed;
    public float UnLockSprintSpeed => _unLockSprintSpeed;
    public float LockOnWalkSpeed => _lockOnWalkSpeed;
    public float LockOnSprintSpeed => _lockOnSprintSpeed;
    public float RoationSmoothness => _roationSmoothness;
    public float Acceleration => _acceleration;
    public float BreakForce => _breakForce;

    [Header("Basic Status")]
    [SerializeField] private int _life = 3;
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _attackPower = 10f;

    [Header("Movement")]
    [SerializeField] private float _noWeaponMoveSpeed = 5f;
    [SerializeField] private float _noWeaponSprintSpeed = 8f;
    [SerializeField] private float _unLockWalkSpeed = 5f;
    [SerializeField] private float _unLockSprintSpeed = 5f;
    [SerializeField] private float _lockOnWalkSpeed = 3f;
    [SerializeField] private float _lockOnSprintSpeed = 5f;
    [SerializeField] private float _roationSmoothness = 0.25f;
    [SerializeField] private float _acceleration = 5f;
    [SerializeField] private float _breakForce = 0.90f;
} 
