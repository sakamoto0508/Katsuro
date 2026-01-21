using UnityEngine;

public sealed class PlayerLightAttackState : PlayerAttackState
{
    public PlayerLightAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetLightAttackDuration() ?? 0.8f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.LightAttack;

    protected override void TriggerAttack()
    {
        Context.Attacker?.PlayLightAttack();
    }
}
