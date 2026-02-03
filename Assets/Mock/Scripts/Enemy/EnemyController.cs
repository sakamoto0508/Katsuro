using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Enemy Status")]
    [SerializeField] private EnemyStuts _enemyStuts;

    [Header("Tags")]
    [SerializeField] private string _playerWeaponTag = "PlayerWeapon";

    [Header("Weapon")]
    [SerializeField] private Collider[] _enemyWeaponColliders;
    [SerializeField] private Collider[] _playerWeaponColliders;

    [Header("Attack")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyAttackData[] _attackData;
    [Header("AI")]
    [SerializeField] private EnemyDecisionConfig _decisionConfig;
    [SerializeField] private float _stepBackDistance = 2f;

    private EnemyHealth _health;
    private EnemyAttacker _attacker;
    private EnemyMover _mover;
    private EnemyAI _ai;
    // pending action set by AI; executed in Update()
    private EnemyActionType? _pendingAction;

    /// <summary>
    /// 初期化処理：必要なランタイムコンポーネントを生成して接続します。
    /// </summary>
    public void Init(Transform playerPosition)
    {
        var navMeshAgent = GetComponent<NavMeshAgent>();
        //クラスの初期化
        _mover = new EnemyMover(_enemyStuts, navMeshAgent, playerPosition);
        _health = new EnemyHealth(_enemyStuts);
        var fallback = _enemyStuts != null ? _enemyStuts.EnemyPower : 0f;
        var wrapper = new EnemyWeapon(_enemyWeaponColliders, fallback);
        _attacker = new EnemyAttacker(_animator, _attackData, new EnemyWeapon[] { wrapper }, _enemyStuts, this.transform);

        
    }

    /// <summary>
    /// Called by AI to enqueue an action to be executed on the next Update.
    /// </summary>
    public void EnqueueAction(EnemyActionType action)
    {
        _pendingAction = action;
    }

    /// <summary>
    /// 公開: ダメージを適用するエントリポイント。DamageInfo を受け取り HP を減算します。
    /// </summary>
    // IDamageable インターフェース実装（正確なシグネチャ）
    public void ApplyDamage(DamageInfo info)
    {
        ApplyDamage(info, false);
    }

    /// <summary>
    /// 公開: ダメージを適用するエントリポイント（拡張）。クリティカルフラグなどの追加引数を受けます。
    /// </summary>
    public void ApplyDamage(DamageInfo info, bool isCritical = false)
    {
        if (_health == null)
        {
            // 予防: Health が未割当ての場合は生成してから適用する
            _health = new EnemyHealth(_enemyStuts);
        }

        _health.ApplyDamage(info.DamageAmount);

        Debug.Log($"Enemy took {info.DamageAmount} damage, currentHp={_health.CurrentHp}");

        if (_health.CurrentHp <= 0f)
        {
            Debug.Log("Enemy dead");
            // とりあえずオブジェクトを破棄する。必要に応じて死亡処理を拡張してください。
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        _attacker?.Dispose();
        _health?.Dispose();
    }

    private void Update()
    {
        // AI の Tick を先に呼び、意思決定を行わせる
        _ai?.Tick(Time.deltaTime);

        // 毎フレーム移動更新を行う（EnemyMover が内部で追跡判定を行う）
        _mover?.Update();

        // AI が設定したペンディングの行動を実行する（ここで実際の制御を呼び出す）
        if (_pendingAction != null)
        {
            var action = _pendingAction.Value;
            _pendingAction = null;
            switch (action)
            {
                case EnemyActionType.Approach:
                    _mover?.Approach();
                    break;
                case EnemyActionType.Slash:
                case EnemyActionType.Thrust:
                case EnemyActionType.HeavySlash:
                case EnemyActionType.WarpAttack:
                    // 攻撃は EnemyAttacker 経由で実行（Animator のトリガなどはそこが担当）
                    _attacker?.PerformAttack(action);
                    break;
                case EnemyActionType.StepBack:
                    _mover?.StepBack(_stepBackDistance);
                    break;
                case EnemyActionType.Wait:
                    // Observe 相当: 停止して何もしない（AI のタイマーで再抽選される）
                    _mover?.StopMove();
                    break;
            }
        }
    }

    // ---------- Animation Events (Enemy) ----------
    /// <summary>
    /// アニメーションイベント用: ヒットボックスを有効化する（攻撃有効フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        _attacker?.EnableWeaponHitbox();
    }

    /// <summary>
    /// アニメーションイベント用: ヒットボックスを無効化する（攻撃終了フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_DisableWeaponHitbox()
    {
        _attacker?.DisableWeaponHitbox();
    }

    /// <summary>
    /// アニメーションイベント用: アニメから攻撃種別を指定して PerformAttack を呼ぶ（optional）。
    /// アニメ上で攻撃トリガーを開始したい場合に使用します。
    /// </summary>
    /// <param name="attackIndex">_attackData の配列インデックス</param>
    public void AnimEvent_PerformAttack(int attackIndex)
    {
        if (_attackData == null || attackIndex < 0 || attackIndex >= _attackData.Length)
        {
            Debug.LogWarning("AnimEvent_PerformAttack: invalid attackIndex");
            return;
        }
        var data = _attackData[attackIndex];
        if (data == null) return;
        _attacker?.PerformAttack(data.actionType);
    }
}
