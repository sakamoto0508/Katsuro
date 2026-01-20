using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerStateMachine
{
    private readonly Dictionary<PlayerStateId, PlayerState> _states;
    private PlayerState _currentState;

    public PlayerStateMachine(PlayerStateContext context)
    {
        Context = context;

        _states = new Dictionary<PlayerStateId, PlayerState>
        {
            //{ PlayerStateId.Locomotion, new PlayerLocomotionState(context, this) },
            //{ PlayerStateId.Dash, new PlayerDashState(context, this) }
        };

        ChangeState(PlayerStateId.Locomotion);
    }

    public PlayerStateContext Context { get; }

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

    public void Update(float deltaTime)
    {
        //Context?.Sprint?.Tick(deltaTime);
        _currentState?.Update(deltaTime);
    }

    public void FixedUpdate(float fixedDeltaTime)
    {
        _currentState?.FixedUpdate(fixedDeltaTime);
    }

    public void HandleMove(Vector2 input) => _currentState?.OnMove(input);
    public void HandleSprintStarted() => _currentState?.OnSprintStarted();
    public void HandleSprintCanceled() => _currentState?.OnSprintCanceled();
}