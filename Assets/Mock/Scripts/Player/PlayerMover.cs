using UnityEngine;

public class PlayerMover
{
    public PlayerMover(PlayerStatus playerStatus, Rigidbody rb
        , Transform playerPosition, Transform cameraPosition)
    {
        _playerStatus = playerStatus;
        _rb = rb;
        _playerPosition = playerPosition;
        _cameraPosition = cameraPosition;
    }
    private PlayerStatus _playerStatus;
    private Rigidbody _rb;
    private Transform _playerPosition;
    private Transform _cameraPosition;
    private Vector2 _currentInput;
    private Vector2 _targetinput;

    public void FixedUpdate()
    {

    }

    public void OnMove(Vector2 input)
    {
        _currentInput = input;
    }
}
