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
    private bool _isPatrolWalking;
    float _speed = 0f;

    public void Update()
    {
        if (_playerPosition == null) return;
        if (_isStepBack)
        {
            StopMove();
            return;
        }
        // パトロール歩行モード中は追跡向けの目的地上書きをしない
        if (_isPatrolWalking)
        {
            // アニメ向け速度更新
            _animationController?.MoveVelocity(_agent != null ? _agent.velocity.magnitude : 0f);

            // 到達判定は通常の到達判定と同じにする
            if (_agent != null && _agent.hasPath && !_agent.pathPending)
            {
                float remaining = _agent.remainingDistance;
                float stopDist = _agent.stoppingDistance;
                if (remaining <= stopDist + 0.1f)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _destinationUpdateTimer = 0f;
                    _isPatrolWalking = false;
                }
            }

            return;
        }
        // アニメ用の速度は NavMeshAgent の速度を優先して使う。
        // Rigidbody 側の速度を参照していると Transform/Warp 等の影響で値が不安定になるため。
        
        if (_agent != null)
        {
            _speed = _agent.velocity.magnitude;
        }
        else if (_rb != null)
        {
            _speed = _rb.linearVelocity.magnitude;
        }
        _animationController?.MoveVelocity(_speed);
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

        // 安定した到達判定: pathPending が false でかつ remainingDistance が stoppingDistance を下回ったら到達とみなす
        if (_agent != null && _agent.hasPath && !_agent.pathPending)
        {
            float remaining = _agent.remainingDistance;
            float stopDist = _agent.stoppingDistance;
            if (remaining <= stopDist + 0.1f)
            {
                // 到達処理: 移動停止してパスをクリア
                _agent.isStopped = true;
                _agent.ResetPath();
                _destinationUpdateTimer = 0f;
            }
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

    public void StartPatrolWalk()
    {
        if (_agent == null || _playerPosition == null || _ownerTransform == null) return;

        // ランダムで左右どちらかに移動する（プレイヤーを基準）
        int choice = UnityEngine.Random.Range(0, 2); // 0 or 1

        // プレイヤー基準の正面方向
        Vector3 toPlayer = (_playerPosition.position - _ownerTransform.position);
        if (toPlayer.sqrMagnitude < 0.001f)
        {
            // プレイヤーとほぼ同位置なら敵の正面を基準にする
            toPlayer = _ownerTransform.forward;
        }
        Vector3 forward = toPlayer.normalized;

        // 横方向ベクトル（右方向）を作る
        Vector3 right = Quaternion.LookRotation(forward) * Vector3.right;

        // オフセット距離は追跡開始距離と停止距離から計算（最低1m、最大4m）
        float offset = Mathf.Clamp(_enemyStuts.ChaseStartDistance - _enemyStuts.StopDistance, 1f, 4f);
        float sign = choice == 0 ? 1f : -1f;

        Vector3 target = _playerPosition.position + right * sign * offset;

        // NavMesh 上の到達可能な位置へスナップ
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
            _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
            _isPatrolWalking = true;
        }
    }

    public void StartStepBack()
    {
        if (_animator == null || _agent == null) return;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
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
            _agent.Warp(_ownerTransform.position); // ★最重要
            _agent.velocity = Vector3.zero;
            _agent.isStopped = false;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            _agent.ResetPath();
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
}
