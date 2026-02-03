using UnityEngine;
using UnityEngine.AI;

public class EnemyMover
{
    public EnemyMover(EnemyStuts enemyStuts, NavMeshAgent navMeshAgent
        ,Transform playerPosition)
    {
        _enemyStuts = enemyStuts;
        _navMeshAgent = navMeshAgent;
        _playerPosition = playerPosition;
    }

    private EnemyStuts _enemyStuts;
    private NavMeshAgent _navMeshAgent;
    private Transform _playerPosition;
    private float _destinationUpdateTimer = 0f;

    public void Update()
    {
        if (_playerPosition == null) return;

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
}
