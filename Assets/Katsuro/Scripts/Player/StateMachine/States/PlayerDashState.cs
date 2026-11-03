using UnityEngine;

/// <summary>Dash中の移動とゲージ消費を進め、解除や攻撃入力に応じて次のPlayer状態へ移る。</summary>
public sealed class PlayerDashState : PlayerState
{
    /// <summary>Dash状態から移動・ゲージ・状態遷移を操作するための共有Contextを保持する。</summary>
    public PlayerDashState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    private bool CanAttack => Context.Attacker != null;

    public override PlayerStateId Id => PlayerStateId.Dash;

    /// <summary>Dash能力を開始してMoverを高速移動へ切り替える。</summary>
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

    /// <summary>Dash能力と高速移動フラグを解除する。</summary>
    public override void Exit()
    {
        Context.Sprint.End();
        Context.Mover.SetSprint(false);
    }

    /// <summary>Dash中の移動を更新し、Dash能力が終了したら通常移動へ戻る。</summary>
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();

        // ダッシュ継続フラグ（ゲージ枯渇で自動停止）を確認して戻す
        if (!Context.Sprint.IsDashing)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>Dash中のRigidbody移動をMoverの物理更新へ委譲する。</summary>
    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover.FixedUpdate();
    }

    /// <summary>Dash解除入力で通常移動状態へ戻る。</summary>
    public override void OnSprintCanceled()
    {
        // 入力で解除された場合は即座にダッシュ停止して戻る
        Context.Sprint.End();
        StateMachine.ChangeState(PlayerStateId.Locomotion);
    }

    /// <summary>攻撃依存先がある場合にDashから弱攻撃状態へ移る。</summary>
    public override void OnLightAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.LightAttack);
        }
    }

    /// <summary>攻撃依存先がある場合にDashから強攻撃状態へ移る。</summary>
    public override void OnStrongAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.StrongAttack);
        }
    }
}