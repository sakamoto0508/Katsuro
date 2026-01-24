using UnityEngine;

/// <summary>
/// ライト攻撃コンボを管理するステート。ロックオン状態に応じて別コンボリストを参照する。
/// </summary>
public sealed class PlayerLightAttackState : PlayerAttackState
{
    private bool _isLockOnCombo;

    public PlayerLightAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetLightAttackDuration() ?? 0.8f)
    {
    }

    public override PlayerStateId Id => PlayerStateId.LightAttack;

    public override void Enter()
    {
        _isLockOnCombo = Context?.IsLockOn ?? false;
        base.Enter();
    }

    /// <summary>
    /// ScriptableObject のクリップ数を最大段数として返す（ロックオン種別で切り替え）。
    /// </summary>
    protected override int MaxComboSteps
    {
        get
        {
            var config = Context?.StateConfig;
            if (config == null)
            {
                return base.MaxComboSteps;
            }

            return config.GetLightAttackComboCount(_isLockOnCombo);
        }
    }

    /// <summary>
    /// 段数ごとのクリップ長からタイムアウト秒数を決定する（ロックオン別）。
    /// </summary>
    protected override float ResolveAttackDuration(int comboStep)
    {
        var config = Context?.StateConfig;
        if (config == null)
        {
            return base.ResolveAttackDuration(comboStep);
        }

        return config.GetLightAttackDuration(_isLockOnCombo, comboStep);
    }

    /// <summary>
    /// 段数とロックオン状態に応じたライト攻撃アニメーションを再生する。
    /// </summary>
    protected override void TriggerAttack(int comboStep)
    {
        Context.Attacker?.PlayLightAttack(comboStep, _isLockOnCombo);
    }
}
