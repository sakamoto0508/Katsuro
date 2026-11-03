using UnityEngine;

/// <summary>幽体化中の移動・見た目・回避受付を管理し、解除や攻撃に応じて状態を切り替える。</summary>
public class PlayerGhostState : PlayerState
{
    /// <summary>幽体化状態が使用する能力・表示・遷移管理のContextを保持する。</summary>
    public PlayerGhostState(PlayerStateContext context, PlayerStateMachine stateMachine) : base(context, stateMachine) { }
    public override PlayerStateId Id => PlayerStateId.Ghost;
    private float _justRemaining;
    /// <summary>幽体化能力の有効性を確認し、見た目と短時間のジャスト回避受付を開始する。</summary>
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
    /// <summary>幽体化能力・回避受付・表示を解除して通常姿勢へ戻す。</summary>
    public override void Exit()
    {
        Context.Ghost?.End();
        Context.IsGhostMode = false;
        Context.SetJustAvoidWindow(false);
        Context.Controller.SetGhostVisual(false);
    }
    /// <summary>移動と回避受付時間を進め、幽体化能力が終了したら通常移動へ戻る。</summary>
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        _justRemaining -= deltaTime;
        if (_justRemaining <= 0f) Context.SetJustAvoidWindow(false);
        if (!Context.Ghost.IsGhosting) StateMachine.ChangeState(PlayerStateId.Locomotion);
    }
    /// <summary>幽体化中の移動をMoverの物理更新へ委譲する。</summary>
    public override void FixedUpdate(float deltaTime) => Context.Mover.FixedUpdate();
    /// <summary>解除入力で幽体化状態を終了し、通常移動へ戻る。</summary>
    public override void OnGhostCanceled() => StateMachine.ChangeState(PlayerStateId.Locomotion);
    /// <summary>幽体化から弱攻撃へ状態を切り替える。</summary>
    public override void OnLightAttack() => StateMachine.ChangeState(PlayerStateId.LightAttack);
    /// <summary>幽体化から強攻撃へ状態を切り替える。</summary>
    public override void OnStrongAttack() => StateMachine.ChangeState(PlayerStateId.StrongAttack);
}
