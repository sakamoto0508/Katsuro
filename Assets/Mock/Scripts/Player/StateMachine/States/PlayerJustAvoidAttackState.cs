using UnityEngine;

public sealed class PlayerJustAvoidAttackState : PlayerAttackState
{
    public PlayerJustAvoidAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetJustAvoidAttackDuration() ?? 0.9f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.JustAvoidAttack;

    protected override void TriggerAttack()
    {
        Context.Attacker?.PlayJustAvoidAttack();
    }
}
