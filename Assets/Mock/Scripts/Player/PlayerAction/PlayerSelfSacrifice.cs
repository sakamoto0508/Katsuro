using System;
using UnityEngine;

/// <summary>
/// 自傷（Self Sacrifice）能力。
/// </summary>
public sealed class PlayerSelfSacrifice : AbilityBase
{
    public PlayerSelfSacrifice(SkillGauge gauge, SkillGaugeCostConfig costConfig = null
        , PlayerStateConfig fallbackStateConfig = null)
        : base(gauge, costConfig, fallbackStateConfig)
    {
    }

    /// <summary>
    /// 現在自傷（チャネリング）中か。
    /// </summary>
    public bool IsSacrificing => IsActive && _skillGauge.Value > Mathf.Epsilon;

    /// <summary>
    /// 指定した現在 HP 割合（0..1）で自傷を開始可能か判定します。
    /// - 最小許容比率は設定 (SkillGaugeCostConfig.SelfSacrificeMinHpRatio) に従います。
    /// </summary>
    /// <param name="currentHpRatio">現在 HP 割合（0..1）。</param>
    public bool CanBegin(float currentHpRatio)
    {
        if (currentHpRatio <= 0f) return false;
        float min = GetSelfSacrificeMinHpRatio();
        return currentHpRatio > min && _skillGauge.Value > Mathf.Epsilon;
    }

    /// <summary>
    /// 自傷を開始します（事前に <see cref="CanBegin"/> で判定してください）。
    /// </summary>
    public void Begin()
    {
        if (_skillGauge.Value > Mathf.Epsilon)
            SetActive(true);
    }

    /// <summary>自傷を終了します。</summary>
    public override void End()
    {
        base.End();
    }

    /// <summary>
    /// 毎フレームの進行処理：
    /// - 継続コスト (ゲージ) を消費できれば PublishConsumed(deltaTime) で購読者へ経過秒を通知します（購読者側で HP を減らす）。
    /// - ゲージ不足なら自動終了します。
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;

        float cost = GetSelfSacrificeGaugePerSecond() * deltaTime;
        if (_skillGauge.TryConsume(cost))
        {
            // 通知内容: このフレーム分の経過秒。購読者が仕様に沿って HP を減らす。
            PublishConsumed(deltaTime);
            return;
        }

        // ゲージ不足で自動終了
        SetActive(false);
    }
}
