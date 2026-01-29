using System;
using UnityEngine;

/// <summary>
/// ダッシュ（スプリント）専任クラス。開始/終了と継続消費のみを担当します。
/// PublishConsumed で毎フレーム消費したゲージ量を通知します。
/// </summary>
public sealed class PlayerSprint : AbilityBase
{
    public PlayerSprint(SkillGauge gauge, SkillGaugeCostConfig costConfig = null)
        : base(gauge, costConfig)
    {
    }

    /// <summary>現在ダッシュ中か（ゲージが残っていることも含む）。</summary>
    public bool IsDashing => IsActive && _skillGauge.Value > Mathf.Epsilon;

    /// <summary>ダッシュ開始（呼び出し前に CanDash をチェックしてください）。</summary>
    public void BeginDash()
    {
        if (_skillGauge.Value > Mathf.Epsilon)
            SetActive(true);
    }

    /// <summary>ダッシュ終了。</summary>
    public override void End()
    {
        base.End();
    }

    /// <summary>
    /// 継続消費を行う。消費に成功すれば PublishConsumed(消費量) を呼ぶ。
    /// ゲージ不足なら自動でダッシュを停止します。
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;

        float cost = GetDashCostPerSecond() * deltaTime;
        if (_skillGauge.TryConsume(cost))
        {
            PublishConsumed(cost);
            return;
        }

        // 枯渇 -> 停止
        SetActive(false);
    }

    /// <summary>ダッシュ可能か（最低閾値を満たしているか）。</summary>
    public bool CanDash => _skillGauge.Value > Mathf.Epsilon;
}
