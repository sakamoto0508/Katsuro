using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private PlayerController _playerController;

    [Header("Enemy")]
    [SerializeField] private Transform _enemyPosition;

    [Header("Camera")]
    [SerializeField] private CameraConfig _cameraConfig;
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController _inputCamera;
    [SerializeField] private CinemachineBrain _cinemachineBrain;

    private LockOnCamera _lockOnCamera;

    private void Awake()
    {

    }

    private void Start()
    {
        Init();
    }


    private void Init()
    {
        _lockOnCamera = new LockOnCamera(_playerPosition, _enemyPosition
            , _cinemachineCamera, _cinemachineBrain);
        _playerController?.Init(_inputBuffer, _enemyPosition, _cinemachineCamera
            , _cameraManager, _lockOnCamera);
        _cameraManager?.Init(_inputBuffer, _playerPosition
            , _enemyPosition, _cameraConfig, _lockOnCamera, _camera);
    }
}

