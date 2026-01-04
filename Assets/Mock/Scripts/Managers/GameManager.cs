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
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CameraConfig _cameraConfig;

    private void Awake()
    {

    }

    private void Start()
    {
        Init();
    }


    private void Init()
    {
        _playerController?.Init(_inputBuffer, _cinemachineCamera, _enemyPosition);
    }
}
