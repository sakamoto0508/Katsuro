using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

/// <summary>移動入力・旋回・Rigidbody移動と攻撃Root Motionの物理反映を担当する。攻撃や能力の選択は行わない。</summary>
public class PlayerMover
{
    /// <summary>移動設定・Rigidbody・PlayerとEnemyのTransform・Animatorを保持する。</summary>
    public PlayerMover(PlayerStatus playerStatus, Rigidbody rb, Transform playerPosition,Transform enemy
        , Transform cameraPosition, PlayerAnimationController animationController, float attackRootMotionScale = 1f)
    {
        _playerStatus = playerStatus;
        _rb = rb;
        _playerPosition = playerPosition;
        _enemyPosition = enemy;
        _cameraPosition = cameraPosition;
        _animationController = animationController;
        _attackRootMotionScale = Mathf.Max(0f, attackRootMotionScale);
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
    private readonly float _attackRootMotionScale;
    private Vector3 _pendingAttackRootMotion;
    public bool IsUsingAttackRootMotion { get; private set; }

    /// <summary>攻撃Root Motionの受付を開始し、前の攻撃の移動量を消去する。</summary>
    public void BeginAttackRootMotion()
    {
        if (IsUsingAttackRootMotion) return;
        IsUsingAttackRootMotion = true;
        _pendingAttackRootMotion = Vector3.zero;
        StopAttackVelocity();
    }
    /// <summary>攻撃Root Motionの受付と蓄積量を解除し、適用していた攻撃速度を停止する。</summary>
    public void EndAttackRootMotion()
    {
        _pendingAttackRootMotion = Vector3.zero;
        if (IsUsingAttackRootMotion) StopAttackVelocity();
        IsUsingAttackRootMotion = false;
    }
    /// <summary>再生中の攻撃から水平移動量を蓄積する。HitStop中の移動は蓄積しない。</summary>
    public void QueueAttackRootMotion(Vector3 delta, bool animationRunning)
    {
        if (!IsUsingAttackRootMotion) return;
        if (!animationRunning) { _pendingAttackRootMotion = Vector3.zero; StopAttackVelocity(); return; }
        delta.y = 0f;
        _pendingAttackRootMotion += delta * _attackRootMotionScale;
    }
    /// <summary>蓄積した攻撃移動量を衝突で制限し、物理フレームのRigidbody速度へ変換する。</summary>
    public void FixedUpdateAttackRootMotion(bool animationRunning)
    {
        if (!IsUsingAttackRootMotion) return;
        Vector3 delta = animationRunning ? AttackRootMotionPhysics.LimitDisplacement(_rb, _pendingAttackRootMotion) : Vector3.zero;
        _pendingAttackRootMotion = Vector3.zero;
        // Dynamic Rigidbody remains under physics collision/gravity control.
        Vector3 velocity = delta / Time.fixedDeltaTime;
        _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
    }
    /// <summary>自身が適用した攻撃速度を停止し、垂直方向の速度を保持する。</summary>
    private void StopAttackVelocity()
    {
        if (_rb != null && !_rb.isKinematic) _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
    }

    /// <summary>入力に応じた移動速度・方向と表示用Animator値を更新する。</summary>
    public void Update()
    {
        UpdateDirection();
        _animationController?.PlayBool(_animationController.AnimName.IsDrawingSword, IsDrawnSword);
        _animationController?.MoveVelocity(ReturnVelocity());
        _animationController?.MoveVector(ReturnVector());
        // アニメーションの不具合調査用に、速度とアニメーターの有無を記録する。
        // 一時的な調査処理。確認後は削除するかコメントアウトする。
        
    }

    /// <summary>攻撃Root Motionが優先されていなければ、通常移動と旋回を物理フレームで反映する。</summary>
    public void FixedUpdate()
    {
        if (IsUsingAttackRootMotion) return;
        Movement();
        UpdateRotation();
        SpeedControll();
    }

    /// <summary>次の移動更新に使用する平面入力を保持する。</summary>
    public void OnMove(Vector2 input)
    {
        _currentInput = input;
    }

    /// <summary>通常移動速度とDash速度の選択に使用するフラグを切り替える。</summary>
    public void SetSprint(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    /// <summary>Lock-On状態と対象方向を保持し、移動・旋回の基準へ反映する。</summary>
    public void LockOnDirection(bool isLockOn, Vector3 lockOnDirection)
    {
        _isLockOn = isLockOn;

        lockOnDirection.y = 0;
        _lockOnDirection = lockOnDirection.sqrMagnitude > 0.001f
            ? lockOnDirection.normalized
            : Vector3.zero;
    }

    /// <summary>移動入力と水平移動速度を止め、静止状態のAnimator値へ戻す。</summary>
    public void MoveStop()
    {
        _velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        if (_velXZ.sqrMagnitude > 0.01f)
        {
            Vector3 brakeForce = -_velXZ.normalized * _playerStatus.BreakForce;
            _rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    /// <summary>抜刀状態を記録し、移動時に使用する姿勢と速度の選択へ反映する。</summary>
    public void SetDrawingSword(bool value)=> IsDrawnSword = value;

    /// <summary>指定ターゲットの方向を見る。</summary>

    /// <returns>対象方向への補間旋回が終了するまで待機するタスク。</returns>
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

    /// <summary>移動速度をAnimatorへ渡すための値に換算する。</summary>
    /// <returns>現在の移動速度に対応するAnimator値。</returns>
    private float ReturnVelocity()
    {
        Vector3 velXZ = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        return velXZ.magnitude;
    }

    /// <summary>カメラとLock-On方向を基準に、入力からワールドの移動方向を求める。</summary>
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

    /// <summary>水平移動方向と速度をRigidbodyへ反映し、垂直速度を維持する。</summary>
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

    /// <summary>静止・歩行・Dashなど現在の入力と状態から目標速度を選ぶ。</summary>
    /// <returns>移動状態に対応する目標速度。</returns>
    private float ResolveTargetSpeed()
    {
        if (_isLockOn)
        {
            return _isSprinting ? _playerStatus.LockOnSprintSpeed : _playerStatus.LockOnWalkSpeed;
        }

        return _isSprinting ? _playerStatus.UnLockSprintSpeed : _playerStatus.UnLockWalkSpeed;
    }

    /// <summary>移動またはLock-Onの向きへPlayerを補間旋回させる。</summary>
    private void UpdateRotation()
    {
        if (_lookDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
            _playerPosition.rotation = Quaternion.Slerp(_playerPosition.rotation
                , targetRotation, _playerStatus.RotationSmoothness);
        }
    }


    /// <summary>目標速度に向けて加減速し、急激な速度切り替えを抑える。</summary>
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

    /// <summary>ワールド移動方向をPlayerローカルの二軸値へ変換してAnimatorへ渡す。</summary>
    /// <returns>移動方向を表す二軸値。</returns>
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
