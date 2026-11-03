using UnityEngine;

/// <summary>EnemyのHP・攻撃・追跡・移動・旋回方式をInspectorで定義する基礎設定。</summary>
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
    /// <summary> 回転の滑らかさ /// </summary>
    public float RotateSmoothTime => _rotateSmoothTime;

    /// <summary>Enemy武器を初期化する基準攻撃力。行動別Damageを使う場合はその値が優先される。</summary>
    [UnityEngine.Tooltip("Enemy武器を初期化する基準攻撃力。行動別Damageを使う場合はその値が優先される。")]
    [SerializeField] private float _enemyPower = 10f;
    /// <summary>Enemyの最大HP。体力初期化とHP比率の基準。</summary>
    [UnityEngine.Tooltip("Enemyの最大HP。体力初期化とHP比率の基準。")]
    [SerializeField] private float _enemyMaxHealth = 100f;

    /// <summary>追跡開始距離の基礎設定（Unity単位）。距離帯別の行動判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("追跡開始距離の基礎設定（Unity単位）。距離帯別の行動判断はEnemyDecisionConfigを使用する。")]
    [Header("Chase Settings")]
    [SerializeField] private float _chaseStartDistance = 10f;   // 追跡開始距離
    /// <summary>Playerへ近づく際のNavMeshAgent停止距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("Playerへ近づく際のNavMeshAgent停止距離（Unity単位）。")]
    [SerializeField] private float _stopDistance = 2.0f;        // 攻撃想定距離
    /// <summary>追跡中にNavMeshAgentの目的地を更新する間隔（秒）。</summary>
    [UnityEngine.Tooltip("追跡中にNavMeshAgentの目的地を更新する間隔（秒）。")]
    [SerializeField] private float _destinationUpdateInterval = 0.2f;

    /// <summary>旧形式の遠距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の遠距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。")]
    [Header("Decision Weights")]
    [SerializeField] public float FarDistance = 12f;
    /// <summary>旧形式の近距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の近距離閾値（Unity単位）。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float NearDistance = 3f;
    /// <summary>旧形式の様子見時間（秒）。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の様子見時間（秒）。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float ObserveSeconds = 1.5f;

    /// <summary>旧形式の背後Warp行動の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の背後Warp行動の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightWarpBehind = 10f;
    /// <summary>旧形式の接近行動の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の接近行動の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightApproach = 30f;
    /// <summary>旧形式の突進行動の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の突進行動の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightRush = 30f;
    /// <summary>旧形式の様子見行動の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の様子見行動の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightObserve = 20f;
    /// <summary>旧形式の近接攻撃の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の近接攻撃の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightMelee = 50f;
    /// <summary>旧形式の後退行動の重み。現行の判断はEnemyDecisionConfigを使用する。</summary>
    [UnityEngine.Tooltip("旧形式の後退行動の重み。現行の判断はEnemyDecisionConfigを使用する。")]
    [SerializeField] public float WeightBackstep = 20f;
    /// <summary>旧回転補間の時間設定（秒）。Smooth旋回はTurnSpeedによる角速度制御を使う。</summary>
    [UnityEngine.Tooltip("旧回転補間の時間設定（秒）。Smooth旋回はTurnSpeedによる角速度制御を使う。")]
    [SerializeField] private float _rotateSmoothTime = 0.1f;

    /// <summary>Enemyの旋回をAgent任せ、対象へ即時旋回、手動補間のいずれで行うか識別する。</summary>
    public enum RotationControlMode
    {
        Agent,      // NavMeshAgent に回転を任せる
        Snap,       // 即時でプレイヤー方向へスナップ
        Smooth      // XZ 平面で滑らかに回転する（TurnSpeed を使用）
    }

    /// <summary>Enemyの向きの制御方式。Agent任せ・即時旋回・角速度で滑らかに旋回から選ぶ。</summary>
    [UnityEngine.Tooltip("Enemyの向きの制御方式。Agent任せ・即時旋回・角速度で滑らかに旋回から選ぶ。")]
    [Header("Rotation")]
    public RotationControlMode RotationMode = RotationControlMode.Smooth;
    /// <summary>回転の速度（Smooth モード時の最大角速度、度/秒）</summary>
    [Tooltip("回転の速度（Smooth モード時の最大角速度、度/秒）")]
    public float TurnSpeed = 360f;
}
