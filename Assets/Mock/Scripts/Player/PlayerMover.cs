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
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    private Vector3 _lockOnDirection;
    private bool _isLockOn;

    public void Update()
    {
        UpdateDirection();
    }

    public void FixedUpdate()
    {
        Movement();
        UpdateRotation();
        SpeedControll();
    }

    /// <summary>
    /// 入力の受け取り。
    /// </summary>
    /// <param name="input"></param>
    public void OnMove(Vector2 input)
    {
        _currentInput = input;
    }

    /// <summary>
    /// プレイヤーの速度を返す。
    /// </summary>
    public float ReturnVelocity()
    {
        Vector3 velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        return velXZ.magnitude;
    }

    public void LockOnDirection(bool isLockOn, Vector3 lockOnDirection)
    {
        _isLockOn = isLockOn;

        lockOnDirection.y = 0;
        // 正規化して代入。ただしゼロベクトルの場合はそのままゼロベクトルを代入。
        _lockOnDirection = lockOnDirection.sqrMagnitude > 0.001f
            ? lockOnDirection.normalized
            : Vector3.zero;
    }

    /// <summary>
    /// 方向処理。
    /// </summary>
    private void UpdateDirection()
    {
        Vector3 direction = (_cameraPosition.forward * _currentInput.y
            + _cameraPosition.right * _currentInput.x).normalized;
        direction.y = 0;
        _moveDirection = direction.normalized;

        if (_isLockOn)
        {
            _lookDirection = _lockOnDirection;
            return;
        }

        // 回転方向は速度優先。
        Vector3 vel = _rb.linearVelocity;
        vel.y = 0;
        _lookDirection = vel.sqrMagnitude > 0.1f ? vel.normalized : _moveDirection;
    }

    /// <summary>
    /// プレイヤーの移動処理。
    /// </summary>
    private void Movement()
    {
        float inputMagnitude = Mathf.Clamp01(_currentInput.magnitude);
        _rb.AddForce(_moveDirection * _playerStatus.MoveSpeed * _playerStatus.Acceleration
            , ForceMode.Acceleration);
    }

    /// <summary>
    /// 回転処理。
    /// </summary>
    private void UpdateRotation()
    {
        if (_lookDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
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
