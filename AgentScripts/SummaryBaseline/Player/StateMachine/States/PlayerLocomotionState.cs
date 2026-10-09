using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    public PlayerLocomotionState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    private bool CanAttack => Context.Attacker != null;

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

    public override void OnGhostStarted()
    {
        StateMachine.ChangeState(PlayerStateId.Ghost);
    }

    public override void OnSelfSacrificeStarted()
    {
        StateMachine.ChangeState(PlayerStateId.SelfSacrifice);
    }

    public override void OnHealStarted()
    {
        StateMachine.ChangeState(PlayerStateId.Heal);
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