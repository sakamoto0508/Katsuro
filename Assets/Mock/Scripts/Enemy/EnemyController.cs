using Cysharp.Threading.Tasks;
using INab.VFXAssets;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IDamageable
{
    /// <summary>
    /// 敵の現在のHP比率を取得します。0.0f（死亡）から1.0f（満タン）の範囲で返されます。
    /// </summary>
    public float HpRatio => _health != null ? _health.CurrentHpRatio : 1f;

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

    [SerializeField] private CharacterEffect _characterEffect;
    [SerializeField] private int _enemyDeadDelay = 2000;

    private EnemyAnimationController _enemyAnimController;
    private EnemyHealth _health;
    private EnemyAttacker _attacker;
    private EnemyMover _mover;
    private EnemyAI _ai;
    private EnemyActionType? _pendingAction;
    private CancellationToken _token;
    private bool _dead = false;
    private GameManager _game;
    private AudioManager _audio;
    private FinalBlowManager _finalBlow;
    private LoadSceneManager _loader;
    private DamageNumbers _damageNumbers;
    private CombatFeedback _combatFeedback;
    private bool _initialized;

    public void Init(Transform playerPosition, DamageNumbers damageNumbers, GameManager game, AudioManager audio, HitStopManager hitStop, FinalBlowManager finalBlow, LoadSceneManager loader)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game; _audio = audio; _finalBlow = finalBlow; _loader = loader;
        _damageNumbers = damageNumbers;
        _combatFeedback = GetComponent<CombatFeedback>();
        if (_combatFeedback != null) _combatFeedback.Init();
        else Debug.LogWarning("Assign CombatFeedback on the character root. Feedback is disabled.", this);
        var navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) navMeshAgent.speed *= RunSession.EnemyMoveSpeed;
        gameObject.name = RunSession.Opponent?.Name ?? gameObject.name;
        var rb = GetComponent<Rigidbody>();
        _enemyAnimController = GetComponent<EnemyAnimationController>();
        _enemyAnimController.Init();
        foreach (var speed in GetComponentsInChildren<AnimationSpeedController>(true)) speed.Init();
        hitStop?.RegisterTarget(gameObject);
        //クラスの初期化
        _mover = new EnemyMover(_enemyStuts, this.transform, playerPosition, _enemyAnimController, rb
            , navMeshAgent, _animator, _animName);
        _health = new EnemyHealth(_enemyStuts);
        var fallback = _enemyStuts != null ? _enemyStuts.EnemyPower : 0f;
        var wrapper = new EnemyWeapon(_enemyWeaponColliders, fallback);
        wrapper.Init();
        _attacker = new EnemyAttacker(_enemyAnimController, _attackData, new EnemyWeapon[] { wrapper }, _enemyStuts, this.transform);
        _attacker.Init(hitStop);
        _ai = new EnemyAI(this, playerPosition, navMeshAgent, _decisionConfig);


        GetComponent<StatusEffectManager>()?.Init();
        PlayEffectNextFrame("Dark").Forget();
        if (_characterEffect != null) _characterEffect.PlayEffect_CharacterEffect();
    }

    private async UniTask PlayEffectNextFrame(string key)
    {
        // 生成したプレハブと視覚効果の初期化を待つため、1フレーム待機する。
        await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
        if (_characterEffect == null)
        {
            Debug.LogWarning("CharacterEffect is null when trying to play effect: " + key);
            return;
        }
        _characterEffect.PlayEffectByKey(key);
    }

    /// <summary>
    /// 敵の思考処理から呼び、次の更新で実行する行動を予約する。
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
        if (_health == null || _dead || (_game != null && !_game.IsCombatActive)) return;
        _audio?.PlaySE("Damage");

        float before = _health.CurrentHp;
        _health.ApplyDamage(info.DamageAmount * RunSession.EnemyDefense);
        float dealt = before - _health.CurrentHp;
        if (dealt > 0f)
        {
            _combatFeedback?.Hit();
            _damageNumbers?.Show(transform.position, dealt, isCritical);
        }

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
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
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
        _pendingAction = null;
        _attacker?.DisableWeaponHitbox();
        _mover?.HoldMovementForAttack();

        _game?.WinGame();
        if (_finalBlow != null) _finalBlow.StartFinalBlow();
        else _loader?.LoadSceneAsync(_loader.SceneNameConfig.TitleScene, 2000).Forget();
    }

    private void OnAnimatorMove()
    {
        _mover?.OnAnimatorMove();
    }

    // ---------- 敵のアニメーションイベント ----------
    /// <summary>
    /// アニメーションイベント用: ヒットボックスを有効化する（攻撃有効フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
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
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
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
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
        _ai?.OnAttackFinished();
        // 攻撃終了時に一時停止していた移動制御を復帰させる
        _mover?.ReleaseMovementAfterAttack();
    }

    public void AnimEvent_OnStepBackFinished()
    {
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
        _mover?.EndStepBack();
        _ai?.OnAttackFinished(); // または専用の完了処理
        // 攻撃停止後は移動を復帰する
        _mover?.ReleaseMovementAfterAttack();
    }

    public void AnimEvent_OnSoundEffect(string soundName)
    {
        _audio?.PlaySE(soundName);
        CombatLog.Trace(soundName);
    }
}
