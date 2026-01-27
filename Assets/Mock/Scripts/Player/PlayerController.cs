using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー入力の受け口となり、各種コンポーネント・ステートマシンを初期化および更新する中枢クラス。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("PlayerStatus")]
    [SerializeField] private Collider[] _weaponColliders;
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private PlayerStateConfig _playerStateConfig;
    [SerializeField] private PlayerPassiveBuffSet _passiveBuffSet;

    // デバッグ用：入力を通して攻撃が可能かを制御。
    [SerializeField] private bool _canAttack;

    private InputBuffer _inputBuffer;
    private PlayerAnimationController _animationController;
    private LockOnCamera _lookOnCamera;
    private PlayerWeapon _playerWeapon;
    private PlayerMover _playerMover;
    private PlayerSprint _playerSprint;
    private PlayerAttacker _playerAttacker;
    private PlayerStateContext _stateContext;
    private PlayerStateMachine _stateMachine;
    private AnimationEventStream _animationEventStream;

    /// <summary>
    /// ゲームマネージャーから呼び出される初期化メソッド。必要な各種モジュールを生成し依存を結線する。
    /// </summary>
    public void Init(InputBuffer inputBuffer, Transform enemyPosition
        , Camera camera, CameraManager cameraManager
        , LockOnCamera lockOnCamera)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        Rigidbody rb = GetComponent<Rigidbody>();
        _animationController = GetComponent<PlayerAnimationController>();

        // クラス生成
        _playerWeapon = new PlayerWeapon(_weaponColliders);
        _playerSprint = new PlayerSprint(_playerStateConfig);
        _playerMover = new PlayerMover(_playerStatus, rb, this.transform
            , camera.transform, _animationController);
        _lookOnCamera = lockOnCamera;
        _playerAttacker = new PlayerAttacker(_animationController, _animationName, _playerWeapon,
            _playerStatus, _passiveBuffSet, transform);
        _animationEventStream = new AnimationEventStream();
        _stateContext = new PlayerStateContext(this, _playerStatus, _playerMover, _playerSprint,
            _lookOnCamera, _playerStateConfig, _playerAttacker, _animationEventStream);
        _stateMachine = new PlayerStateMachine(_stateContext);
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }

        _stateMachine?.Dispose();
        _stateMachine = null;
        _stateContext = null;

        _playerAttacker?.Dispose();
        _playerAttacker = null;

        _animationEventStream?.Dispose();
        _animationEventStream = null;
    }

    private void Update()
    {
        if (_playerMover != null && _lookOnCamera != null)
        {
            // ロックオン方向を都度更新し、移動計算へ反映。
            _playerMover.LockOnDirection(_lookOnCamera.IsLockOn, _lookOnCamera.ReturnLockOnDirection());
        }
        _stateMachine?.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _stateMachine?.FixedUpdate(Time.fixedDeltaTime);
    }

    /// <summary>必要な InputAction を購読する。</summary>
    private void InputEventRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.MoveAction.performed += OnMove;
        inputBuffer.MoveAction.canceled += OnMove;
        inputBuffer.LightAttackAction.started += OnLightAttackAction;
        inputBuffer.StrongAttackAction.started += OnStrongAttackAction;
        inputBuffer.EvasionAction.started += OnEvasionAction;
        inputBuffer.EvasionAction.canceled += OnEvasionAction;
        inputBuffer.BuffAction.started += OnBuffAction;
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
        inputBuffer.EvasionAction.started -= OnEvasionAction;
        inputBuffer.EvasionAction.canceled -= OnEvasionAction;
        inputBuffer.BuffAction.started -= OnBuffAction;
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
        if (!_playerAttacker.IsSwordReady) return;
        _stateMachine?.HandleLightAttack();
    }

    private void OnStrongAttackAction(InputAction.CallbackContext context)
    {
        if (!context.started || !_canAttack)
        {
            return;
        }

        TryDrawSword();
        if (!_playerAttacker.IsSwordReady) return;
        _stateMachine?.HandleStrongAttack();
    }

    private void OnEvasionAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {

        }
        else
        {
                
        }
    }

    private void OnBuffAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {

        }
        else
        {

        }
    }

    private void OnHeal(InputAction.CallbackContext context)
    {
        if(context.started)
        {

        }
        else
        {

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
        if (_playerAttacker == null)
        {
            return;
        }

        if (!_playerAttacker.IsSwordReady && !_playerAttacker.IsDrawingSword)
        {
            _playerAttacker.DrawSword();
        }
    }

    //＝＝＝＝＝＝＝＝ アニメーションイベント＝＝＝＝＝＝＝＝＝＝＝

    /// <summary>
    /// アニメーションイベント（ヒットボックス有効化）から呼ばれ、ゴースト中なら無効化を維持、それ以外は武器ヒットボックスを有効化する。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        if (_stateContext != null && _stateContext.IsGhostMode)
        {
            _playerAttacker?.DisableWeaponHitbox();
            _animationEventStream?.Publish(AnimationEventType.WeaponHitboxDisabled);
            return;
        }

        _playerAttacker?.EnableWeaponHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxEnabled);
    }

    /// <summary>アニメーションイベント（ヒットボックス無効化）で呼ばれ、武器ヒットボックスを強制的にオフにする。</summary>
    public void AnimEvent_DisableWeaponHitbox()
    {
        _playerAttacker?.DisableWeaponHitbox();
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
        _playerAttacker?.CompleteDrawSword();
        _animationEventStream?.Publish(AnimationEventType.SwordDrawCompleted);
    }
}
