using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("PlayerStatus")]
    [SerializeField] private Collider[] _weaponColliders;
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private PlayerStateConfig _playerStateConfig;

    //テスト用フラグ
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
    /// ゲームマネージャーで呼ばれるAwakeの代替メソッド
    /// </summary>
    /// <param name="inputBuffer"></param>
    public void Init(InputBuffer inputBuffer, Transform enemyPosition
        , Camera camera, CameraManager cameraManager
        , LockOnCamera lockOnCamera)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        Rigidbody rb = GetComponent<Rigidbody>();
        _animationController = GetComponent<PlayerAnimationController>();

        //クラス生成
        _playerWeapon = new PlayerWeapon(_weaponColliders);
        _playerSprint = new PlayerSprint(_playerStateConfig);
        _playerMover = new PlayerMover(_playerStatus, rb, this.transform
            , camera.transform, _animationController);
        _lookOnCamera = lockOnCamera;
        _playerAttacker = new PlayerAttacker(_animationController, _animationName, _playerWeapon);
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

        _animationEventStream?.Dispose();
        _animationEventStream = null;
    }

    private void Update()
    {
        if (_playerMover != null && _lookOnCamera != null)
        {
            _playerMover.LockOnDirection(_lookOnCamera.IsLockOn, _lookOnCamera.ReturnLockOnDirection());
        }
        _stateMachine?.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _stateMachine?.FixedUpdate(Time.fixedDeltaTime);
    }

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
    }

    private void OnBuffAction(InputAction.CallbackContext context)
    {
    }

    private void OnHeal(InputAction.CallbackContext context)
    {
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
    /// 肉体攻撃アニメのヒット判定開始イベント。ゴースト中は無効化のみ行う。
    /// </summary>
    public void AnimEvent_EnableWeaponHitbox()
    {
        if (_stateContext != null && _stateContext.IsGhostMode)
        {
            _playerWeapon?.DisableHitbox();
            _animationEventStream?.Publish(AnimationEventType.WeaponHitboxDisabled);
            return;
        }

        _playerWeapon?.EnableHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxEnabled);
    }

    /// <summary>
    /// 攻撃アニメの該当フレームでヒットボックスを無効化するイベント。
    /// </summary>
    public void AnimEvent_DisableWeaponHitbox()
    {
        _playerWeapon?.DisableHitbox();
        _animationEventStream?.Publish(AnimationEventType.WeaponHitboxDisabled);
    }

    /// <summary>
    /// コンボ受付開始フレームで呼ばれ、現在ステートへ受付可能を通知する。
    /// </summary>
    public void AnimEvent_OnComboWindowOpened()
    {
        _animationEventStream?.Publish(AnimationEventType.ComboWindowOpened);
    }

    /// <summary>
    /// コンボ受付終了フレームで呼ばれ、ステートへ受付終了を通知する。
    /// </summary>
    public void AnimEvent_OnComboWindowClosed()
    {
        _animationEventStream?.Publish(AnimationEventType.ComboWindowClosed);
    }

    /// <summary>
    /// 攻撃アニメの最後で呼び出し、ステート側の攻撃完了処理をトリガーする。
    /// </summary>
    public void AnimEvent_OnAttackFinished()
    {
        _animationEventStream?.Publish(AnimationEventType.AttackFinished);
    }

    /// <summary>
    /// 抜刀アニメの完了イベント。攻撃準備完了としてフラグを更新する。
    /// </summary>
    public void AnimEvent_OnSwordDrawCompleted()
    {
        _playerAttacker?.CompleteDrawSword();
        _animationEventStream?.Publish(AnimationEventType.SwordDrawCompleted);
    }
}
