using UnityEngine;

/// <summary>
/// 自傷（Self Sacrifice）ステート。
/// チャネリングを開始し、停止はステート外からのキャンセルやゲージ枯渇により行われます。
/// </summary>
public class PlayerSelfSacrificeState : PlayerState
{
    public PlayerSelfSacrificeState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.SelfSacrifice;

    public override void Enter()
    {
        if (Context?.SelfSacrifice == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        // 現在HP比率を元に開始可否を判定
        float currentHpRatio = Context.PlayerResource != null ? Context.PlayerResource.CurrentHpRatio : 1f;
        if (Context.SelfSacrifice.CanBegin(currentHpRatio))
        {
            Context.SelfSacrifice.Begin();
        }
        else
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void Exit()
    {
        Context.SelfSacrifice?.End();
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        // Ability 側で終了判定を行う（ゲージ枯渇など）
        if (!(Context.SelfSacrifice?.IsSacrificing ?? false))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void OnSelfSacrificeCanceled()
    {
        Context.SelfSacrifice?.End();
    }

    public override void OnLightAttack()
    {
        // 攻撃は SelfSacrifice 中でも可能にする（SelfSacrifice は Exit で自動的に End される）
        StateMachine.ChangeState(PlayerStateId.LightAttack);
    }

    public override void OnStrongAttack()
    {
        StateMachine.ChangeState(PlayerStateId.StrongAttack);
    }

    public override void FixedUpdate(float deltaTime)
    {
        // 物理更新は Mover 側で行う（移動を有効にするため必須）
        Context.Mover.FixedUpdate();
    }
}
