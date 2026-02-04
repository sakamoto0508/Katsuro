using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMover
{
    public EnemyMover(EnemyStuts enemyStuts, Transform owner, Transform playerPosition
        , EnemyAnimationController animationController, Rigidbody rb, NavMeshAgent agent
        , Animator animator, AnimationName animName, CancellationToken destroyToken)
    {
        _enemyStuts = enemyStuts;
        _ownerTransform = owner;
        _playerPosition = playerPosition;
        _animationController = animationController;
        _rb = rb;
        _agent = agent;
        _animator = animator;
        _animName = animName;
        _destroyToken = destroyToken;
    }

    private EnemyStuts _enemyStuts;
    private Transform _playerPosition;
    private Transform _ownerTransform;
    private EnemyAnimationController _animationController;
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimationName _animName;
    private CancellationToken _destroyToken;
    private float _destinationUpdateTimer = 0f;
    private bool _usingRootMotionStepBack;
    private bool _isStepBack;

    public void Update()
    {
        if (_playerPosition == null) return;
        if (_isStepBack)
        {
            StopMove();
            return;
        }
        // アニメ用の速度は NavMeshAgent の速度を優先して使う。
        // Rigidbody 側の速度を参照していると Transform/Warp 等の影響で値が不安定になるため。
        float speed = 0f;
        if (_agent != null)
        {
            speed = _agent.velocity.magnitude;
        }
        else if (_rb != null)
        {
            speed = _rb.linearVelocity.magnitude;
        }
        _animationController?.MoveVelocity(speed);
        float distanceToPlayer =
            Vector3.Distance(_ownerTransform.position, _playerPosition.position);

        // 追跡開始距離外なら何もしない
        if (distanceToPlayer > _enemyStuts.ChaseStartDistance)
        {
            StopMove();
            return;
        }

        // 攻撃距離に入ったら停止
        if (distanceToPlayer <= _enemyStuts.StopDistance)
        {
            StopMove();
            return;
        }

        // 目的地更新（一定間隔）
        _destinationUpdateTimer -= Time.deltaTime;
        if (_destinationUpdateTimer <= 0f)
        {
            _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
            _agent.SetDestination(_playerPosition.position);
        }
    }

    public void StopMove()
    {
        if (!_agent.isStopped)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    public bool IsStopped => _agent.isStopped;

    public void ResumeMove()
    {
        _agent.isStopped = false;
        _destinationUpdateTimer = 0f;
    }

    /// <summary>
    /// 即時にプレイヤー方向へ目的地を更新して追跡を開始します。
    /// </summary>
    public void Approach()
    {
        if (_playerPosition == null || _agent == null) return;
        _agent.isStopped = false;
        _agent.SetDestination(_playerPosition.position);
        _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
    }

    /// <summary>
    /// クイックバック（テレポート的な後退）を行う。簡易実装は Warp を利用します。
    /// </summary>
    public void StepBack(float distance)
    {
        if (_agent == null || _playerPosition == null) return;
        Vector3 dir = (_ownerTransform.position - _playerPosition.position);
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = -_ownerTransform.forward;
        }
        Vector3 target = _ownerTransform.position + dir.normalized * distance;
        // Warp caused unstable agent corrections when combined with root-motion.
        // Instead, move the transform directly as a fallback and keep agent in sync via nextPosition.
        _ownerTransform.position = target;
        if (_agent != null)
        {
            _agent.nextPosition = target;
            Debug.Log($"EnemyMover.StepBack: moved owner to {target}, agent.nextPosition set");
        }
    }

    public void StartStepBack()
    {
        if (_animator == null || _agent == null) return;
        _agent.updatePosition = false;
        _agent.isStopped = true;
        _usingRootMotionStepBack = true;
        _animator.applyRootMotion = true;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _animator.SetTrigger(_animName.BackStep);
        _isStepBack = true;
    }

    public void EndStepBack()
    {
        _usingRootMotionStepBack = false;
        if (_animator != null) _animator.applyRootMotion = false;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        if (_agent != null)
        {
            _agent.velocity = Vector3.zero;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }

        _isStepBack = false;
    }

    public void OnAnimatorMove()
    {
        if (!_usingRootMotionStepBack) return;
        // Animator.deltaPosition/Rotation を transform に適用
        _ownerTransform.position += _animator.deltaPosition;
        _ownerTransform.rotation *= _animator.deltaRotation;
        // NavMeshAgent と位置同期
        if (_agent != null)
        {
            _agent.nextPosition = _ownerTransform.position;
        }
    }

    /// <summary>
    /// バックステップ終了時の現在の所有者位置を返します。
    /// </summary>
    public Vector3 GetOwnerPosition()
    {
        return _ownerTransform != null ? _ownerTransform.position : Vector3.zero;
    }

    public async UniTaskVoid StepBackSequence()
    {
        StartStepBack();

        try
        {
            await UniTask.Delay(
                300,
                cancellationToken: _destroyToken
            );
        }
        catch (OperationCanceledException)
        {
            // 破壊されたので何もしない
            return;
        }

        EndStepBack();
    }
}
