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
        // スプリント開始要求（内部でゲージチェックを行う）
        Context.Sprint.BeginDash();
        Context.Mover.SetSprint(true);

        // 開始に失敗（ゲージ不足など）している場合は状態を戻す
        if (!Context.Sprint.IsDashing)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void Exit()
    {
        Context.Sprint.End();
        Context.Mover.SetSprint(false);
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();

        // ダッシュ継続フラグ（ゲージ枯渇で自動停止）を確認して戻す
        if (!Context.Sprint.IsDashing)
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
        // 入力で解除された場合は即座にダッシュ停止して戻る
        Context.Sprint.End();
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