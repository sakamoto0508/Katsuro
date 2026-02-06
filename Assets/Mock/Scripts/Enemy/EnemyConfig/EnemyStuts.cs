using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStuts", menuName = "ScriptableObjects/Enemy/EnemyStuts", order = 1)]
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

    [Header("Decision Weights")]
    [SerializeField] public float FarDistance = 12f;
    [SerializeField] public float NearDistance = 3f;
    [SerializeField] public float ObserveSeconds = 1.5f;

    [SerializeField] public float WeightWarpBehind = 10f;
    [SerializeField] public float WeightApproach = 30f;
    [SerializeField] public float WeightRush = 30f;
    [SerializeField] public float WeightObserve = 20f;
    [SerializeField] public float WeightMelee = 50f;
    [SerializeField] public float WeightBackstep = 20f;

    public enum RotationControlMode
    {
        Agent,      // NavMeshAgent に回転を任せる
        Snap,       // 即時でプレイヤー方向へスナップ
        Smooth      // XZ 平面で滑らかに回転する（TurnSpeed を使用）
    }

    [Header("Rotation")]
    public RotationControlMode RotationMode = RotationControlMode.Smooth;
    [Tooltip("回転の速度（Smooth モード時の最大角速度、度/秒）")]
    public float TurnSpeed = 360f;
}
