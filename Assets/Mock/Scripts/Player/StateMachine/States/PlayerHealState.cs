using UnityEngine;

/// <summary>
/// 回復（Heal）ステート。
/// チャネリング回復を行い、停止は再度押下またはゲージ不足で行います。
/// </summary>
public class PlayerHealState : PlayerState
{
    public PlayerHealState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Heal;

    public override void Enter()
    {
        if (Context?.Healer == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        // デフォルト回復速度（%/s）
        float defaultPercentPerSecond = 5f;
        if (!Context.Healer.TryBegin(defaultPercentPerSecond))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void Exit()
    {
        Context.Healer?.End();
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        if (!(Context.Healer?.IsHealing ?? false))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void OnHealCanceled()
    {
        Context.Healer?.End();
    }
}
