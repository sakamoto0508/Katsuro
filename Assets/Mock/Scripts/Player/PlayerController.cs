using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Mock.UI;
using System;
using INab.VFXAssets;

/// <summary>
/// プレイヤー入力の受け口となり、各種コンポーネント・ステートマシンを初期化および更新する中枢クラス。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, IDamageable
{
    public PlayerAnimationController AnimController => _animationController;
    /// <summary>Player攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。</summary>
    [UnityEngine.Tooltip("Player攻撃Clipから取得する水平Root Motionの移動倍率。0で攻撃時の移動を抑える。")]
    [Header("Attack Root Motion")]
    [SerializeField, Min(0f)] private float _attackRootMotionScale = .25f;
    private Animator _rootMotionAnimator;
    private int _attackAnimationHash;
    /// <summary>戦闘可能なときだけ攻撃Stateのハッシュを記録し、MoverのRoot Motion受付を開始する。</summary>
    /// <param name="stateHash">Root Motionを所有する攻撃StateのfullPathHash。</param>
    public void BeginAnimationAttackRootMotion(int stateHash)
    {
        if (!CanFight) { StopAttackRootMotion(); return; }
        _attackAnimationHash = stateHash;
        _stateContext?.Mover?.BeginAttackRootMotion();
    }
    /// <summary>終了Stateが現在の所有者に一致する場合だけ攻撃Root Motionを停止する。</summary>
    /// <param name="stateHash">終了したStateのfullPathHash。別Stateの終了では所有権を解除しない。</param>
    public void EndAnimationAttackRootMotion(int stateHash)
    {
        if (_attackAnimationHash == stateHash) StopAttackRootMotion();
    }
    /// <summary>攻撃Stateの所有権とMoverの保留移動・攻撃速度を解除する。</summary>
    public void StopAttackRootMotion()
    {
        _attackAnimationHash = 0;
        _stateContext?.Mover?.EndAttackRootMotion();
    }
    /// <summary>所有中の攻撃Stateの水平移動をMoverへ渡す。戦闘終了や別Stateへの遷移では受付を解除する。</summary>
    private void OnAnimatorMove()
    {
        if (!CanFight) { StopAttackRootMotion(); return; }
        if (_rootMotionAnimator == null) _rootMotionAnimator = GetComponent<Animator>();
        var state = _rootMotionAnimator.IsInTransition(0) ? _rootMotionAnimator.GetNextAnimatorStateInfo(0) : _rootMotionAnimator.GetCurrentAnimatorStateInfo(0);
        if (_attackAnimationHash == 0 || state.fullPathHash != _attackAnimationHash) { StopAttackRootMotion(); return; }
        _stateContext?.Mover?.QueueAttackRootMotion(_rootMotionAnimator.deltaPosition, _rootMotionAnimator.speed > 0f);
    }
    /// <summary>無効化時に保留中の攻撃Root Motionを消去する。</summary>
    private void OnDisable() => StopAttackRootMotion();

    /// <summary>抜刀・納刀に合わせて表示を切り替える刀のMeshRenderer。</summary>
    [UnityEngine.Tooltip("抜刀・納刀に合わせて表示を切り替える刀のMeshRenderer。")]
    [Header("PlayerStatus")]
    [SerializeField] private MeshRenderer _playerWeapon;
    /// <summary>納刀中の刀の見た目を切り替えるGameObject参照。</summary>
    [UnityEngine.Tooltip("納刀中の刀の見た目を切り替えるGameObject参照。")]
    [SerializeField] private GameObject _playerStartWeapon;
    /// <summary>Player武器の攻撃判定Collider一覧。Animation Eventで有効・無効を切り替える。</summary>
    [UnityEngine.Tooltip("Player武器の攻撃判定Collider一覧。Animation Eventで有効・無効を切り替える。")]
    [SerializeField] private Collider[] _weaponColliders;
    /// <summary>プレイヤーの刀判定に加算する全体サイズ。刀のローカル軸。Yは長さ、X/Zは厚み。</summary>
    [Tooltip("プレイヤーの刀判定に加算する全体サイズ。刀のローカル軸。Yは長さ、X/Zは厚み。")]
    [SerializeField] private Vector3 _weaponHitboxPadding = new Vector3(.08f, .02f, .08f);
    private PlayerWeapon _weaponHitboxes;
    private Vector3 _appliedHitboxPadding;
    /// <summary>接触調整で参照するEnemy武器のCollider一覧。</summary>
    [UnityEngine.Tooltip("接触調整で参照するEnemy武器のCollider一覧。")]
    [SerializeField] private Collider[] _enemyWeaponColliders;

    /// <summary>PlayerのHP・移動速度・ゲージ・能力コストの基礎設定。</summary>
    [UnityEngine.Tooltip("PlayerのHP・移動速度・ゲージ・能力コストの基礎設定。")]
    [Header("ScriptableObject")]
    [SerializeField] private PlayerStatus _playerStatus;
    /// <summary>Player Animatorのパラメータ名を共有する設定。</summary>
    [UnityEngine.Tooltip("Player Animatorのパラメータ名を共有する設定。")]
    [SerializeField] private AnimationName _animationName;
    /// <summary>Playerの回避受付時間・幽体化・コンボClipの設定。</summary>
    [UnityEngine.Tooltip("Playerの回避受付時間・幽体化・コンボClipの設定。")]
    [SerializeField] private PlayerStateConfig _playerStateConfig;
    /// <summary>装備による攻撃倍率・加算威力・命中Effectの設定。</summary>
    [UnityEngine.Tooltip("装備による攻撃倍率・加算威力・命中Effectの設定。")]
    [SerializeField] private PlayerPassiveBuffSet _passiveBuffSet;
    /// <summary>命中・Just Avoid・回復・バフなどの見た目演出設定。</summary>
    [UnityEngine.Tooltip("命中・Just Avoid・回復・バフなどの見た目演出設定。")]
    [SerializeField] private VFXConfig _vfxConfig;

    /// <summary>Just Avoid成功時に適用するスロー状態効果の定義。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時に適用するスロー状態効果の定義。")]
    [Header("Status Effects")]
    [SerializeField] private StatusEffectDef _justAvoidSlowDef;

    // デバッグ用：入力を通して攻撃が可能かを制御。
    /// <summary>攻撃入力を受け付けるための許可フラグ。falseならLight/Heavy入力を無視する。</summary>
    [UnityEngine.Tooltip("攻撃入力を受け付けるための許可フラグ。falseならLight/Heavy入力を無視する。")]
    [SerializeField] private bool _canAttack;
    // 表示・状態・仲介を分離したゲーム内表示。
    /// <summary>PlayerのHP・ダメージ追従・ゲージを表示するHUD参照。</summary>
    [UnityEngine.Tooltip("PlayerのHP・ダメージ追従・ゲージを表示するHUD参照。")]
    [Header("UI")]
    [SerializeField] private PlayerHUDView _playerHudView;
    private PlayerHUDPresenter _playerHudPresenter;

    private InputBuffer _inputBuffer;
    private PlayerAnimationController _animationController;
    private LockOnCamera _lookOnCamera;
    private PlayerStateContext _stateContext;
    private PlayerStateMachine _stateMachine;
    private AnimationEventStream _animationEventStream;
    private PlayerResource _playerResource;
    private float _justBuffRemaining;
    public int JustStacks => _stateContext != null ? _stateContext.JustAvoidStacks : 0;
    public float JustSeconds => Mathf.Max(0f, _justBuffRemaining);
    public float Hp => _playerResource != null ? _playerResource.CurrentHp : 0f;
    public float Gauge => _stateContext != null ? _stateContext.SkillGauge.Value : 0f;
    public float JustBonus => JustStacks * (_playerStatus != null && _playerStatus.JustAvoidBuffConfig != null ? _playerStatus.JustAvoidBuffConfig.DamageMultiplierPerStack : 0f);
    public bool IsInvulnerable => (_stateContext?.IsGhostMode ?? false) || (_playerResource != null && Time.time < _playerResource.InvulnerableUntil);
    private bool CanFight => _playerResource != null && !_playerResource.IsDead
        && (_game == null || _game.IsCombatActive);
    /// <summary>能力・攻撃・演出を終了させ、復活後の通常移動状態へ戻す。</summary>
    private void OnRevived()
    {
        _stateMachine.ChangeState(PlayerStateId.Locomotion);
        _stateContext.Sprint.End();
        _stateContext.Ghost.End();
        _stateContext.Healer.End();
        _stateContext.SelfSacrifice.End();
        _stateContext.ClearJustAvoidStacks();
        _stateContext.SetJustAvoidWindow(false);
        _stateContext.IsGhostMode = false;
        _stateContext.Attacker.EndAttack();
        _stateContext.SkillGauge.Add(_stateContext.SkillGauge.Max);
        _justBuffRemaining = 0f;
        _audio?.StopBGM(2);
    }

    /// <summary>
    /// ゲームマネージャーから呼び出される初期化メソッド。必要な各種モジュールを生成し依存を結線する。
    /// </summary>
    public VFXConfig FeedbackConfig => _vfxConfig;
    /// <summary>幽体化の表示状態をCombatFeedbackへ渡す。</summary>
    public void SetGhostVisual(bool active) => _combatFeedback?.SetGhost(active);
    private GameManager _game;
    private AudioManager _audio;
    private CombatFeedback _combatFeedback;
    /// <summary>バフ用BGMを専用の第2チャンネルへ要求する。</summary>
    public void PlayBuffAudio() => _audio?.PlayBGM("BuffBGM", 2, 1f);
    /// <summary>バフ用の第2BGMチャンネルを停止する。</summary>
    public void StopBuffAudio() => _audio?.StopBGM(2);
    private bool _initialized;
    /// <summary>入力・状態・移動・能力・武器・表示の依存先を組み立て、必要な通知を購読する。</summary>
    public void Init(InputBuffer inputBuffer, Transform enemyPosition, Camera camera
        , CameraManager cameraManager, LockOnCamera lockOnCamera, GameManager game, AudioManager audio, HitStopManager hitStop, PlayerDeadManager playerDead, LoadSceneManager loader)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game;
        _audio = audio;
        _combatFeedback = GetComponent<CombatFeedback>();
        if (_combatFeedback != null) _combatFeedback.Init(audio, _vfxConfig, cameraManager, hitStop);
        else Debug.LogWarning("Assign CombatFeedback on the character root. Feedback is disabled.", this);

        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        Rigidbody rb = GetComponent<Rigidbody>();
        _animationController = GetComponent<PlayerAnimationController>();
        _animationController.Init();
        foreach (var speed in GetComponentsInChildren<AnimationSpeedController>(true)) speed.Init();
        hitStop?.RegisterTarget(gameObject);
        GetComponent<StatusEffectManager>()?.Init();
        _lookOnCamera = lockOnCamera;
        CharacterEffect characterEffect = GetComponent<CharacterEffect>();

        // --- 設定値（SkillGauge などの生成に使う）
        float maxGauge = _playerStatus != null ? _playerStatus.MaxSkillGauge
            : _playerStateConfig?.MaxSkillGauge ?? 100f;
        float passiveRecovery = _playerStatus != null ? _playerStatus.SkillGaugePassiveRecoveryPerSecond
            : _playerStateConfig?.SkillGaugeRecoveryPerSecond ?? 0f;

        // --- 各種コンポーネントを生成
        _playerResource = new PlayerResource(_playerStatus, _animationController);
        _playerResource.Init(game, audio, playerDead, loader);
        var ownerColliders = GetComponentsInChildren<Collider>();
        var playerWeapon = new PlayerWeapon(_weaponColliders, ownerColliders);
        playerWeapon.Init(_weaponHitboxPadding);
        _weaponHitboxes = playerWeapon;
        _appliedHitboxPadding = _weaponHitboxPadding;
        var skillGauge = new SkillGauge(maxGauge, passiveRecovery);
        var skillGaugeCostConfig = _playerStatus?.SkillGaugeCost ?? new SkillGaugeCostConfig();
        var playerMover = new PlayerMover(_playerStatus, rb, this.transform, enemyPosition, camera.transform, _animationController, _attackRootMotionScale);
        var playerSprint = new PlayerSprint(skillGauge, skillGaugeCostConfig);
        var playerGhost = new PlayerGhost(skillGauge, skillGaugeCostConfig);
        var playerHeal = new PlayerHeal(skillGauge, playerMover, skillGaugeCostConfig);
        var playerBuff = new PlayerSelfSacrifice(skillGauge, skillGaugeCostConfig);
        var playerAttacker = new PlayerAttacker(_animationController, _animationName, playerWeapon
            , _playerStatus, _passiveBuffSet, transform, _playerResource);
        playerAttacker.Init(game, hitStop);
        _animationEventStream = new AnimationEventStream();
        _stateContext = new PlayerStateContext(this, _playerResource, skillGauge, _playerStatus, playerMover, playerSprint,
            playerGhost, playerBuff, playerHeal, _lookOnCamera, _playerStateConfig, playerAttacker
            , _animationEventStream, _vfxConfig, characterEffect);
        _stateMachine = new PlayerStateMachine(_stateContext);
        playerAttacker.SetContext(_stateContext);
        _playerResource.Revived += OnRevived;

        // HUD プレゼンターを生成（Inspector に View を割り当てている場合）
        if (_playerHudView != null)
        {
            _playerHudPresenter = new PlayerHUDPresenter(
                _playerHudView,
                _playerResource.CurrentHpReactive,
                _playerResource.MaxHp,
                skillGauge.NormalizedReactive);
        }

        // 定期処理の購読登録（Ability の通知を受けて PlayerResource を操作する）
        _stateContext.SelfSacrifice.OnConsumed
            .Subscribe(deltaSeconds => HandleSelfSacrificeTick(deltaSeconds))
            .AddTo(this);
        _stateContext.Healer.OnConsumed
            .Subscribe(percent => HandleHealTick(percent))
            .AddTo(this);

        //武器の見た目を最初は非表示にする。
        _playerWeapon.enabled = false;
        _playerStartWeapon.SetActive(true);
    }

    /// <summary>ダメージを適用する。</summary>
    public void ApplyDamage(DamageInfo info)
    {
        if (!CanFight || info.DamageAmount <= 0f || Time.time < _playerResource.InvulnerableUntil) return;
        // ジャスト回避ウィンドウ内であればダメージを無効化し、ジャスト回避スタックを加算する。
        if (_stateContext?.IsInJustAvoidWindow ?? false)
        {
            // ジャスト回避成功によるバフ加算。
            _stateContext.AddJustAvoidStack(1);
            _stateContext.SetJustAvoidWindow(false);
            _justBuffRemaining = Mathf.Max(0.01f, GameplayRules.Current.JustBuffDuration);
            // PlayerStatus に設定されているゲージボーナスを即時付与（存在すれば）
            float bonus = _playerStatus?.SkillGaugeOnJustAvoidBonus ?? 0f;
            if (bonus > 0f)
            {
                _stateContext?.SkillGauge?.Add(bonus);
            }
            // デバッグログ: ジャスト回避成功を出力
            CombatLog.Trace($"PlayerController: JustAvoid succeeded stacks={_stateContext.JustAvoidStacks} bonus={bonus}");
            _animationController?.PlayTrigger(_animationName?.JustAvoidWindow);
            // ジャスト回避スロウ効果を付与する。
            var instigator = info.Instigator;
            if (instigator != null && _justAvoidSlowDef != null)
            {
                var receiver = instigator.GetComponentInParent<IStatusEffectReceiver>();
                if (receiver != null)
                {
                    receiver.ApplyStatusEffect(new StatusEffectInstance(_justAvoidSlowDef, this.gameObject));
                }
            }
            _combatFeedback?.PlayJustAvoidFeedback(info);
            return;
        }
        // ゴーストモード中はダメージを無効化する。
        if (_stateContext?.IsGhostMode ?? false)
        {
            _stateContext.SkillGauge.Add(_playerStatus != null ? _playerStatus.SkillGaugeOnAvoidGain : 5f);
            return;
        }
        _combatFeedback?.Hit(info);
        _playerResource?.ApplyDamage(info.DamageAmount * RunSession.IncomingMultiplier, _combatFeedback == null);
        if (!CanFight) StopAttackRootMotion();
    }

    /// <summary>Root Motionと入力購読を停止し、Playerが所有する状態・能力・武器のリソースを解放する。</summary>
    private void OnDestroy()
    {
        StopAttackRootMotion();
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }

        // Ability と SkillGauge / PlayerResource の Dispose
        _playerHudPresenter?.Dispose();
        _stateMachine?.Dispose();
        _stateMachine = null;
        _stateContext?.Sprint?.Dispose();
        _stateContext?.Ghost?.Dispose();
        _stateContext?.SelfSacrifice?.Dispose();
        _stateContext?.Healer?.Dispose();
        _stateContext?.SkillGauge?.Dispose();
        _stateContext?.Attacker?.Dispose();
        _playerResource?.Dispose();
        _stateContext?.Dispose();

        _stateContext = null;

        _animationEventStream?.Dispose();
        _animationEventStream = null;
    }

    /// <summary>Inspector変更の刀判定を反映し、戦闘可能な間だけ状態と能力のフレーム更新を進める。</summary>
    private void Update()
    {
        if (_weaponHitboxes != null && _appliedHitboxPadding != _weaponHitboxPadding)
        {
            _weaponHitboxes.SetHitboxPadding(_weaponHitboxPadding);
            _appliedHitboxPadding = _weaponHitboxPadding;
        }
        if (!CanFight) { StopAttackRootMotion(); return; }
        if (_justBuffRemaining > 0f)
        {
            _justBuffRemaining -= Time.deltaTime;
            if (_justBuffRemaining <= 0f) _stateContext.ClearJustAvoidStacks();
        }
        if (_stateContext?.Mover != null && _lookOnCamera != null)
        {
            // ロックオン方向を都度更新し、移動計算へ反映。
            _stateContext.Mover.LockOnDirection(_lookOnCamera.IsLockOn, _lookOnCamera.ReturnLockOnDirection());
        }
        // Ability レイヤを先に Tick（Ghost / SelfSacrifice の継続処理）
        _stateContext?.AbilityManager?.Tick(Time.deltaTime);
        _stateMachine?.Update(Time.deltaTime);
    }

    /// <summary>戦闘可能な間だけ攻撃Root Motionと現在状態の物理更新を進める。</summary>
    private void FixedUpdate()
    {
        if (!CanFight) { StopAttackRootMotion(); return; }
        _stateContext?.Mover?.FixedUpdateAttackRootMotion(_rootMotionAnimator != null && _rootMotionAnimator.speed > 0f);
        _stateMachine?.FixedUpdate(Time.fixedDeltaTime);
    }

    /// <summary>自傷能力から受けた経過秒を最大HPの割合へ換算し、音を重ねずHP消費へ反映する。</summary>
    private void HandleSelfSacrificeTick(float deltaSeconds)
    {
        // deltaSeconds：このフレームの経過秒（Ability が通知）
        // SelfSacrifice の秒あたり%値は SkillGaugeCost 側で管理（未設定時は 1%/s をフォールバック）
        float percentPerSecond = _playerStatus?.SkillGaugeCost?.SelfSacrificeDamagePercentPerSecond ?? 1f;
        float percent = percentPerSecond * deltaSeconds; // 最大体力に対する割合。
        float damage = _playerResource.MaxHp * (percent / 100f);

        // 最小HP保護: SelfSacrificeMinHpRatio を超えないように分割適用または自動停止
        float currentHp = _playerResource?.CurrentHp ?? 0f;
        float minHpRatio = _playerStatus?.SkillGaugeCost?.SelfSacrificeMinHpRatio ?? 0.1f;
        float minHp = _playerResource != null ? _playerResource.MaxHp * minHpRatio : 0f;

        // 適用可能な最大ダメージ（現HP を minHp までしか減らさない）
        float maxAllowedDamage = Mathf.Max(0f, currentHp - minHp);

        if (maxAllowedDamage <= Mathf.Epsilon)
        {
            // 最低HPに到達しているのでチャネリングを停止する
            _stateContext?.SelfSacrifice?.End();
            return;
        }

        float applied = Mathf.Min(damage, maxAllowedDamage);
        // SelfSacrifice による毎フレームのダメージは効果音を鳴らさない
        _playerResource?.ApplyDamage(applied, false);

        // もし要求ダメージが大きく、残量が不足している場合は自動停止
        if (applied < damage)
        {
            _stateContext?.SelfSacrifice?.End();
        }
    }

    /// <summary>回復能力から通知された割合をPlayerResourceのHP回復へ反映する。</summary>
    private void HandleHealTick(float healedPercent)
    {
        // healedPercent は "このフレームで回復した割合 (%)"（Ability が通知）
        _playerResource.HealByPercent(healedPercent);
    }

    /// <summary>必要な 入力Action を購読する。</summary>
    private void InputEventRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.MoveAction.performed += OnMove;
        inputBuffer.MoveAction.canceled += OnMove;
        inputBuffer.LightAttackAction.started += OnLightAttackAction;
        inputBuffer.StrongAttackAction.started += OnStrongAttackAction;
        inputBuffer.GhostAction.started += OnGhostAction;
        inputBuffer.GhostAction.canceled += OnGhostAction;
        inputBuffer.BuffAction.started += OnSelfSacrificeAction;
        inputBuffer.HealAction.started += OnHeal;
        inputBuffer.HealAction.canceled += OnHeal;
        inputBuffer.SprintAction.started += OnSprint;
        inputBuffer.SprintAction.canceled += OnSprint;
    }

    /// <summary>購読していた 入力Action を解除する。</summary>
    private void InputEventUnRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.MoveAction.performed -= OnMove;
        inputBuffer.MoveAction.canceled -= OnMove;
        inputBuffer.LightAttackAction.started -= OnLightAttackAction;
        inputBuffer.StrongAttackAction.started -= OnStrongAttackAction;
        inputBuffer.GhostAction.started -= OnGhostAction;
        inputBuffer.GhostAction.canceled -= OnGhostAction;
        inputBuffer.BuffAction.started -= OnSelfSacrificeAction;
        inputBuffer.HealAction.started -= OnHeal;
        inputBuffer.HealAction.canceled -= OnHeal;
        inputBuffer.SprintAction.started -= OnSprint;
        inputBuffer.SprintAction.canceled -= OnSprint;
    }

    /// <summary>移動入力の開始・変更・解除を読み取り、解除時はゼロ入力を状態管理へ渡す。</summary>
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 currentInput = context.ReadValue<Vector2>();
        if (context.canceled)
        {
            currentInput = Vector2.zero;
        }

        _stateMachine?.HandleMove(currentInput);
    }

    /// <summary>有効な弱攻撃開始入力で抜刀状態を確認し、現在状態の攻撃・コンボ受付へ渡す。</summary>
    private void OnLightAttackAction(InputAction.CallbackContext context)
    {
        if (!context.started || !_canAttack)
        {
            return;
        }

        if (_stateContext != null && _stateContext.IsGhostMode) _stateMachine.ChangeState(PlayerStateId.Locomotion);
        TryDrawSword();
        // SelfSacrifice 中は攻撃を即時遷移させる（抜刀が未完でもステートを切り替え、攻撃中に抜刀完了を待つ）
        if (_stateContext?.SelfSacrifice?.IsSacrificing ?? false)
        {
            _stateMachine?.HandleLightAttack();
            return;
        }

        if (!_stateContext?.Attacker?.IsSwordReady ?? true) return;
        if (!_stateContext.Attacker.IsSwordReady)
        {
            CombatLog.Trace("PlayerController: Attack input ignored, sword not ready.");
        }
        _stateMachine?.HandleLightAttack();
    }

    /// <summary>有効な強攻撃開始入力で抜刀状態を確認し、現在状態の強攻撃・コンボ受付へ渡す。</summary>
    private void OnStrongAttackAction(InputAction.CallbackContext context)
    {
        if (!context.started || !_canAttack)
        {
            return;
        }

        if (_stateContext != null && _stateContext.IsGhostMode) _stateMachine.ChangeState(PlayerStateId.Locomotion);
        TryDrawSword();
        if (_stateContext?.SelfSacrifice?.IsSacrificing ?? false)
        {
            _stateMachine?.HandleStrongAttack();
            return;
        }

        if (!_stateContext?.Attacker?.IsSwordReady ?? true) return;
        _stateMachine?.HandleStrongAttack();
    }

    /// <summary>幽体化能力の開始・解除を入力に応じて処理し、状態と表示を接続する。</summary>
    private void OnGhostAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            bool began = _stateContext?.Ghost?.TryBegin() ?? false;
            if (began)
            {
                _stateMachine?.HandleGhostStarted();
            }
            else
            {
                CombatLog.Trace("Ghost Failed to begin (hold)");
            }
        }
        else if (context.canceled)
        {
            _stateContext?.Ghost?.End();
            _stateMachine?.HandleGhostCanceled();
            CombatLog.Trace("Ghost Ended (release)");
        }
    }

    /// <summary>自傷能力の開始・解除結果を状態管理とバフ表示へ反映する。</summary>
    private void OnSelfSacrificeAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            var started = _stateContext?.AbilityManager?.ToggleSelfSacrifice() ?? false;
            if (started)
            {
                _stateMachine?.HandleSelfSacrificeStarted();
                CombatLog.Trace("SelfSacrifice Started (via AbilityManager)");
            }
            else
            {
                // 切り替え結果が無効なら、終了したか開始に失敗している。
                _stateMachine?.HandleSelfSacrificeCanceled();
                CombatLog.Trace("SelfSacrifice Canceled/Failed (via AbilityManager)");
            }
        }
    }

    /// <summary>回復能力の開始・解除を入力に応じて要求し、対応するPlayer状態へ通知する。</summary>
    private void OnHeal(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            var started = _stateContext?.AbilityManager?.ToggleHeal() ?? false;
            if (started)
            {
                _stateMachine?.HandleHealStarted();
                CombatLog.Trace("Heal Started (via AbilityManager)");
            }
            else
            {
                CombatLog.Trace("Heal Failed to start (via AbilityManager)");
            }
        }
        else if (context.canceled)
        {
            // 解除入力は常にステートへ伝える
            _stateMachine?.HandleHealCanceled();
            CombatLog.Trace("Heal Canceled");
        }
    }

    /// <summary>Dash入力の開始と解除を現在状態へ転送する。</summary>
    private void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _stateMachine?.HandleSprintStarted();
        }
        else if (context.canceled)
        {
            _stateMachine?.HandleSprintCanceled();
        }
    }

    /// <summary>抜刀状態でなければ抜刀アニメを開始する。</summary>
    private void TryDrawSword()
    {
        var attacker = _stateContext?.Attacker;
        if (attacker == null) return;

        if (!attacker.IsSwordReady && !attacker.IsDrawingSword)
        {
            attacker.DrawSword();
        }
    }

    //＝＝＝＝＝＝＝＝ アニメーションイベント＝＝＝＝＝＝＝＝＝＝＝

    /// <summary>
    /// アニメーションイベント（ヒットボックス有効化）から呼ばれ、ゴースト中なら無効化を維持、それ以外は武器ヒットボックスを有効化する。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox(AnimationEvent animationEvent)
    {
        if (!NormalAttackSpeedState.AcceptGameplayEvent(GetComponent<Animator>(), animationEvent)) return;
        _stateContext?.Attacker?.EnableWeaponHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxEnabled);
    }

    /// <summary>アニメーションイベント（ヒットボックス無効化）で呼ばれ、武器ヒットボックスを強制的にオフにする。</summary>
    public void AnimEvent_DisableWeaponHitbox(AnimationEvent animationEvent)
    {
        if (!NormalAttackSpeedState.AcceptGameplayEvent(GetComponent<Animator>(), animationEvent)) return;
        _stateContext?.Attacker?.DisableWeaponHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxDisabled);
    }

    /// <summary>アニメーションイベントでコンボ受付が開いたタイミングを通知する。</summary>
    public void AnimEvent_OnComboWindowOpened(AnimationEvent animationEvent)
    {
        if (!NormalAttackSpeedState.AcceptGameplayEvent(GetComponent<Animator>(), animationEvent)) return;
        _animationEventStream?.Publish(AnimationEventType.ComboWindowOpened);
    }

    /// <summary>アニメーションイベントでコンボ受付が閉じたタイミングを通知する。</summary>
    public void AnimEvent_OnComboWindowClosed(AnimationEvent animationEvent)
    {
        if (!NormalAttackSpeedState.AcceptGameplayEvent(GetComponent<Animator>(), animationEvent)) return;
        _animationEventStream?.Publish(AnimationEventType.ComboWindowClosed);
    }

    /// <summary>攻撃アニメーション完了を現在ステートへ伝える。</summary>
    public void AnimEvent_OnAttackFinished(AnimationEvent animationEvent)
    {
        if (!NormalAttackSpeedState.AcceptGameplayEvent(GetComponent<Animator>(), animationEvent)) return;
        _animationEventStream?.Publish(AnimationEventType.AttackFinished);
    }

    /// <summary>抜刀アニメ完了を攻撃モジュールへ伝え、抜刀フラグを更新する。</summary>
    public void AnimEvent_OnSwordDrawCompleted()
    {
        _stateContext?.Attacker?.CompleteDrawSword();
        _animationEventStream?.Publish(AnimationEventType.SwordDrawCompleted);
    }

    /// <summary>抜刀ClipのEventに合わせ、使用中の刀モデルを表示して納刀側モデルを隠す。</summary>
    public void AnimaEvent_OnSordDrawWeapon()
    {
        //武器の見た目を表示する。
        _playerWeapon.enabled = true;
        _playerStartWeapon.SetActive(false);
    }

    /// <summary>納刀ClipのEventに合わせ、使用中の刀モデルを隠して鞘側モデルへ切り替える。</summary>
    public void AnimEvent_OnSwordSheathing()
    {
        //武器の見た目を非表示にする。
        _playerWeapon.enabled = false;
        _playerStartWeapon.SetActive(true);
    }

    //アニメーションイベント：ジャスト回避アニメーション開始時。
    /// <summary>ジャスト回避Clip開始Eventで専用Animator Boolを有効にする。</summary>
    public void AnimEvent_OnJustAvoidStarted()
    {
        if (_animationController == null) _animationController = GetComponent<PlayerAnimationController>();
        string justAvoidBool = _animationName?.JustAvoid;
        if (!string.IsNullOrEmpty(justAvoidBool))
        {
            _animationController?.PlayBool(justAvoidBool, true);
        }
    }

    //アニメーションイベント：ジャスト回避アニメーション終了時
    /// <summary>ジャスト回避Clip終了Eventで専用Animator Boolを解除する。</summary>
    public void AnimEvent_OnJustAvoidFinished()
    {
        if (_animationController == null) _animationController = GetComponent<PlayerAnimationController>();
        string justAvoidBool = _animationName?.JustAvoid;
        if (!string.IsNullOrEmpty(justAvoidBool))
        {
            _animationController?.PlayBool(justAvoidBool, false);
        }
    }

    /// <summary>Clipが指定した登録名のSEをAudioManagerへ要求する。</summary>
    public void AnimEvent_OnSoundEffect(string soundName)
    {
        _audio?.PlaySE(soundName);
    }
}
