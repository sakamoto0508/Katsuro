using System;
using UnityEngine;

/// <summary>
/// チャネリング回復ハンドラ（秒あたりの回復率を指定して開始／継続消費を行う）。
/// PublishConsumed で「このフレームで回復した割合（%）」を通知します。実際の HP 増加は購読側で行ってください。
/// </summary>
public sealed class PlayerHeal : AbilityBase
{
    public PlayerHeal(SkillGauge gauge, PlayerMover playerMover, SkillGaugeCostConfig costConfig = null)
        : base(gauge, costConfig)
    {
        _plaeyrMover = playerMover;
    }

    /// <summary>現在チャネリング回復中か。</summary>
    public bool IsHealing => IsActive && _skillGauge.Value > Mathf.Epsilon;

    private PlayerMover _plaeyrMover;

    /// <summary>
    /// チャネリング回復を開始する。percentPerSecond は「秒あたりの回復率(%)」。
    /// 0 以下だと開始に失敗します。
    /// </summary>
    public bool TryBegin(float percentPerSecond)
    {
        if (percentPerSecond <= 0f) return false;
        if (_skillGauge.Value <= Mathf.Epsilon) return false;

        _healPercentPerSecond = percentPerSecond;
        SetActive(true);
        _plaeyrMover?.MoveStop();
        return true;
    }

    /// <summary>回復チャネリングを終了する。</summary>
    public override void End()
    {
        base.End();
    }

    /// <summary>
    /// Tick: このフレームで回復する割合(%) を計算し、必要ゲージを消費できれば PublishConsumed(percent) を呼ぶ。
    /// 不足時は可能な分だけ回復してチャネリングを終了する。
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;

        float percentToHeal = _healPercentPerSecond * deltaTime; // このフレームで回復する%（0..100）
        if (percentToHeal <= 0f) return;

        float costPerPercent = GetHealGaugePerPercent(); // ゲージ / 1%
        float requiredCost = percentToHeal * costPerPercent;

        if (_skillGauge.TryConsume(requiredCost))
        {
            PublishConsumed(percentToHeal);
            return;
        }

        // 足りない分だけ回復して終了
        float available = _skillGauge.Value;
        if (available > Mathf.Epsilon)
        {
            float affordablePercent = available / costPerPercent;
            if (_skillGauge.TryConsume(available))
            {
                PublishConsumed(affordablePercent);
            }
        }

        SetActive(false);
    }

    private float _healPercentPerSecond;
}
