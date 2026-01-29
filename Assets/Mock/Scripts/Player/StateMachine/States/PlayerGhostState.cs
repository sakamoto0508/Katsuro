using UnityEngine;

public class PlayerGhostState :PlayerState
{
    public PlayerGhostState(PlayerStateContext context,PlayerStateMachine staeMachine)
        : base(context, staeMachine)
    {
        
    }

    public override PlayerStateId Id => PlayerStateId.Ghost;

    public override void Enter()
    {
        // 試行：Ability の起動を要求
        if (Context?.Ghost == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        if (Context.Ghost.TryBegin())
        {
            Context.IsGhostMode = true;
            // 即時にヒットボックスを無効化（アニメイベント待ちではなく明示的に）
            Context.Attacker?.DisableWeaponHitbox();
        }
        else
        {
            // 起動失敗（ゲージ不足等）は即座に戻す
            Context.IsGhostMode = false;
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void Exit()
    {
        base.Exit();
        Context.Ghost?.End();
    }

    public override void Update(float deltaTime)
    {
        // 安全ガード
        if (Context?.Ghost == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        Context.Mover.Update();

        // Ability 側が枯渇・終了していれば戻す
        if (!Context.Ghost.IsGhosting)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover.FixedUpdate();
    }

    public override void OnGhostCanceled()
    {
        // 入力等で解除が来たときは Ability を終了してクールダウンへ
        Context.Ghost?.End();
        Context.IsGhostMode = false;
    }

    public override void OnGhostStarted()
    {
        if (Context.Ghost == null) return;
        if (Context.Ghost.IsActive) return;

        // 再度試行（成功すればゴーストに復帰）
        if (Context.Ghost.TryBegin())
        {
            Context.IsGhostMode = true;
        }
    }
}
