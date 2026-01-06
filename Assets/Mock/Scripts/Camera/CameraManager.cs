using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private CinemachineCamera _camera;
    private LockOnCamera _lockOnCamera;
    private LockOnCameraMover _lockOnCameraMover;
    public void Init(InputBuffer inputBuffer, Transform playerPosition
        , Transform enemyPosition, CameraConfig config)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _camera = GetComponent<CinemachineCamera>();
        _lockOnCameraMover = new LockOnCameraMover(_lockOnCamera, playerPosition
            , enemyPosition, _camera, config);
    }

    public void SetLockOnCamera(LockOnCamera lockOnCamera)
    {
        _lockOnCamera = lockOnCamera;
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
        _lockOnCameraMover?.Update();
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
