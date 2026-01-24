using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの状態遷移と更新処理を管理するステートマシン。
/// 登録済みステートを ID で切り替え、各種入力を現在ステートへ委譲する。
/// </summary>
public sealed class PlayerStateMachine
{
    private readonly Dictionary<PlayerStateId, PlayerState> _states;
    private PlayerState _currentState;

    public PlayerStateMachine(PlayerStateContext context)
    {
        Context = context;

        _states = new Dictionary<PlayerStateId, PlayerState>
        {
            { PlayerStateId.Locomotion, new PlayerLocomotionState(context, this) },
            { PlayerStateId.Dash, new PlayerDashState(context, this) },
            { PlayerStateId.LightAttack, new PlayerLightAttackState(context, this) },
            { PlayerStateId.StrongAttack, new PlayerStrongAttackState(context, this) },
        };

        ChangeState(PlayerStateId.Locomotion);
    }

    /// <summary>ステート間で共有される依存情報。</summary>
    public PlayerStateContext Context { get; }

    /// <summary>
    /// 指定 ID のステートへ遷移する。
    /// 未登録 ID の場合は警告を出し遷移しない。
    /// </summary>
    public void ChangeState(PlayerStateId next)
    {
        if (!_states.TryGetValue(next, out var state))
        {
            Debug.LogWarning($"PlayerStateMachine: {next} is not registered.");
            return;
        }

        if (_currentState == state)
        {
            return;
        }

        _currentState?.Exit();
        _currentState = state;
        _currentState.Enter();
    }

    /// <summary>MonoBehaviour.Update 相当の処理を現在ステートへ委譲する。</summary>
    public void Update(float deltaTime)
    {
        Context?.Sprint?.Tick(deltaTime);
        _currentState?.Update(deltaTime);
    }

    /// <summary>MonoBehaviour.FixedUpdate 相当の処理を現在ステートへ委譲する。</summary>
    public void FixedUpdate(float fixedDeltaTime)
    {
        _currentState?.FixedUpdate(fixedDeltaTime);
    }

    /// <summary>移動入力を現在ステートへ転送する。</summary>
    public void HandleMove(Vector2 input) => _currentState?.OnMove(input);

    /// <summary>スプリント開始入力を現在ステートへ転送する。</summary>
    public void HandleSprintStarted() => _currentState?.OnSprintStarted();

    /// <summary>スプリント解除入力を現在ステートへ転送する。</summary>
    public void HandleSprintCanceled() => _currentState?.OnSprintCanceled();

    /// <summary>ライト攻撃入力を現在ステートへ転送する。</summary>
    public void HandleLightAttack() => _currentState?.OnLightAttack();

    /// <summary>強攻撃入力を現在ステートへ転送する。</summary>
    public void HandleStrongAttack() => _currentState?.OnStrongAttack();

    /// <summary>アニメーションイベント経由でコンボ受付開始を通知する。</summary>
    public void HandleComboWindowOpened() => _currentState?.OnComboWindowOpened();

    /// <summary>アニメーションイベント経由でコンボ受付終了を通知する。</summary>
    public void HandleComboWindowClosed() => _currentState?.OnComboWindowClosed();

    /// <summary>攻撃アニメーション終了イベントを現在ステートへ通知する。</summary>
    public void HandleAttackAnimationFinished() => _currentState?.OnAttackAnimationFinished();
}