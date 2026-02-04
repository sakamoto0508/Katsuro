using UnityEngine;
using UnityEngine.AI;

public class EnemyMover
{
    public EnemyMover(EnemyStuts enemyStuts, Transform owner, Transform playerPosition
        , EnemyAnimationController animationController, Rigidbody rb, NavMeshAgent agent
        , Animator animator, AnimationName animName)
    {
        _enemyStuts = enemyStuts;
        _ownerTransform = owner;
        _playerPosition = playerPosition;
        _animationController = animationController;
        _rb = rb;
        _agent = agent;
        _animator = animator;
        _animName = animName;
    }

    private EnemyStuts _enemyStuts;
    private Transform _playerPosition;
    private Transform _ownerTransform;
    private EnemyAnimationController _animationController;
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Animator _animator;
    private AnimationName _animName;
    private float _destinationUpdateTimer = 0f;
    private bool _usingRootMotionStepBack;

    public void Update()
    {
        if (_playerPosition == null) return;
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
        _agent.Warp(target);
    }

    public void StartStepBack()
    {
        if (_animator == null || _agent == null) return;
        Debug.Log("EnemyMover: StartStepBack");
        _agent.updatePosition = false;      // Agent に位置更新させない
        _agent.isStopped = true;
        _usingRootMotionStepBack = true;
        // Disable rigidbody physics while applying root motion to avoid physics interfering with transform
        if (_rb != null) _rb.isKinematic = true;
        _animator.applyRootMotion = true;   // Animator が root motion を反映するようにする
        _animator.SetTrigger(_animName.BackStep);   // アニメ再生
    }

    public void EndStepBack()
    {
        Debug.Log("EnemyMover: EndStepBack");
        _animator.applyRootMotion = false;
        _usingRootMotionStepBack = false;
        // restore rigidbody and sync agent to transform
        if (_rb != null) _rb.isKinematic = false;
        //_agent.Warp(_ownerTransform.position);
        _agent.updatePosition = true;
        _agent.isStopped = false;
    }

    public void OnAnimatorMove()
    {
        if (!_usingRootMotionStepBack) return;
        // Animator.deltaPosition/Rotation を transform に適用
        var deltaPos = _animator.deltaPosition;
        var deltaRot = _animator.deltaRotation;
        _ownerTransform.position += deltaPos;
        _ownerTransform.rotation *= deltaRot;
        // NavMeshAgent と位置同期
        if (_agent != null)
        {
            _agent.nextPosition = _ownerTransform.position;
        }
    }
}
