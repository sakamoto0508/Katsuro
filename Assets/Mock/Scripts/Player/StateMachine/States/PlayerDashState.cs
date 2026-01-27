using UnityEngine;

public sealed class PlayerDashState : PlayerState
{
    public PlayerDashState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    private bool CanAttack => Context.Attacker != null;

    public override PlayerStateId Id => PlayerStateId.Dash;

    public override void Enter()
    {
        Context.Sprint.BeginDash();
        Context.Mover.SetSprint(true);
    }

    public override void Exit()
    {
        Context.Sprint.EndDash();
        Context.Mover.SetSprint(false);
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();

        if (!Context.Sprint.IsSprint)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover.FixedUpdate();
    }

    public override void OnSprintCanceled()
    {
        StateMachine.ChangeState(PlayerStateId.Locomotion);
    }

    public override void OnLightAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.LightAttack);
        }
    }

    public override void OnStrongAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.StrongAttack);
        }
    }
}