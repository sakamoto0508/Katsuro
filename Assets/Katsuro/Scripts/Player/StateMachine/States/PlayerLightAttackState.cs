using UnityEngine;

/// <summary>
/// ライト攻撃コンボを管理するステート。ロックオン状態に応じて別コンボリストを参照する。
/// </summary>
public sealed class PlayerLightAttackState : PlayerAttackState
{
    /// <summary>弱攻撃用の設定時間と共有Contextを共通攻撃状態へ渡す。</summary>
    public PlayerLightAttackState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine, context?.StateConfig?.GetLightAttackDuration() ?? 0.8f)
    {
    }

    private bool _isLockOnCombo;

    public override PlayerStateId Id => PlayerStateId.LightAttack;

    /// <summary>攻撃開始時のLock-On種別を固定して、共通の弱攻撃・コンボ進行を開始する。</summary>
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
    /// 段数に応じたコンボ受付ディレイを設定する（ロックオン別）。
    /// </summary>
    protected override float ResolveComboWindowDelay(int comboStep)
    {
        var config = Context?.StateConfig;
        if (config == null)
        {
            return base.ResolveComboWindowDelay(comboStep);
        }

        return config.GetLightAttackComboWindowDelay(_isLockOnCombo, comboStep);
    }

    /// <summary>
    /// 段数とロックオン状態に応じたライト攻撃アニメーションを再生する。
    /// </summary>
    protected override void TriggerAttack(int comboStep)
    {
        Context.Attacker?.PlayLightAttack(comboStep, _isLockOnCombo);
    }
}
