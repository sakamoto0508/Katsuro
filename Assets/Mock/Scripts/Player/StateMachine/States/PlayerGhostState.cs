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

        // ゴーストの起動は AbilityManager 側で行う（段階移行）。
        // 既にアクティブになっているか確認し、そうでなければ戻る。
        if (Context.Ghost.IsActive)
        {
            Context.IsGhostMode = true;
            Context.SetJustAvoidWindow(true);
            _startJustAvoidRemaining = Context.StateConfig.JustAvoidTime;
        }
        else
        {
            // ゴースト未起動なら通常移行に戻す
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
        // Ghost の開始は AbilityManager 側で処理されるため、ここでは
        // アクティブになっているかを見て状態を更新するのみ。
        if (Context.Ghost == null) return;
        if (!Context.Ghost.IsActive) return;
        Context.IsGhostMode = true;
        Context.SetJustAvoidWindow(true);
        _startJustAvoidRemaining = Context.StateConfig.JustAvoidTime;
    }
}
