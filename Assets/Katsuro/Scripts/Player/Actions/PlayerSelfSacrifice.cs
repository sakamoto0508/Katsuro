using System;
using UnityEngine;

/// <summary>
/// 自傷（Self Sacrifice）能力。
/// </summary>
public sealed class PlayerSelfSacrifice : AbilityBase
{
    /// <summary>自傷能力が使用するゲージとコスト設定を基底能力へ渡す。</summary>
    public PlayerSelfSacrifice(SkillGauge gauge, SkillGaugeCostConfig costConfig = null)
        : base(gauge, costConfig)
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
    /// <returns>HP割合が開始しきい値より高く、ゲージが残っている場合はtrue。</returns>
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

    /// <summary>自傷の継続ゲージ消費へ掛ける追加倍率。HPの消費は通知先が担当する。</summary>
    public float CostMultiplier { get; set; } = 1f;
    /// <summary>継続ゲージを消費し、成功時は経過秒を購読者へ渡してHP消費を委譲する。ゲージ不足なら終了する。</summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;

        float cost = GetSelfSacrificeGaugePerSecond() * Mathf.Max(0f, CostMultiplier) * deltaTime;
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
