using UnityEngine;

public class EnemyStuts : ScriptableObject
{
    public float EnemyPower => _enemyPower;
    public float EnemyMaxHealth => _enemyMaxHealth;

    [SerializeField] private float _enemyPower = 10f;
    [SerializeField] private float _enemyMaxHealth = 100f;
}
