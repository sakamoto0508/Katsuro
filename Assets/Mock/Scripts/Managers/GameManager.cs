using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private PlayerController _playerController;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        _playerController?.Init(_inputBuffer);
    }
}
