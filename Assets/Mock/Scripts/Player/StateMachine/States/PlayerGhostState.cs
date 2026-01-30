using System;
using UnityEngine;

public class PlayerGhostState : PlayerState
{
    public PlayerGhostState(PlayerStateContext context, PlayerStateMachine staeMachine)
        : base(context, staeMachine)
    {

    }

    public override PlayerStateId Id => PlayerStateId.Ghost;
    private float _startJustAvoidRemaining = 0f;

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
            Context.SetJustAvoidWindow(true);
            _startJustAvoidRemaining = Context.StateConfig.JustAvoidTime;
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
        // ジャスト回避ウィンドウが残っていれば解除
        if (Context.IsInJustAvoidWindow)
        {
            Context.SetJustAvoidWindow(false);
            _startJustAvoidRemaining = 0f;
        }
        Context.IsGhostMode = false;
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

        // ジャスト回避ウィンドウの経過処理
        if (_startJustAvoidRemaining > 0f)
        {
            _startJustAvoidRemaining -= deltaTime;
            if (_startJustAvoidRemaining <= 0f)
            {
                _startJustAvoidRemaining = 0f;
                if (Context.IsInJustAvoidWindow)
                    Context.SetJustAvoidWindow(false);
            }
        }

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
        Context.Ghost?.End();
        Context.IsGhostMode = false;
        // ジャスト回避ウィンドウが残っていれば解除。
        if (Context.IsInJustAvoidWindow)
        {
            Context.SetJustAvoidWindow(false);
            _startJustAvoidRemaining = 0f;
        }
    }

    public override void OnGhostStarted()
    {
        if (Context.Ghost == null) return;
        if (Context.Ghost.IsActive) return;

        // 再度試行（成功すればゴーストに復帰）。
        if (Context.Ghost.TryBegin())
        {
            Context.IsGhostMode = true;
            Context.SetJustAvoidWindow(true);
            _startJustAvoidRemaining = Context.StateConfig.JustAvoidTime;
        }
    }
}
