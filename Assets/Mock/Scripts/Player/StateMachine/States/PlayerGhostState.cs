using UnityEngine;

public class PlayerGhostState : PlayerState
{
    public PlayerGhostState(PlayerStateContext context, PlayerStateMachine stateMachine) : base(context, stateMachine) { }
    public override PlayerStateId Id => PlayerStateId.Ghost;
    private float _justRemaining;
    public override void Enter()
    {
        if (Context.Ghost == null || !Context.Ghost.IsActive)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }
        Context.IsGhostMode = true;
        _justRemaining = (Context.StateConfig != null ? Context.StateConfig.JustAvoidTime : 0.2f)
            * GameplayRules.Current.AvoidWindow(Context.PlayerResource.CurrentHpRatio);
        Context.SetJustAvoidWindow(_justRemaining > 0f);
        Context.Controller.SetGhostVisual(true);
    }
    public override void Exit()
    {
        Context.Ghost?.End();
        Context.IsGhostMode = false;
        Context.SetJustAvoidWindow(false);
        Context.Controller.SetGhostVisual(false);
    }
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        _justRemaining -= deltaTime;
        if (_justRemaining <= 0f) Context.SetJustAvoidWindow(false);
        if (!Context.Ghost.IsGhosting) StateMachine.ChangeState(PlayerStateId.Locomotion);
    }
    public override void FixedUpdate(float deltaTime) => Context.Mover.FixedUpdate();
    public override void OnGhostCanceled() => StateMachine.ChangeState(PlayerStateId.Locomotion);
    public override void OnLightAttack() => StateMachine.ChangeState(PlayerStateId.LightAttack);
    public override void OnStrongAttack() => StateMachine.ChangeState(PlayerStateId.StrongAttack);
}
