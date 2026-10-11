using UnityEngine;

/// <summary>
/// 自傷（Self Sacrifice）ステート。
/// 能力中の移動と攻撃への遷移を扱います。能力の寿命と表示はStateから独立して管理します。
/// </summary>
public class PlayerSelfSacrificeState : PlayerState
{
    /// <summary>自傷中の移動と状態遷移で使用するContextを保持する。</summary>
    public PlayerSelfSacrificeState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.SelfSacrifice;

    /// <summary>能力が開始済みであることを確認し、未開始なら通常移動へ戻す。</summary>
    public override void Enter()
    {
        if (Context?.SelfSacrifice == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        // 開始可否と寿命はAbilityManager、表示とBGMはPlayerBuffPresentationが所有する。
        if (!Context.SelfSacrifice.IsSacrificing)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }
    }

    /// <summary>自傷能力の寿命はAbilityManagerに任せ、攻撃への遷移でも効果を継続させる。</summary>
    public override void Exit()
    {
        // SelfSacrifice は AbilityManager 側で管理しているため、
        // ステート離脱時に自動で End しない（攻撃中も継続したい）。
        // 終了は入力キャンセルやゲージ枯渇など Ability 側の判定で行う。
    }

    /// <summary>移動を更新し、自傷能力が終了したら通常移動へ戻る。</summary>
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        // Ability 側で終了判定を行う（ゲージ枯渇など）
        if (!(Context.SelfSacrifice?.IsSacrificing ?? false))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>入力解除を能力へ伝え、表示と専用音声の終了は能力通知へ任せる。</summary>
    public override void OnSelfSacrificeCanceled()
    {
        Context.SelfSacrifice?.End();
    }

    /// <summary>自傷能力を継続したまま弱攻撃状態へ移る。</summary>
    public override void OnLightAttack()
    {
        // 攻撃へ移ってもSelfSacrificeの能力と表示を継続する。
        StateMachine.ChangeState(PlayerStateId.LightAttack);
    }

    /// <summary>自傷能力を継続したまま強攻撃状態へ移る。</summary>
    public override void OnStrongAttack()
    {
        StateMachine.ChangeState(PlayerStateId.StrongAttack);
    }

    /// <summary>自傷中もMoverへ物理更新を委譲して移動を維持する。</summary>
    public override void FixedUpdate(float deltaTime)
    {
        // 物理更新は Mover 側で行う（移動を有効にするため必須）
        Context.Mover.FixedUpdate();
    }
}
