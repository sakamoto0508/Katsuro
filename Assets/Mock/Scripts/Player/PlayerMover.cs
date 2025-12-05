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
    private Vector3 _targetVelocity;

    public void Update()
    {

    }

    public void FixedUpdate()
    {
        Movement();
        Rotation();
        SpeedControll();
    }

    public void OnMove(Vector2 input)
    {
        _currentInput = input;
    }

    /// <summary>
    /// 速度を返す。
    /// </summary>
    private Vector3 Direction()
    {
        Vector3 direction = (_cameraPosition.forward * _currentInput.y
            + _cameraPosition.right * _currentInput.x).normalized;
        direction.y = 0;

        return direction;
    }

    /// <summary>
    /// プレイヤーの移動処理。
    /// </summary>
    private void Movement()
    {
        float inputMagnitude = Mathf.Clamp01(_currentInput.magnitude);
        _rb.AddForce(Direction() * _playerStatus.MoveSpeed * 10f, ForceMode.Acceleration);
    }

    private void Rotation()
    {
        // 入力方向から回転目標を決める（停止時も回転できる）。
        Vector3 inputDir = Direction();
        //  移動している場合はvelocityを優先。
        Vector3 vel=_rb.linearVelocity;
        vel.y = 0;
        Vector3 lookDir = vel.sqrMagnitude > 0.1f ? vel : inputDir;
        if (lookDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            _playerPosition.rotation = Quaternion.Slerp(_playerPosition.rotation
                , targetRotation, _playerStatus.RoationSmoothness);
        }
    }

    /// <summary>
    /// 速度制御。
    /// </summary>
    private void SpeedControll()
    {
        Vector3 vel = _rb.linearVelocity;
        vel.y = 0;
        Vector3 velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        // 最大速度を超過している場合は制限をかける。
        if (velXZ.magnitude >= _playerStatus.MoveSpeed)
        {
            Vector3 limited = velXZ.normalized * _playerStatus.MoveSpeed;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }
        // 入力無し → ブレーキをかける。
        if (_currentInput.sqrMagnitude < 0.01f)
        {
            if (velXZ.sqrMagnitude > 0.01f)
            {
                // まだ動いているならブレーキ力を与える。
                Vector3 brakeForce = -velXZ.normalized * _playerStatus.BreakForce;
                _rb.AddForce(brakeForce, ForceMode.Acceleration);
            }
            else
            {
                // 完全停止。
                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            }
        }
    }
}
