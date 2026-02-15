using Cysharp.Threading.Tasks;
using INab.VFXAssets;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Enemy Status")]
    [SerializeField] private EnemyStuts _enemyStuts;
    [SerializeField] private AnimationName _animName;

    [Header("Weapon")]
    [SerializeField] private Collider[] _enemyWeaponColliders;
    [SerializeField] private Collider[] _playerWeaponColliders;

    [Header("Attack")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyAttackData[] _attackData;

    [Header("AI")]
    [SerializeField] private EnemyDecisionConfig _decisionConfig;
    [SerializeField] private float _stepBackDistance = 2f;

    [SerializeField] private int _enemyDeadDelay = 2000;

    private EnemyAnimationController _enemyAnimController;
    private EnemyHealth _health;
    private EnemyAttacker _attacker;
    private EnemyMover _mover;
    private EnemyAI _ai;
    private EnemyActionType? _pendingAction;
    private CancellationToken _token;
    private bool _dead = false;

    /// <summary>
    /// 初期化処理：必要なランタイムコンポーネントを生成して接続します。
    /// </summary>
    public void Init(Transform playerPosition)
    {
        var navMeshAgent = GetComponent<NavMeshAgent>();
        var rb = GetComponent<Rigidbody>();
        _enemyAnimController = GetComponent<EnemyAnimationController>();
        var enemyEffect = GetComponent<CharacterEffect>();
        enemyEffect.PlayEffect_CharacterEffect();
        //クラスの初期化
        _mover = new EnemyMover(_enemyStuts, this.transform, playerPosition, _enemyAnimController, rb
            , navMeshAgent, _animator, _animName);
        _health = new EnemyHealth(_enemyStuts);
        var fallback = _enemyStuts != null ? _enemyStuts.EnemyPower : 0f;
        var wrapper = new EnemyWeapon(_enemyWeaponColliders, fallback);
        _attacker = new EnemyAttacker(_enemyAnimController, _attackData, new EnemyWeapon[] { wrapper }, _enemyStuts, this.transform);
        _ai = new EnemyAI(this, playerPosition, navMeshAgent, _decisionConfig);
        if (GetComponent<StatusEffectManager>() == null)
        {
            gameObject.AddComponent<StatusEffectManager>();
        }
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
        if (_dead) return;
        //todo:ダメージSEとヒットストップの追加
        if (_health == null)
        {
            // 予防: Health が未割当ての場合は生成してから適用する
            _health = new EnemyHealth(_enemyStuts);
        }

        _health.ApplyDamage(info.DamageAmount);

        if (_health.CurrentHp <= 0f && !_dead)
        {
            EnemyDead();
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

        // AI が設定したペンディングの行動を先に実行してから移動更新を行う。
        // これにより WaitWalk 等が選択されたフレームで即座に振る舞いを反映できます。
        if (_pendingAction != null)
        {
            var action = _pendingAction.Value;
            _pendingAction = null;
            switch (action)
            {
                case EnemyActionType.Approach:
                    _mover?.Approach();
                    break;
                case EnemyActionType.WaitWalk:
                    _mover?.StartPatrolWalk();
                    break;
                case EnemyActionType.Slash:
                    _mover?.FacePlayerInstant();
                    _mover?.HoldMovementForAttack();
                    _attacker?.PerformAttack(action);
                    break;
                case EnemyActionType.Thrust:
                case EnemyActionType.HeavySlash:
                    _mover?.FacePlayerInstant();
                    _mover?.HoldMovementForAttack();
                    _attacker?.PerformAttack(action);
                    break;
                case EnemyActionType.WarpAttack:
                    _mover?.FacePlayerInstant();
                    _mover?.HoldMovementForAttack();
                    _attacker?.PerformAttack(action);
                    break;
                case EnemyActionType.StepBack:
                    _mover?.StartStepBack();
                    break;
                case EnemyActionType.Wait:
                    _mover?.StopMove();
                    break;
            }
        }

        // 毎フレーム移動更新を行う（EnemyMover が内部で追跡判定を行う）
        _mover?.Update();
    }

    private void EnemyDead()
    {
        _enemyAnimController.PlayTrigger(_enemyAnimController.AnimName.EnemyDead);
        _dead = true;

        FinalBlowManager.Instance?.StartFinalBlow();

        LoadSceneManager.Instance.LoadSceneAsync(LoadSceneManager.Instance.SceneNameConfig.TitleScene, _enemyDeadDelay).Forget();
    }

    private void OnAnimatorMove()
    {
        _mover?.OnAnimatorMove();
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
            return;
        }
        var data = _attackData[attackIndex];
        if (data == null) return;
        _attacker?.PerformAttack(data.ActionType);
    }

    /// <summary>
    /// アニメーションイベント用: 攻撃アニメ終了を通知して AI の再抽選を可能にする。
    /// アニメの終端にこのイベントを配置してください。
    /// </summary>
    public void AnimEvent_OnAttackFinished()
    {
        _ai.OnAttackFinished();
        // 攻撃終了時に一時停止していた移動制御を復帰させる
        _mover?.ReleaseMovementAfterAttack();
    }

    public void AnimEvent_OnStepBackFinished()
    {
        _mover?.EndStepBack();
        _ai?.OnAttackFinished(); // または専用の完了処理
        // 攻撃停止後は移動を復帰する
        _mover?.ReleaseMovementAfterAttack();
    }
}
