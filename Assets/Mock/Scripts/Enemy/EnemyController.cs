using Cysharp.Threading.Tasks;
using INab.VFXAssets;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Enemyの依存先を接続し、AI更新・命中・死亡・Animation Eventを統括する。死亡後の戦闘更新を遮断する。</summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour, IDamageable
{
    /// <summary>
    /// 敵の現在のHP比率を取得します。0.0f（死亡）から1.0f（満タン）の範囲で返されます。
    /// </summary>
    public float HpRatio => _health != null ? _health.CurrentHpRatio : 1f;

    /// <summary>EnemyのHP・基準攻撃力・NavMesh移動・旋回の設定参照。</summary>
    [UnityEngine.Tooltip("EnemyのHP・基準攻撃力・NavMesh移動・旋回の設定参照。")]
    [Header("Enemy Status")]
    [SerializeField] private EnemyStuts _enemyStuts;
    /// <summary>Enemy Animatorのパラメータ名を共有する設定参照。</summary>
    [UnityEngine.Tooltip("Enemy Animatorのパラメータ名を共有する設定参照。")]
    [SerializeField] private AnimationName _animName;

    /// <summary>Enemyの武器攻撃判定Collider。Animation Eventと死亡処理でON/OFFする。</summary>
    [UnityEngine.Tooltip("Enemyの武器攻撃判定Collider。Animation Eventと死亡処理でON/OFFする。")]
    [Header("Weapon")]
    [SerializeField] private Collider[] _enemyWeaponColliders;
    /// <summary>Player武器との接触を調整するために参照するCollider一覧。</summary>
    [UnityEngine.Tooltip("Player武器との接触を調整するために参照するCollider一覧。")]
    [SerializeField] private Collider[] _playerWeaponColliders;

    /// <summary>Enemy本体のAnimator。攻撃・移動・死亡・Root Motionの状態を取得する。</summary>
    [UnityEngine.Tooltip("Enemy本体のAnimator。攻撃・移動・死亡・Root Motionの状態を取得する。")]
    [Header("Attack")]
    [SerializeField] private Animator _animator;
    /// <summary>行動種類に対応する攻撃Trigger・Damageの設定一覧。</summary>
    [UnityEngine.Tooltip("行動種類に対応する攻撃Trigger・Damageの設定一覧。")]
    [SerializeField] private EnemyAttackData[] _attackData;

    /// <summary>距離帯別の行動候補・抽選重みを定義したAI設定。</summary>
    [UnityEngine.Tooltip("距離帯別の行動候補・抽選重みを定義したAI設定。")]
    [Header("AI")]
    [SerializeField] private EnemyDecisionConfig _decisionConfig;
    /// <summary>後退行動でPlayerから離れる目標距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("後退行動でPlayerから離れる目標距離（Unity単位）。")]
    [SerializeField] private float _stepBackDistance = 2f;

    /// <summary>Enemyの登録済み見た目Effectを再生するコンポーネント参照。</summary>
    [UnityEngine.Tooltip("Enemyの登録済み見た目Effectを再生するコンポーネント参照。")]
    [SerializeField] private CharacterEffect _characterEffect;
    /// <summary>旧死亡待機の設定（ミリ秒）。現行の死亡・Final Blow処理では直接参照しない。</summary>
    [UnityEngine.Tooltip("旧死亡待機の設定（ミリ秒）。現行の死亡・Final Blow処理では直接参照しない。")]
    [SerializeField] private int _enemyDeadDelay = 2000;

    private EnemyAnimationController _enemyAnimController;
    private EnemyHealth _health;
    private EnemyAttacker _attacker;
    private EnemyMover _mover;
    /// <summary>Enemy攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。</summary>
    [UnityEngine.Tooltip("Enemy攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。")]
    [SerializeField, Min(0f)] private float _attackRootMotionScale = .18f;
    private int _attackAnimationHash;
    /// <summary>Root Motionを所有する攻撃Stateが記録されているか。通常被弾の表示量の調整に使用する。</summary>
    public bool IsAttackAnimationActive => _attackAnimationHash != 0;
    /// <summary>戦闘中の有効な攻撃StateだけにRoot Motionの所有権を渡す。死亡・被弾中は開始しない。</summary>
    /// <param name="stateHash">Root Motionを所有する攻撃StateのfullPathHash。</param>
    public void BeginAnimationAttackRootMotion(int stateHash)
    {
        if (_dead || (_game != null && !_game.IsCombatActive) || (_enemyAnimController != null && _enemyAnimController.IsReacting)) return;
        _attackAnimationHash = stateHash;
        _mover?.BeginAttackRootMotion();
    }
    /// <summary>終了Stateが攻撃移動の所有者に一致する場合だけ移動停止を解除する。</summary>
    /// <param name="stateHash">終了したStateのfullPathHash。現在の所有者と一致する場合だけ解除する。</param>
    public void EndAnimationAttackRootMotion(int stateHash)
    {
        if (_attackAnimationHash != stateHash) return;
        _attackAnimationHash = 0;
        _mover?.ReleaseMovementAfterAttack();
    }
    /// <summary>戦闘中の攻撃Root Motionを物理更新へ反映する。死亡・戦闘終了時は移動受付を解除する。</summary>
    private void FixedUpdate()
    {
        if (_dead || (_game != null && !_game.IsCombatActive)) { _mover?.EndAttackRootMotion(); return; }
        _mover?.FixedUpdateAttackRootMotion();
    }
    /// <summary>攻撃Stateの所有権と被弾・攻撃による移動停止を解除する。</summary>
    private void OnDisable()
    {
        _attackAnimationHash = 0;
        _mover?.EndAttackRootMotion();
        _mover?.ReleaseMovementAfterReaction();
        _mover?.ReleaseMovementAfterAttack();
    }
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
    private bool _reactionInterruptedAttack;

    /// <summary>EnemyのHP・AI・移動・武器・演出を作成し、ゲーム進行と命中通知を一度だけ接続する。</summary>
    public void Init(Transform playerPosition, DamageNumbers damageNumbers, GameManager game, AudioManager audio, HitStopManager hitStop, FinalBlowManager finalBlow, LoadSceneManager loader, CameraManager cameraManager = null, VFXConfig vfxConfig = null)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game; _audio = audio; _finalBlow = finalBlow; _loader = loader;
        _damageNumbers = damageNumbers;
        _combatFeedback = GetComponent<CombatFeedback>();
        if (_combatFeedback != null) _combatFeedback.Init(audio, vfxConfig, cameraManager, hitStop, false);
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
            , navMeshAgent, _animator, _animName, _attackRootMotionScale);
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

    /// <summary>生成直後の初期化を一フレーム待ってCharacterEffectを再生する。破棄時は待機をキャンセルする。</summary>
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
    /// <summary>通常の命中情報をクリティカルなしの共通ダメージ処理へ渡す。</summary>
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

        float before = _health.CurrentHp;
        _health.ApplyDamage(info.DamageAmount * RunSession.EnemyDefense);
        float dealt = before - _health.CurrentHp;
        if (dealt > 0f)
        {
            bool lethal = _health.CurrentHp <= 0f;
            bool counter = !lethal && info.IsJustAvoidCounter;
            if (counter || lethal) _combatFeedback?.CancelNormalHitReaction();
            if (counter)
            {
                _attacker?.DisableWeaponHitbox();
                _pendingAction = null;
                _mover?.ReleaseMovementAfterReaction();
                _mover?.InterruptMovementAction();
                _enemyAnimController?.InterruptAttackForHitReaction();
            }
            bool animated = counter && _enemyAnimController != null && _enemyAnimController.TryPlayHitReaction(info);
            if (animated)
            {
                _reactionInterruptedAttack = true;
                _mover?.HoldMovementForReaction();
            }
            else if (counter) _ai?.OnAttackFinished();
            bool boneReaction = !lethal && !animated && !(_enemyAnimController != null && _enemyAnimController.IsReacting);
            _combatFeedback?.Hit(info, boneReaction);
            if (_combatFeedback == null) _audio?.PlaySE("Damage");
            _damageNumbers?.Show(transform.position, dealt, isCritical);
        }

        if (_health.CurrentHp <= 0f && !_dead)
        {
            EnemyDead();
        }
    }

    /// <summary>Root Motionを終了し、武器購読とHP通知の所有リソースを解放する。</summary>
    private void OnDestroy()
    {
        _mover?.EndAttackRootMotion();
        _attacker?.Dispose();
        _health?.Dispose();
    }

    /// <summary>戦闘中だけ被弾の復帰・AI判断・予約行動・移動を順に進める。死亡時は歩行表示を停止する。</summary>
    private void Update()
    {
        if (_dead || (_game != null && !_game.IsCombatActive)) { _mover?.ResetLocomotionAnimation(); return; }
        if (_enemyAnimController != null && _enemyAnimController.IsReacting)
        {
            _mover?.ResetLocomotionAnimation();
            if (_enemyAnimController.TickHitReaction(Time.deltaTime))
            {
                _mover?.ReleaseMovementAfterReaction();
                if (_reactionInterruptedAttack) _ai?.OnAttackFinished();
                _reactionInterruptedAttack = false;
            }
            else return;
        }
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

    /// <summary>死亡を確定して攻撃・AI更新・移動を停止し、既存死亡ClipとFinal Blowまたは遷移を要求する。</summary>
    private void EnemyDead()
    {
        _combatFeedback?.CancelNormalHitReaction();
        _attackAnimationHash = 0;
        _mover?.InterruptMovementAction();
        _enemyAnimController.CancelHitReaction();
        _mover?.ReleaseMovementAfterReaction();
        _enemyAnimController.PlayTrigger(_enemyAnimController.AnimName.EnemyDead);
        _dead = true;
        _pendingAction = null;
        _attacker?.DisableWeaponHitbox();
        _mover?.HoldMovementForAttack();

        _game?.WinGame();
        if (_finalBlow != null) _finalBlow.StartFinalBlow();
        else _loader?.LoadSceneAsync(_loader.SceneNameConfig.TitleScene, 2000).Forget();
    }

    /// <summary>所有中の攻撃Stateの移動をMoverへ渡す。死亡・別Stateへの切り替えではRoot Motionを解除する。</summary>
    private void OnAnimatorMove()
    {
        if (_dead || (_game != null && !_game.IsCombatActive)) { _mover?.EndAttackRootMotion(); return; }
        if (_mover != null && _mover.IsUsingAttackRootMotion)
        {
            var state = _animator.IsInTransition(0) ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != _attackAnimationHash) { _attackAnimationHash = 0; _mover.ReleaseMovementAfterAttack(); return; }
        }
        _mover?.OnAnimatorMove();
    }

    // ---------- 敵のアニメーションイベント ----------
    /// <summary>
    /// アニメーションイベント用: ヒットボックスを有効化する（攻撃有効フレームで呼ぶ）。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        if (_enemyAnimController != null && _enemyAnimController.IsHeavyReacting) return;
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
        if (_enemyAnimController != null && _enemyAnimController.IsHeavyReacting) return;
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
        if (_enemyAnimController != null && _enemyAnimController.IsHeavyReacting) return;
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
        _ai?.OnAttackFinished();
        // 攻撃終了時に一時停止していた移動制御を復帰させる
        _mover?.ReleaseMovementAfterAttack();
    }

    /// <summary>有効な後退終了Eventで移動を再開し、AIへ行動完了を通知する。死亡・大被弾中は無視する。</summary>
    public void AnimEvent_OnStepBackFinished()
    {
        if (_enemyAnimController != null && _enemyAnimController.IsHeavyReacting) return;
        if (_dead || (_game != null && !_game.IsCombatActive)) return;
        _mover?.EndStepBack();
        _ai?.OnAttackFinished(); // または専用の完了処理
        // 攻撃停止後は移動を復帰する
        _mover?.ReleaseMovementAfterAttack();
    }

    /// <summary>Clipから指定されたSEを再生し、診断用の戦闘ログへ記録する。</summary>
    public void AnimEvent_OnSoundEffect(string soundName)
    {
        _audio?.PlaySE(soundName);
        CombatLog.Trace(soundName);
    }
}
