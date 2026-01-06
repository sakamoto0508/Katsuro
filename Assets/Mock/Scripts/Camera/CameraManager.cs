using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private LockOnCameraMover _lockOnCamera;
    public void Init(InputBuffer inputBuffer)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _lockOnCamera = new LockOnCameraMover();
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
        inputBuffer.LookOnAction.started += OnLookOnAction;
    }

    private void InputEventUnRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.LookOnAction.started -= OnLookOnAction;
    }

    private void OnLookOnAction(InputAction.CallbackContext context)
    {

    }
}
