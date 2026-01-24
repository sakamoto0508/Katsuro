using UnityEngine;

/// <summary>
/// 強攻撃コンボを管理するステート。強攻撃用クリップリストを参照して段数と時間を決定する。
/// </summary>
public sealed class PlayerStrongAttackState : PlayerAttackState
{
    public PlayerStrongAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetStrongAttackDuration() ?? 1.0f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.StrongAttack;

    /// <summary>
    /// 強攻撃用クリップ数を最大段数として返す。
    /// </summary>
    protected override int MaxComboSteps
    {
        get
        {
            var clips = Context?.StateConfig?.StrongAttackClips;
            int count = clips?.Count ?? 0;
            return Mathf.Max(1, count);
        }
    }

    /// <summary>
    /// 段数ごとのクリップ長を元に攻撃継続時間を決める。
    /// </summary>
    protected override float ResolveAttackDuration(int comboStep)
    {
        if (Context?.StateConfig == null)
        {
            return base.ResolveAttackDuration(comboStep);
        }

        return Context.StateConfig.GetStrongAttackDuration(comboStep);
    }

    /// <summary>
    /// 段数に応じた強攻撃アニメーションを再生する。
    /// </summary>
    protected override void TriggerAttack(int comboStep)
    {
        Context.Attacker?.PlayStrongAttack(comboStep);
    }
}
