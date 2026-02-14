using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private LockOnCamera _lookOnCamera;
    private LockOnCameraMover _lockOnCameraMover;
    public void Init(InputBuffer inputBuffer, Transform playerPosition
        , Transform enemyPosition, CameraConfig config, LockOnCamera lockOnCamera, CinemachineCamera camera)
    {
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _lookOnCamera = lockOnCamera;
        _lockOnCameraMover = new LockOnCameraMover(lockOnCamera, playerPosition
            , enemyPosition, camera, config);
        Debug.Log($"CameraManager.Init: player={playerPosition?.name ?? "null"}, enemy={enemyPosition?.name ?? "null"}, camera={(camera!=null?camera.name:"null")}");
    }

    private void OnDestroy()
    {
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
    }

    private void LateUpdate()
    {
        _lockOnCameraMover?.LateUpdate();
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
        }
        else
        {
            _lookOnCamera?.UnLockOn();
        }
    }
}
