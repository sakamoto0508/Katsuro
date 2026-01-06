using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private CinemachineCamera _camera;
    private LockOnCamera _lookOnCamera; 
    private LockOnCameraMover _lockOnCameraMover;
    public void Init(InputBuffer inputBuffer, Transform playerPosition
        , Transform enemyPosition, CameraConfig config,LockOnCamera lockOnCamera)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _camera = GetComponent<CinemachineCamera>();
        _lookOnCamera = lockOnCamera;
        _lockOnCameraMover = new LockOnCameraMover(lockOnCamera, playerPosition
            , enemyPosition, _camera, config);
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
        if (_lookOnCamera.IsLockOn == false)
        {
            _lookOnCamera?.LockOn();
            Debug.Log("LockOn");
        }
        else
        {
            _lookOnCamera?.UnLockOn();
            Debug.Log("UnLockOn");
        }
    }
}
