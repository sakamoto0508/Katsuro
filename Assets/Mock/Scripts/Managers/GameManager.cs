using Unity.Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private PlayerController _playerController;

    [Header("Camera")]
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
        _playerController?.Init(_inputBuffer, _cinemachineCamera);
    }
}
