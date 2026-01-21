using UnityEngine;

public sealed class PlayerStrongAttackState : PlayerAttackState
{
    public PlayerStrongAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetStrongAttackDuration() ?? 1.0f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.StrongAttack;

    protected override void TriggerAttack()
    {
        Context.Attacker?.PlayStrongAttack();
    }
}
