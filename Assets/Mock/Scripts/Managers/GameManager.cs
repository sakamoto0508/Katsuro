using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerAnimationController _playerAnimationController;

    [Header("Config")]
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private CameraConfig _cameraConfig;

    [Header("Enemy")]
    [SerializeField] private Transform _enemyPosition;
    [SerializeField] private EnemyController _enemyController;

    [Header("Camera")]
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineCamera _cinemachineLockOncamera;
    [SerializeField] private CinemachineInputAxisController _inputCamera;

    private LockOnCamera _lockOnCamera;

    private void Start()
    {
        Init();
    }


    private void Init()
    {
        _lockOnCamera = new LockOnCamera(_playerPosition, _enemyPosition
            , _cinemachineCamera, _cinemachineLockOncamera, _playerAnimationController,_animationName);
        _playerController?.Init(_inputBuffer, _enemyPosition, _camera
            , _cameraManager, _lockOnCamera);
        _enemyController?.Init(_playerPosition);
        _cameraManager?.Init(_inputBuffer, _playerPosition
            , _enemyPosition, _cameraConfig, _lockOnCamera, _cinemachineCamera);
    }
}

