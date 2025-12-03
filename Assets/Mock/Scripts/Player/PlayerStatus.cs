using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatus", menuName = "ScriptableObjects/Player/PlayerStatus", order = 1)]
public class PlayerStatus : ScriptableObject
{
    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float AttackPower => _attackPower;

    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _attackPower = 10f;
}
