using UnityEngine;
using UnityEngine.AI;

public class EnemyMover
{
    public EnemyMover(EnemyStuts enemyStuts, NavMeshAgent navMeshAgent, Transform playerPosition
        , EnemyAnimationController animationController, Rigidbody rb)
    {
        _enemyStuts = enemyStuts;
        _navMeshAgent = navMeshAgent;
        _playerPosition = playerPosition;
        _animationController = animationController;
        _rb = rb;
    }

    private EnemyStuts _enemyStuts;
    private NavMeshAgent _navMeshAgent;
    private Transform _playerPosition;
    private EnemyAnimationController _animationController;
    private Rigidbody _rb;
    private float _destinationUpdateTimer = 0f;

    public void Update()
    {
        if (_playerPosition == null) return;
        // アニメ用の速度は NavMeshAgent の速度を優先して使う。
        // Rigidbody 側の速度を参照していると Transform/Warp 等の影響で値が不安定になるため。
        float speed = 0f;
        if (_navMeshAgent != null)
        {
            speed = _navMeshAgent.velocity.magnitude;
        }
        else if (_rb != null)
        {
            speed = _rb.linearVelocity.magnitude;
        }
        _animationController?.MoveVelocity(speed);
        float distanceToPlayer =
            Vector3.Distance(_navMeshAgent.transform.position, _playerPosition.position);

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
            _navMeshAgent.SetDestination(_playerPosition.position);
        }
    }

    public void StopMove()
    {
        if (!_navMeshAgent.isStopped)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }
    }

    public bool IsStopped => _navMeshAgent.isStopped;

    public void ResumeMove()
    {
        _navMeshAgent.isStopped = false;
        _destinationUpdateTimer = 0f;
    }

    /// <summary>
    /// 即時にプレイヤー方向へ目的地を更新して追跡を開始します。
    /// </summary>
    public void Approach()
    {
        if (_playerPosition == null || _navMeshAgent == null) return;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(_playerPosition.position);
        _destinationUpdateTimer = _enemyStuts.DestinationUpdateInterval;
    }

    /// <summary>
    /// クイックバック（テレポート的な後退）を行う。簡易実装は Warp を利用します。
    /// </summary>
    public void StepBack(float distance)
    {
        if (_navMeshAgent == null || _playerPosition == null) return;
        Vector3 dir = (_navMeshAgent.transform.position - _playerPosition.position);
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f)
        {
            dir = -_navMeshAgent.transform.forward;
        }
        Vector3 target = _navMeshAgent.transform.position + dir.normalized * distance;
        _navMeshAgent.Warp(target);
    }
}
