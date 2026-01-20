using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("PlayerStatus")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private PlayerStateConfig _playerStateConfig;

    //テスト用フラグ
    [SerializeField] private bool _canAttack;

    private InputBuffer _inputBuffer;
    private PlayerAnimationController _animationController;
    private LockOnCamera _lookOnCamera;
    private PlayerMover _playerMover;
    private PlayerSprint _playerSprint;
    private PlayerAttacker _playerAttacker;
    private PlayerStateContext _stateContext;
    private PlayerStateMachine _stateMachine;

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
        _playerSprint = new PlayerSprint(_playerStateConfig);
        _playerMover = new PlayerMover(_playerStatus, rb, this.transform
            , camera.transform, _animationController);
        _lookOnCamera = lockOnCamera;
        _playerAttacker = new PlayerAttacker(_animationController, _animationName);
        _stateContext = new PlayerStateContext(this, _playerStatus, _playerMover, _playerSprint, _lookOnCamera);
        _stateMachine = new PlayerStateMachine(_stateContext);
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
        _stateMachine = null;
        _stateContext = null;
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
        if (_playerAttacker.IsDrawingSword == false && _canAttack)
        {
            _playerAttacker.DrawSword();
        }
    }

    private void OnStrongAttackAction(InputAction.CallbackContext context)
    {

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
}
