using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵の AI 制御クラス。
/// 距離や過去の行動から次の行動を決定し、EnemyController に対して行動を列挙します。
/// </summary>
public class EnemyAI
{
    /// <summary>
    /// コンストラクタ：AI を初期化します。
    /// </summary>
    /// <param name="controller">この AI が操作する EnemyController</param>
    /// <param name="player">追跡対象のプレイヤー Transform</param>
    /// <param name="agent">NavMeshAgent（経路探索用）</param>
    /// <param name="config">行動決定の設定</param>
    public EnemyAI(EnemyController controller, Transform player, NavMeshAgent agent, EnemyDecisionConfig config)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _player = player;
        _agent = agent;
        _config = config;
        _decisionMaker = new EnemyDecisionMaker();

        // initial decision
        TriggerDecision();
    }

    private readonly EnemyController _controller;
    private readonly Transform _player;
    private readonly NavMeshAgent _agent;
    private readonly EnemyDecisionConfig _config;
    private readonly EnemyDecisionMaker _decisionMaker;
    /// <summary>現在の AI ステート</summary>
    private EnemyState _state = EnemyState.Idle;
    /// <summary>直前に実行した行動</summary>
    private EnemyActionType _lastAction = EnemyActionType.Wait;
    /// <summary>攻撃・怯みなど、次の Decision を行えない状態かどうか</summary>
    private bool _isBusy;
    /// <summary>観察中かどうか</summary>
    private bool _isObserving;
    /// <summary>観察タイマ</summary>
    private float _observeTimer;
    // 再評価タイマ: Chase 中に定期的に意思決定を再評価するため
    private float _reconsiderTimer = 0f;

    /// <summary>
    /// 毎フレームの更新処理。AI の状態タイマや追従行動を処理します。
    /// </summary>
    /// <param name="deltaTime">フレームの経過時間（秒）</param>
    public void Tick(float deltaTime)
    {
        if (_isObserving)
        {
            if (_observeTimer > 0f)
            {
                _observeTimer -= deltaTime;
                // 様子見が終わったら次の行動を決定
                if (_observeTimer <= 0f)
                {
                    _isObserving = false;
                    TriggerDecision();
                }
            }
        }

        if (_state == EnemyState.Chase && _controller != null)
        {
            // 毎フレームは移動コマンドを出す（追跡継続）
            _controller.EnqueueAction(EnemyActionType.Approach);

            // 定期的に再評価して攻撃に遷移できるか確認する
            _reconsiderTimer -= deltaTime;
            if (_reconsiderTimer <= 0f)
            {
                _reconsiderTimer =  _config.ReconsiderInterval;
                if (!_isBusy)
                {
                    TriggerDecision();
                }
            }
        }
    }

    /// <summary>
    /// 攻撃アニメーション終了時に呼び出すコールバック。
    /// 攻撃中フラグを解除し再度意思決定を行います。
    /// </summary>
    public void OnAttackFinished()
    {
        _isBusy = false;
        TriggerDecision();
    }

    /// <summary>
    /// 現在の状況をもとに次の行動を決定する。
    /// </summary>
    private void TriggerDecision()
    {
        // 行動中、またはプレイヤー不在なら何もしない
        if (_isBusy || _player == null)
            return;

        // プレイヤーとの距離を計算
        float distance = Vector3.Distance(
            _controller.transform.position,
            _player.position);

        // DecisionMaker に行動選択を委譲
        var action = _decisionMaker.Decide(
            distance,
            _lastAction,
            _config);

        Debug.Log($"AI decide {action}");

        _lastAction = action;
        ExecuteAction(action);
    }

    /// <summary>
    /// 決定された行動を EnemyController に指示する。
    /// </summary>
    private void ExecuteAction(EnemyActionType action)
    {
        switch (action)
        {
            case EnemyActionType.WarpAttack:
                _controller.EnqueueAction(action);
                _state = EnemyState.Attack;
                _isBusy = true;
                break;
            case EnemyActionType.Approach:
                _controller.EnqueueAction(EnemyActionType.Approach);
                _state = EnemyState.Chase;
                break;
            case EnemyActionType.Thrust:
            case EnemyActionType.Slash:
            case EnemyActionType.HeavySlash:
                _controller.EnqueueAction(action);
                _state = EnemyState.Attack;
                _isBusy = true;
                break;
            case EnemyActionType.Wait:
                _state = EnemyState.Recovery;
                _isObserving = true;
                _observeTimer = _config != null ? _config.ObserveSeconds : 1.0f;
                break;
            case EnemyActionType.StepBack:
                _controller.EnqueueAction(EnemyActionType.StepBack);
                _state = EnemyState.Stagger;
                _isBusy = true;
                break;
            default:
                _state = EnemyState.Idle;
                break;
        }
    }
}
