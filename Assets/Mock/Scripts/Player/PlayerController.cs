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

    [Header("PlayerStatus")]
    [SerializeField] private MeshRenderer _playerWeapon;
    [SerializeField] private GameObject _playerStartWeapon;
    [SerializeField] private Collider[] _weaponColliders;
    [SerializeField] private Collider[] _enemyWeaponColliders;

    [Header("ScriptableObject")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private PlayerStateConfig _playerStateConfig;
    [SerializeField] private PlayerPassiveBuffSet _passiveBuffSet;
    [SerializeField] private VFXConfig _vfxConfig;

    [Header("Status Effects")]
    [SerializeField] private StatusEffectDef _justAvoidSlowDef;

    // デバッグ用：入力を通して攻撃が可能かを制御。
    [SerializeField] private bool _canAttack;
    // MVP HUD
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

    /// <summary>
    /// ゲームマネージャーから呼び出される初期化メソッド。必要な各種モジュールを生成し依存を結線する。
    /// </summary>
    public void Init(InputBuffer inputBuffer, Transform enemyPosition, Camera camera
        , CameraManager cameraManager, LockOnCamera lockOnCamera)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        Rigidbody rb = GetComponent<Rigidbody>();
        _animationController = GetComponent<PlayerAnimationController>();
        _lookOnCamera = lockOnCamera;
        CharacterEffect characterEffect = GetComponent<CharacterEffect>();

        // --- 設定値（SkillGauge などの生成に使う）
        float maxGauge = _playerStatus != null ? _playerStatus.MaxSkillGauge
            : _playerStateConfig?.MaxSkillGauge ?? 100f;
        float passiveRecovery = _playerStatus != null ? _playerStatus.SkillGaugePassiveRecoveryPerSecond
            : _playerStateConfig?.SkillGaugeRecoveryPerSecond ?? 0f;

        // --- 各種コンポーネントを生成
        _playerResource = new PlayerResource(_playerStatus, _animationController);
        var ownerColliders = GetComponentsInChildren<Collider>();
        var playerWeapon = new PlayerWeapon(_weaponColliders, ownerColliders);
        var skillGauge = new SkillGauge(maxGauge, passiveRecovery);
        var skillGaugeCostConfig = _playerStatus?.SkillGaugeCost ?? new SkillGaugeCostConfig();
        var playerMover = new PlayerMover(_playerStatus, rb, this.transform, enemyPosition, camera.transform, _animationController);
        var playerSprint = new PlayerSprint(skillGauge, skillGaugeCostConfig);
        var playerGhost = new PlayerGhost(skillGauge, skillGaugeCostConfig);
        var playerHeal = new PlayerHeal(skillGauge, playerMover, skillGaugeCostConfig);
        var playerBuff = new PlayerSelfSacrifice(skillGauge, skillGaugeCostConfig);
        var playerAttacker = new PlayerAttacker(_animationController, _animationName, playerWeapon
            , _playerStatus, _passiveBuffSet, transform, _playerResource);
        _animationEventStream = new AnimationEventStream();
        _stateContext = new PlayerStateContext(this, _playerResource, skillGauge, _playerStatus, playerMover, playerSprint,
            playerGhost, playerBuff, playerHeal, _lookOnCamera, _playerStateConfig, playerAttacker
            , _animationEventStream, _vfxConfig, characterEffect);
        _stateMachine = new PlayerStateMachine(_stateContext);
        playerAttacker.SetContext(_stateContext);

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
        // ジャスト回避ウィンドウ内であればダメージを無効化し、ジャスト回避スタックを加算する。
        if (_stateContext?.IsInJustAvoidWindow ?? false)
        {
            // ジャスト回避成功によるバフ加算。
            _stateContext.AddJustAvoidStack(1);
            // PlayerStatus に設定されているゲージボーナスを即時付与（存在すれば）
            float bonus = _playerStatus?.SkillGaugeOnJustAvoidBonus ?? 0f;
            if (bonus > 0f)
            {
                _stateContext?.SkillGauge?.Add(bonus);
            }
            // デバッグログ: ジャスト回避成功を出力
            Debug.Log($"PlayerController: JustAvoid succeeded stacks={_stateContext.JustAvoidStacks} bonus={bonus}");
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
            return;
        }
        // ゴーストモード中はダメージを無効化する。
        if (_stateContext?.IsGhostMode ?? false)
        {
            return;
        }
        _playerResource?.ApplyDamage(info.DamageAmount);
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }

        // Ability と SkillGauge / PlayerResource の Dispose
        _stateContext?.Sprint?.Dispose();
        _stateContext?.Ghost?.Dispose();
        _stateContext?.SelfSacrifice?.Dispose();
        _stateContext?.Healer?.Dispose();
        _stateContext?.SkillGauge?.Dispose();
        _stateContext?.Attacker?.Dispose();
        _stateMachine?.Dispose();
        _playerResource?.Dispose();
        _stateContext?.Dispose();

        _stateMachine = null;
        _stateContext = null;

        _animationEventStream?.Dispose();
        _animationEventStream = null;
    }

    private void Update()
    {
        if (_stateContext?.Mover != null && _lookOnCamera != null)
        {
            // ロックオン方向を都度更新し、移動計算へ反映。
            _stateContext.Mover.LockOnDirection(_lookOnCamera.IsLockOn, _lookOnCamera.ReturnLockOnDirection());
        }
        // Ability レイヤを先に Tick（Ghost / SelfSacrifice の継続処理）
        _stateContext?.AbilityManager?.Tick(Time.deltaTime);
        _stateMachine?.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _stateMachine?.FixedUpdate(Time.fixedDeltaTime);
    }

    private void HandleSelfSacrificeTick(float deltaSeconds)
    {
        // deltaSeconds：このフレームの経過秒（Ability が通知）
        // SelfSacrifice の秒あたり%値は SkillGaugeCost 側で管理（未設定時は 1%/s をフォールバック）
        float percentPerSecond = _playerStatus?.SkillGaugeCost?.SelfSacrificeDamagePercentPerSecond ?? 1f;
        float percent = percentPerSecond * deltaSeconds; // % of MaxHP
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
        _playerResource?.ApplyDamage(applied);

        // もし要求ダメージが大きく、残量が不足している場合は自動停止
        if (applied < damage)
        {
            _stateContext?.SelfSacrifice?.End();
        }
    }

    private void HandleHealTick(float healedPercent)
    {
        // healedPercent は "このフレームで回復した割合 (%)"（Ability が通知）
        _playerResource.HealByPercent(healedPercent);
    }

    /// <summary>必要な InputAction を購読する。</summary>
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

    /// <summary>購読していた InputAction を解除する。</summary>
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

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 currentInput = context.ReadValue<Vector2>();
        if (context.canceled)
        {
            currentInput = Vector2.zero;
        }

        _stateMachine?.HandleMove(currentInput);
    }

    private void OnLightAttackAction(InputAction.CallbackContext context)
    {
        if (!context.started || !_canAttack)
        {
            return;
        }

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
            Debug.Log("PlayerController: Attack input ignored, sword not ready.");
        }
        _stateMachine?.HandleLightAttack();
    }

    private void OnStrongAttackAction(InputAction.CallbackContext context)
    {
        if (!context.started || !_canAttack)
        {
            return;
        }

        TryDrawSword();
        if (_stateContext?.SelfSacrifice?.IsSacrificing ?? false)
        {
            _stateMachine?.HandleStrongAttack();
            return;
        }

        if (!_stateContext?.Attacker?.IsSwordReady ?? true) return;
        _stateMachine?.HandleStrongAttack();
    }

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
                Debug.Log("Ghost Failed to begin (hold)");
            }
        }
        else if (context.canceled)
        {
            _stateContext?.Ghost?.End();
            _stateMachine?.HandleGhostCanceled();
            Debug.Log("Ghost Ended (release)");
        }
    }

    private void OnSelfSacrificeAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            var started = _stateContext?.AbilityManager?.ToggleSelfSacrifice() ?? false;
            if (started)
            {
                _stateMachine?.HandleSelfSacrificeStarted();
                Debug.Log("SelfSacrifice Started (via AbilityManager)");
            }
            else
            {
                // Toggle returned false -> either ended or failed to start
                _stateMachine?.HandleSelfSacrificeCanceled();
                Debug.Log("SelfSacrifice Canceled/Failed (via AbilityManager)");
            }
        }
    }

    private void OnHeal(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            var started = _stateContext?.AbilityManager?.ToggleHeal() ?? false;
            if (started)
            {
                _stateMachine?.HandleHealStarted();
                Debug.Log("Heal Started (via AbilityManager)");
            }
            else
            {
                Debug.Log("Heal Failed to start (via AbilityManager)");
            }
        }
        else if (context.canceled)
        {
            // 解除入力は常にステートへ伝える
            _stateMachine?.HandleHealCanceled();
            Debug.Log("Heal Canceled");
        }
    }

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
    public void AnimEvent_EnableWeaponHitbox()
    {
        _stateContext?.Attacker?.EnableWeaponHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxEnabled);
    }

    /// <summary>アニメーションイベント（ヒットボックス無効化）で呼ばれ、武器ヒットボックスを強制的にオフにする。</summary>
    public void AnimEvent_DisableWeaponHitbox()
    {
        _stateContext?.Attacker?.DisableWeaponHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxDisabled);
    }

    /// <summary>アニメーションイベントでコンボ受付が開いたタイミングを通知する。</summary>
    public void AnimEvent_OnComboWindowOpened()
    {
        _animationEventStream?.Publish(AnimationEventType.ComboWindowOpened);
    }

    /// <summary>アニメーションイベントでコンボ受付が閉じたタイミングを通知する。</summary>
    public void AnimEvent_OnComboWindowClosed()
    {
        _animationEventStream?.Publish(AnimationEventType.ComboWindowClosed);
    }

    /// <summary>攻撃アニメーション完了を現在ステートへ伝える。</summary>
    public void AnimEvent_OnAttackFinished()
    {
        _animationEventStream?.Publish(AnimationEventType.AttackFinished);
    }

    /// <summary>抜刀アニメ完了を攻撃モジュールへ伝え、抜刀フラグを更新する。</summary>
    public void AnimEvent_OnSwordDrawCompleted()
    {
        _stateContext?.Attacker?.CompleteDrawSword();
        _animationEventStream?.Publish(AnimationEventType.SwordDrawCompleted);
    }

    public void AnimaEvent_OnSordDrawWeapon()
    {
        //武器の見た目を表示する。
        _playerWeapon.enabled = true;
        _playerStartWeapon.SetActive(false);
    }

    public void AnimEvent_OnSwordSheathing()
    {
        //武器の見た目を非表示にする。
        _playerWeapon.enabled = false;
        _playerStartWeapon.SetActive(true);
    }

    //アニメーションイベント：ジャスト回避アニメーション開始時。
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
    public void AnimEvent_OnJustAvoidFinished()
    {
        if (_animationController == null) _animationController = GetComponent<PlayerAnimationController>();
        string justAvoidBool = _animationName?.JustAvoid;
        if (!string.IsNullOrEmpty(justAvoidBool))
        {
            _animationController?.PlayBool(justAvoidBool, false);
        }
    }

    public void AnimEvent_OnSoundEffect(string soundName)
    {
        AudioManager.Instance?.PlaySE(soundName);
    }
}
