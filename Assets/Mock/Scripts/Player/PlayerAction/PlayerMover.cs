using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class PlayerMover
{
    public PlayerMover(PlayerStatus playerStatus, Rigidbody rb, Transform playerPosition,Transform enemy
        , Transform cameraPosition, PlayerAnimationController animationController)
    {
        _playerStatus = playerStatus;
        _rb = rb;
        _playerPosition = playerPosition;
        _enemyPosition = enemy;
        _cameraPosition = cameraPosition;
        _animationController = animationController;
    }
    /// <summary>抜刀しているかどうか。</summary>
    public bool IsDrawnSword { get; private set; }
    private PlayerStatus _playerStatus;
    private PlayerAnimationController _animationController;
    private Rigidbody _rb;
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private Transform _cameraPosition;
    private Vector2 _currentInput;
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    private Vector3 _lockOnDirection;
    private Vector3 _velXZ;
    private bool _isLockOn;
    private bool _isSprinting;

    public void Update()
    {
        UpdateDirection();
        _animationController?.PlayBool(_animationController.AnimName.IsDrawingSword, IsDrawnSword);
        _animationController?.MoveVelocity(ReturnVelocity());
        _animationController?.MoveVector(ReturnVector());
        // Debug quick-check: log velocity and animator presence when running into animation issues
        // (temporary) remove or comment out when confirmed
        
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

    public void SetSprint(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    public void LockOnDirection(bool isLockOn, Vector3 lockOnDirection)
    {
        _isLockOn = isLockOn;

        lockOnDirection.y = 0;
        _lockOnDirection = lockOnDirection.sqrMagnitude > 0.001f
            ? lockOnDirection.normalized
            : Vector3.zero;
    }

    public void MoveStop()
    {
        _velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (_velXZ.sqrMagnitude > 0.01f)
        {
            Vector3 brakeForce = -_velXZ.normalized * _playerStatus.BreakForce;
            _rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    public void SetDrawingSword(bool value)=> IsDrawnSword = value;

    /// <summary>指定ターゲットの方向を見る。</summary>

    public async UniTask LookTargetSmooth(float duration, CancellationToken ct = default)
    {
        if (_enemyPosition == null || _playerPosition == null) return;

        Vector3 dir = _enemyPosition.position - _playerPosition.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion start = _playerPosition.rotation;
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _playerPosition.rotation = Quaternion.Slerp(start, target, t);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        _playerPosition.rotation = target;
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
        float targetSpeed = ResolveTargetSpeed();
        // ロックオン／スプリント状態に応じた目標速度で加速力を決定。
        Vector3 acceleration = _moveDirection * targetSpeed * _playerStatus.Acceleration * inputMagnitude;
        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    private float ResolveTargetSpeed()
    {
        if (_isLockOn)
        {
            return _isSprinting ? _playerStatus.LockOnSprintSpeed : _playerStatus.LockOnWalkSpeed;
        }

        return _isSprinting ? _playerStatus.UnLockSprintSpeed : _playerStatus.UnLockWalkSpeed;
    }

    private void UpdateRotation()
    {
        if (_lookDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
            _playerPosition.rotation = Quaternion.Slerp(_playerPosition.rotation
                , targetRotation, _playerStatus.RotationSmoothness);
        }
    }


    private void SpeedControll()
    {
        _velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        float maxSpeed = ResolveTargetSpeed();

        // 速度上限を超えていたら水平方向のみ制限。
        if (_velXZ.magnitude >= maxSpeed)
        {
            Vector3 limited = _velXZ.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(limited.x, _rb.linearVelocity.y, limited.z);
        }
        // 入力が無いときは減速力を与えるか完全停止させる。
        if (_currentInput.sqrMagnitude < 0.01f)
        {
            MoveStop();
        }
    }

    private Vector2 ReturnVector()
    {
        Vector2 animInput = Vector2.zero;
        if (_moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 localDir = _playerPosition.InverseTransformDirection(_moveDirection);
            animInput = new Vector2(localDir.x, localDir.z);
        }
        return animInput;
    }
}
