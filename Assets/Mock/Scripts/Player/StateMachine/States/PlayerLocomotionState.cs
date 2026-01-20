using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    public PlayerLocomotionState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Locomotion;

    public override void Enter()
    {
        Context.Mover.SetSprint(false);
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
    }

    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover.FixedUpdate();
    }

    public override void OnSprintStarted()
    {
        if (Context.Sprint.CanDash)
        {
            StateMachine.ChangeState(PlayerStateId.Dash);
        }
    }
}