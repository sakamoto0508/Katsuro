using UnityEngine;

/// <summary>通常移動の更新をMoverへ委譲し、能力や攻撃の入力で対応するPlayer状態へ移る。</summary>
public sealed class PlayerLocomotionState : PlayerState
{
    /// <summary>通常移動と各行動の開始に使用する共有Contextを保持する。</summary>
    public PlayerLocomotionState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    private bool CanAttack => Context.Attacker != null;

    public override PlayerStateId Id => PlayerStateId.Locomotion;

    /// <summary>前の状態から持ち越したDash速度フラグを解除する。</summary>
    public override void Enter()
    {
        Context.Mover.SetSprint(false);
    }

    /// <summary>通常移動の入力・方向・速度更新をMoverへ委譲する。</summary>
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
    }

    /// <summary>通常移動のRigidbody反映をMoverへ委譲する。</summary>
    public override void FixedUpdate(float deltaTime)
    {
        Context.Mover.FixedUpdate();
    }

    /// <summary>Dash能力が開始可能な場合だけDash状態へ移る。</summary>
    public override void OnSprintStarted()
    {
        if (Context.Sprint.CanDash)
        {
            StateMachine.ChangeState(PlayerStateId.Dash);
        }
    }

    /// <summary>幽体化の開始要求を対応する状態へ渡す。</summary>
    public override void OnGhostStarted()
    {
        StateMachine.ChangeState(PlayerStateId.Ghost);
    }

    /// <summary>自傷能力を扱う状態へ移る。</summary>
    public override void OnSelfSacrificeStarted()
    {
        StateMachine.ChangeState(PlayerStateId.SelfSacrifice);
    }

    /// <summary>回復能力を扱う状態へ移る。</summary>
    public override void OnHealStarted()
    {
        StateMachine.ChangeState(PlayerStateId.Heal);
    }

    /// <summary>攻撃依存先が存在する場合に弱攻撃状態へ移る。</summary>
    public override void OnLightAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.LightAttack);
        }
    }

    /// <summary>攻撃依存先が存在する場合に強攻撃状態へ移る。</summary>
    public override void OnStrongAttack()
    {
        if (CanAttack)
        {
            StateMachine.ChangeState(PlayerStateId.StrongAttack);
        }
    }
}