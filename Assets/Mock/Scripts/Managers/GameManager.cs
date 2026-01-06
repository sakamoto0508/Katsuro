using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private PlayerController _playerController;

    [Header("Enemy")]
    [SerializeField] private Transform _enemyPosition;

    [Header("Camera")]
    [SerializeField] private CameraConfig _cameraConfig;
    [SerializeField] private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {

    }

    private void Start()
    {
        Init();
    }


    private void Init()
    {
        _playerController?.Init(_inputBuffer, _enemyPosition
            , _cinemachineCamera, _cameraConfig);
    }
}

