using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private PlayerMover _playerMover;
    private Vector2 _currentInput;

    /// <summary>
    /// ゲームマネージャーで呼ばれるAwakeの代替メソッド
    /// </summary>
    /// <param name="inputBuffer"></param>
    public void Init(InputBuffer inputBuffer)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _playerMover = new PlayerMover();
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
    }

    private void InputEventRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.MoveAction.performed += OnMove;
        inputBuffer.MoveAction.canceled += OnMove;
    }

    private void InputEventUnRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.MoveAction.performed -= OnMove;
        inputBuffer.MoveAction.canceled -= OnMove;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _currentInput = context.ReadValue<Vector2>();
        if (context.performed)
        {

        }
        else if (context.canceled)
        {
            _currentInput = Vector2.zero;
        }
    }
}
