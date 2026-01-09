using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatus", menuName = "ScriptableObjects/Player/PlayerStatus", order = 1)]
public class PlayerStatus : ScriptableObject
{
    public int MaxHealth => _maxHealth;
    public float AttackPower => _attackPower;
    public float WalkSpeed => _walkSpeed;
    public float SprintSpeed => _sprintSpeed;
    public float LockOnWalkSpeed => _lockOnWalkSpeed;
    public float LockOnSprintSpeed => _lockOnSprintSpeed;
    public float RoationSmoothness => _roationSmoothness;
    public float Acceleration => _acceleration;
    public float BreakForce => _breakForce;

    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _attackPower = 10f;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 5f;
    [SerializeField] private float _lockOnWalkSpeed = 3f;
    [SerializeField] private float _lockOnSprintSpeed = 5f;
    [SerializeField] private float _roationSmoothness = 0.25f;
    [SerializeField] private float _acceleration = 5f;
    [SerializeField] private float _breakForce = 0.90f;
} 
