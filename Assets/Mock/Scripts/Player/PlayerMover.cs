using UnityEngine;

public class PlayerMover
{
    public PlayerMover(PlayerStatus playerStatus, Rigidbody rb, Transform playerPosition
        , Transform cameraPosition, PlayerAnimationController animationController)
    {
        _playerStatus = playerStatus;
        _rb = rb;
        _playerPosition = playerPosition;
        _cameraPosition = cameraPosition;
        _animationController = animationController;
    }
    private PlayerStatus _playerStatus;
    private PlayerAnimationController _animationController;
    private Rigidbody _rb;
    private Transform _playerPosition;
    private Transform _cameraPosition;
    private Vector2 _currentInput;
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    private Vector3 _lockOnDirection;
    private bool _isLockOn;

    public void Update()
    {
        UpdateDirection();
        _animationController?.MoveVelocity(ReturnVelocity());
    }

    public void FixedUpdate()
    {
        Movement();
        UpdateRotation();
        SpeedControll();
    }

    public void OnMove(Vector2 input)
    {
        _currentInput = input;
    }

    public void LockOnDirection(bool isLockOn, Vector3 lockOnDirection)
    {
        _isLockOn = isLockOn;

        lockOnDirection.y = 0;
        _lockOnDirection = lockOnDirection.sqrMagnitude > 0.001f
            ? lockOnDirection.normalized
            : Vector3.zero;
    }

    private float ReturnVelocity()
    {
        Vector3 velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        return velXZ.magnitude;
    }

    private void UpdateDirection()
    {
        // カメラ基準の前後・左右を水平面に投影し、入力からワールド方向を求める。
        Vector3 cameraForward = _cameraPosition.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = _cameraPosition.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 worldDirection = cameraForward * _currentInput.y + cameraRight * _currentInput.x;
        _moveDirection = worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : Vector3.zero;

        if (_isLockOn && _lockOnDirection.sqrMagnitude > 0.001f)
        {
            // 敵方向(forward)とカメラ右方向(lateral)を直交化し、純粋なストレーフ軸を作る。
            Vector3 forward = _lockOnDirection;
            Vector3 lateral = cameraRight;
            Vector3 up = Vector3.up;
            Vector3.OrthoNormalize(ref up, ref forward, ref lateral);

            Vector3 lockMove = forward * _currentInput.y + lateral * _currentInput.x;
            _moveDirection = lockMove.sqrMagnitude > 0.001f ? lockMove.normalized : Vector3.zero;

            // ロックオン時は常に敵方向を向く。
            _lookDirection = forward;
            return;
        }

        // 非ロックオン時は移動速度が十分あれば速度方向を、なければ入力方向を向く。
        Vector3 vel = _rb.linearVelocity;
        vel.y = 0;
        _lookDirection = vel.sqrMagnitude > 0.1f ? vel.normalized : _moveDirection;
    }

    private void Movement()
    {
        if (_moveDirection.sqrMagnitude < 0.001f)
        {
            // 入力が極小なら力を加えない。
            return;
        }

        float inputMagnitude = Mathf.Clamp01(_currentInput.magnitude);
        float targetSpeed = _isLockOn ? _playerStatus.LockOnWalkSpeed : _playerStatus.UnLockWalkSpeed;
        // ロックオン状態に応じた目標速度で加速力を決定。
        Vector3 acceleration = _moveDirection * targetSpeed * _playerStatus.Acceleration * inputMagnitude;
        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void UpdateRotation()
    {
        if (_lookDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
            _playerPosition.rotation = Quaternion.Slerp(_playerPosition.rotation
                , targetRotation, _playerStatus.RoationSmoothness);
        }
    }


    private void SpeedControll()
    {
        Vector3 velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        float maxSpeed = _isLockOn ? _playerStatus.LockOnWalkSpeed : _playerStatus.UnLockWalkSpeed;

        // 速度上限を超えていたら水平方向のみ制限。
        if (velXZ.magnitude >= maxSpeed)
        {
            Vector3 limited = velXZ.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }

        // 入力が無いときは減速力を与えるか完全停止させる。
        if (_currentInput.sqrMagnitude < 0.01f)
        {
            if (velXZ.sqrMagnitude > 0.01f)
            {
                Vector3 brakeForce = -velXZ.normalized * _playerStatus.BreakForce;
                _rb.AddForce(brakeForce, ForceMode.Acceleration);
            }
            else
            {
                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            }
        }
    }
}
