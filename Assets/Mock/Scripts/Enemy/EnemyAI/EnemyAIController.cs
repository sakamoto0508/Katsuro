using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 軽量な Enemy AI の骨組み。
/// - State 管理（Idle/Chase/Attack/Observe/Backstep/Dead）
/// - Decision 呼び出し（距離＋履歴＋重みで行動選択）
/// - アニメーションイベントやタイマーから Decision を発火する想定
/// </summary>
[RequireComponent(typeof(Transform))]
public class EnemyAIController : MonoBehaviour
{
    [SerializeField] private EnemyDecisionConfig _config = null;
    private UnityEngine.AI.NavMeshAgent _agent;
    private Transform _player;
    private EnemyState _state = EnemyState.Idle;
    private EnemyActionType _lastAction = EnemyActionType.Wait;
    private bool _isBusy = false;
    private bool _isObserving = false;
    private float _observeTimer = 0f;
    private EnemyDecisionMaker _decisionMaker = new EnemyDecisionMaker();

    public void Init(Transform player, UnityEngine.AI.NavMeshAgent agent, EnemyDecisionConfig config = null)
    {
        _player = player;
        _agent = agent;
        if (config != null)
            _config = config;
        ChangeState(EnemyState.Idle);
    }

    private void Update()
    {
        // Observe 状態のタイマー処理（_isObserving が true のとき）
        if (_isObserving)
        {
            if (_observeTimer > 0f)
            {
                _observeTimer -= Time.deltaTime;
                if (_observeTimer <= 0f)
                {
                    _isObserving = false;
                    TriggerDecision();
                }
            }
        }
    }

    /// <summary>ステート遷移（内部で Enter を処理）</summary>
    private void ChangeState(EnemyState next)
    {
        if (_state == next) return;
        _state = next;
        OnEnterState(next);
    }

    private void OnEnterState(EnemyState s)
    {
        switch (s)
        {
            case EnemyState.Idle:
                _isBusy = false;
                break;
            case EnemyState.Chase:
                _isBusy = false;
                if (_agent != null)
                {
                    _agent.isStopped = false;
                }
                break;
            case EnemyState.Attack:
                _isBusy = true;
                // 攻撃は Animator 側や EnemyAttacker 呼び出しで実施
                break;
            case EnemyState.Observe:
                // Observe/Recovery handled by flags when entered
                _isObserving = true;
                _observeTimer = _config != null ? _config.ObserveSeconds : 1.0f;
                break;
            case EnemyState.Stagger:
                _isBusy = true;
                break;
            case EnemyState.Dead:
                _isBusy = true;
                break;
        }
    }

    /// <summary>
    /// 攻撃が終了した際にアニメーションイベント等から呼ぶエントリ。
    /// ここで行動抽選を行う。
    /// </summary>
    public void OnAttackFinished()
    {
        _isBusy = false;
        TriggerDecision();
    }

    /// <summary>プレイヤーとの距離などをもとに Decision を行い、選ばれた行動を実行する（骨組み）。</summary>
    private void TriggerDecision()
    {
        if (_isBusy || _player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);
        var action = _decisionMaker.Decide(distance, _lastAction, _config);

        _lastAction = action;
        ExecuteAction(action);
    }

    private void ExecuteAction(EnemyActionType action)
    {
        switch (action)
        {
            case EnemyActionType.WarpAttack:
                ChangeState(EnemyState.Attack);
                break;
            case EnemyActionType.Approach:
                ChangeState(EnemyState.Chase);
                break;
            case EnemyActionType.Thrust:
            case EnemyActionType.Slash:
            case EnemyActionType.HeavySlash:
                ChangeState(EnemyState.Attack);
                break;
            case EnemyActionType.Wait:
                ChangeState(EnemyState.Observe);
                break;
            case EnemyActionType.StepBack:
                ChangeState(EnemyState.Backstep);
                break;
            default:
                ChangeState(EnemyState.Idle);
                break;
        }
    }

    // --- Decision Maker (内部クラス) ---
    private class EnemyDecisionMaker
    {
        private readonly System.Random _rand = new();

        public EnemyActionType Decide(float distance, EnemyActionType lastAction, EnemyDecisionConfig stuts)
        {
            // 距離でランク分け
            if (stuts == null) return EnemyActionType.Wait;
            if (distance > stuts.FarDistance)
            {
                return Sample(new[] { (EnemyActionType.WarpAttack, stuts.WeightWarpAttack), (EnemyActionType.Approach, stuts.WeightApproach) });
            }
            else if (distance > stuts.NearDistance)
            {
                return Sample(new[] { (EnemyActionType.Thrust, stuts.WeightRush), (EnemyActionType.Approach, stuts.WeightApproach), (EnemyActionType.Wait, stuts.WeightObserve) });
            }
            else
            {
                // 近距離
                var candidates = new List<(EnemyActionType action, float weight)>()
                {
                    (EnemyActionType.Slash, stuts.WeightSlash),
                    (EnemyActionType.Wait, stuts.WeightObserve),
                    (EnemyActionType.StepBack, stuts.WeightBackstep)
                };
                // Observe (Wait) が連続しないようにする
                if (lastAction == EnemyActionType.Wait)
                {
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        if (candidates[i].action == EnemyActionType.Wait)
                            candidates[i] = (candidates[i].action, 0f);
                    }
                }
                return Sample(candidates.ToArray());
            }
        }

        private EnemyActionType Sample((EnemyActionType action, float weight)[] candidates)
        {
            float total = 0f;
            foreach (var c in candidates) total += Math.Max(0f, c.weight);
            if (total <= 0f) return EnemyActionType.Wait;
            double r = _rand.NextDouble() * total;
            double acc = 0;
            foreach (var c in candidates)
            {
                acc += Math.Max(0f, c.weight);
                if (r <= acc) return c.action;
            }
            return candidates[candidates.Length - 1].action;
        }
    }
}
