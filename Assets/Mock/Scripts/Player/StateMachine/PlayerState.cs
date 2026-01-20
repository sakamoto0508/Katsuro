using UnityEngine;

public abstract class PlayerState
{
    protected PlayerState(PlayerStateContext context, PlayerStateMachine stateMachine)
    {
        Context = context;
        StateMachine = stateMachine;
    }

    protected PlayerStateContext Context { get; }
    protected PlayerStateMachine StateMachine { get; }

    public abstract PlayerStateId Id { get; }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update(float deltaTime) { }
    public virtual void FixedUpdate(float deltaTime) { }

    public virtual void OnMove(Vector2 input)
    {
        Context?.Mover?.OnMove(input);
    }

    public virtual void OnSprintStarted() { }
    public virtual void OnSprintCanceled() { }
}


public enum PlayerAttackType
{
    Light,
    Strong
}

public enum PlayerStateType
{
    Idle = 0,
    LockOn = 1 << 0,
    Sprint = 1 << 1,
    Ghost = 1 << 2,
    JustAvoid = 1 << 3,
}

public enum PlayerLifeState
{
    Alive,
    DamageTaking,
    Down,
    Dead
}