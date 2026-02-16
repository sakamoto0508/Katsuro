using UnityEngine;
using UniRx;
using System;

/// <summary>
/// 自傷（Self Sacrifice）ステート。
/// チャネリングを開始し、停止はステート外からのキャンセルやゲージ枯渇により行われます。
/// </summary>
public class PlayerSelfSacrificeState : PlayerState
{
    public PlayerSelfSacrificeState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.SelfSacrifice;

    private IDisposable _selfSacrificeActiveDisp;

    public override void Enter()
    {
        if (Context?.SelfSacrifice == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        // 現在HP比率を元に開始可否を判定
        float currentHpRatio = Context.PlayerResource != null ? Context.PlayerResource.CurrentHpRatio : 1f;
        if (Context.SelfSacrifice.CanBegin(currentHpRatio))
        {
            Context.SelfSacrifice.Begin();
        }
        else
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
        Context?.CharacterEffect?.PlayEffectByKey(Context.VFXConfig.PlayEffectBuff);
        AudioManager.Instance.PlayBGM("BuffBGM", 2, 1f);

        // Subscribe to ability active state so we stop effects/BGM only when ability actually ends.
        // Dispose any previous subscription before creating a new one.
        _selfSacrificeActiveDisp?.Dispose();
        if (Context.SelfSacrifice != null)
        {
            _selfSacrificeActiveDisp = Context.SelfSacrifice.IsActiveRx.Subscribe(active =>
            {
                if (!active)
                {
                    // Ability ended (gauge depleted or canceled) -> stop visuals and sound
                    Context?.CharacterEffect?.StopEffect_CharacterEffect();
                    AudioManager.Instance?.StopBGM(2);
                    // cleanup subscription since ability has ended
                    _selfSacrificeActiveDisp?.Dispose();
                    _selfSacrificeActiveDisp = null;
                }
            });
        }
    }

    public override void Exit()
    {
        // SelfSacrifice は AbilityManager 側で管理しているため、
        // ステート離脱時に自動で End しない（攻撃中も継続したい）。
        // 終了は入力キャンセルやゲージ枯渇など Ability 側の判定で行う。
        // Do not stop effect/BGM here; those are stopped when the ability actually ends.
        // Keep subscription active so we can detect the ability end even if the state exits (e.g. for attacks).
    }

    public override void Update(float deltaTime)
    {
        Context.Mover.Update();
        // Ability 側で終了判定を行う（ゲージ枯渇など）
        if (!(Context.SelfSacrifice?.IsSacrificing ?? false))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    public override void OnSelfSacrificeCanceled()
    {
        Context.SelfSacrifice?.End();
        // Stop immediately on explicit cancel
        Context?.CharacterEffect?.StopEffect_CharacterEffect();
        AudioManager.Instance?.StopBGM(2);
    }

    public override void OnLightAttack()
    {
        // 攻撃は SelfSacrifice 中でも可能にする（SelfSacrifice は Exit で自動的に End される）
        StateMachine.ChangeState(PlayerStateId.LightAttack);
    }

    public override void OnStrongAttack()
    {
        StateMachine.ChangeState(PlayerStateId.StrongAttack);
    }

    public override void FixedUpdate(float deltaTime)
    {
        // 物理更新は Mover 側で行う（移動を有効にするため必須）
        Context.Mover.FixedUpdate();
    }
}
