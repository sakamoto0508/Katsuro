using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("PlayerStatus")]
    [SerializeField] private PlayerStatus _playerStatus;

    private InputBuffer _inputBuffer;
    private PlayerAnimationController _animationController;
    private LockOnCamera _lookOnCamera;
    private PlayerMover _playerMover;
    private PlayerAttacker _playerAttacker;

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

        _playerMover = new PlayerMover(_playerStatus, rb, this.transform
            , camera.transform,_animationController);
        _lookOnCamera = lockOnCamera;
        _playerAttacker = new PlayerAttacker();
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
    }

    private void Update()
    {
        _playerMover.LockOnDirection(_lookOnCamera.IsLockOn, _lookOnCamera.ReturnLockOnDirection());
        _playerMover?.Update();
    }

    private void FixedUpdate()
    {
        _playerMover?.FixedUpdate();
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
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 currentInput = context.ReadValue<Vector2>();
        if (context.performed)
        {
            _playerMover?.OnMove(currentInput);
        }
        else if (context.canceled)
        {
            currentInput = Vector2.zero;
            _playerMover?.OnMove(currentInput);
        }
    }

    private void OnLightAttackAction(InputAction.CallbackContext context)
    {
        if (_playerAttacker.IsDrawingSword == false)
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
}
