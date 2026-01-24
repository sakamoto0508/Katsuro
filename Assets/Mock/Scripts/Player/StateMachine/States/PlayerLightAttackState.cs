using UnityEngine;

/// <summary>
/// ライト攻撃コンボを管理するステート。登録クリップ数に応じて段数を自動決定する。
/// </summary>
public sealed class PlayerLightAttackState : PlayerAttackState
{
    public PlayerLightAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetLightAttackDuration() ?? 0.8f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.LightAttack;

    /// <summary>
    /// ScriptableObject のクリップ数を最大段数として返す。
    /// </summary>
    protected override int MaxComboSteps
    {
        get
        {
            var clips = Context?.StateConfig?.LightAttackClips;
            int count = clips?.Count ?? 0;
            return Mathf.Max(1, count);
        }
    }

    /// <summary>
    /// 段数ごとのクリップ長からタイムアウト秒数を決定する。
    /// </summary>
    protected override float ResolveAttackDuration(int comboStep)
    {
        if (Context?.StateConfig == null)
        {
            return base.ResolveAttackDuration(comboStep);
        }

        return Context.StateConfig.GetLightAttackDuration(comboStep);
    }

    /// <summary>
    /// 段数に応じたライト攻撃アニメーションを再生する。
    /// </summary>
    protected override void TriggerAttack(int comboStep)
    {
        Context.Attacker?.PlayLightAttack(comboStep);
    }
}
