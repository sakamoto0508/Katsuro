using INab.VFXAssets;
using UnityEngine;

/// <summary>
/// 回復（Heal）ステート。
/// チャネリング回復を行い、停止は再度押下またはゲージ不足で行います。
/// </summary>
public class PlayerHealState : PlayerState
{
    /// <summary>回復能力と移動・表示を扱うための共有Contextを保持する。</summary>
    public PlayerHealState(PlayerStateContext context, PlayerStateMachine stateMachine)
        : base(context, stateMachine)
    {
    }

    public override PlayerStateId Id => PlayerStateId.Heal;

    /// <summary>回復能力を開始する唯一の入口。開始できた場合だけ回復中の表示・移動状態を開始する。</summary>
    public override void Enter()
    {
        if (Context?.Healer == null)
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }

        // デフォルト回復速度（%/s）
        float defaultPercentPerSecond = 5f;
        if (!Context.Healer.TryBegin(defaultPercentPerSecond))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
            return;
        }
        Context?.CharacterEffect?.PlayEffectByKey(Context.VFXConfig.PlayEffectHeal);
    }

    /// <summary>回復能力と能力用の表示を終了する。</summary>
    public override void Exit()
    {
        Context.Healer?.End();
        Context?.CharacterEffect?.StopEffect_CharacterEffect();
    }

    /// <summary>回復中の移動を更新し、能力が終了したら通常移動へ戻る。</summary>
    public override void Update(float deltaTime)
    {
        Context.Mover.Update();

        // 回復が停止していたらロコモーションへ遷移。
        if (!(Context.Healer?.IsHealing ?? false))
        {
            StateMachine.ChangeState(PlayerStateId.Locomotion);
        }
    }

    /// <summary>入力解除で回復能力を終了し、State更新を待たずに通常移動へ戻って回復演出を止める。</summary>
    public override void OnHealCanceled()
    {
        Context.Healer?.End();
        // 敗北直後など戦闘不能でUpdateが呼ばれない場合も、この時点でExitを通す。
        StateMachine.ChangeState(PlayerStateId.Locomotion);
    }
}
