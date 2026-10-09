using UnityEngine;
using UniRx;
using System;

/// <summary>
/// 自傷（Self Sacrifice）ステート。
/// チャネリングを開始し、停止はステート外からのキャンセルやゲージ枯渇により行われます。
/// </summary>
public class PlayerSelfSacrificeState : PlayerState
{
    /// <summary>自傷能力の継続・表示と状態遷移で使用するContextを保持する。</summary>
    public PlayerSelfSacrificeState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.SelfSacrifice;

    private IDisposable _selfSacrificeActiveDisp;

    /// <summary>自傷能力を確認し、能力中の移動・表示を準備する。</summary>
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
        Context.Controller.PlayBuffAudio();

        //アビリティの終了を監視するための購読を設定
        _selfSacrificeActiveDisp?.Dispose();
        if (Context.SelfSacrifice != null)
        {
            _selfSacrificeActiveDisp = Context.SelfSacrifice.IsActiveRx.Subscribe(active =>
            {
                if (!active)
                {
                    //アビリティが終了したらエフェクトとBGMを停止する
                    Context?.CharacterEffect?.StopEffect_CharacterEffect();
                    Context.Controller.StopBuffAudio();
                    _selfSacrificeActiveDisp?.Dispose();
                    _selfSacrificeActiveDisp = null;
                }
            });
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

    /// <summary>自傷能力・バフ表示・専用音声を入力解除で終了する。</summary>
    public override void OnSelfSacrificeCanceled()
    {
        Context.SelfSacrifice?.End();
        Context?.CharacterEffect?.StopEffect_CharacterEffect();
        Context.Controller.StopBuffAudio();
    }

    /// <summary>自傷能力を継続したまま弱攻撃状態へ移る。</summary>
    public override void OnLightAttack()
    {
        // 攻撃は SelfSacrifice 中でも可能にする（SelfSacrifice は Exit で自動的に End される）
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
