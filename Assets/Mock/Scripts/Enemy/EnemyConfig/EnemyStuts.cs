using UnityEngine;

public class EnemyStuts : ScriptableObject
{
    /// <summary> エネミーの基礎攻撃力 /// </summary>
    public float EnemyPower => _enemyPower;
    /// <summary> エネミーの最大体力 /// </summary>
    public float EnemyMaxHealth => _enemyMaxHealth;
    /// <summary> 追跡開始距離 /// </summary>
    public float ChaseStartDistance => _chaseStartDistance;
    /// <summary> 攻撃想定距離 /// </summary>
    public float StopDistance => _stopDistance;
    /// <summary> 目的地更新間隔 /// </summary>
    public float DestinationUpdateInterval => _destinationUpdateInterval;

    [SerializeField] private float _enemyPower = 10f;
    [SerializeField] private float _enemyMaxHealth = 100f;

    [Header("Chase Settings")]
    [SerializeField] private float _chaseStartDistance = 10f;   // 追跡開始距離
    [SerializeField] private float _stopDistance = 2.0f;        // 攻撃想定距離
    [SerializeField] private float _destinationUpdateInterval = 0.2f;
}
