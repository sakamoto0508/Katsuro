using System;
using UnityEngine;

/// <summary>
/// ゴースト（幽霊化）能力。起動ワンタイムコストと継続消費を管理する。
/// 実際の演出／当たり判定切替はステート側で行ってください。
/// </summary>
public sealed class PlayerGhost : AbilityBase
{
    public PlayerGhost(SkillGauge gauge, SkillGaugeCostConfig costConfig = null)
        : base(gauge, costConfig)
    {
    }

    /// <summary>現在ゴースト中か（ゲージが枯渇していないことも確認）。</summary>
    public bool IsGhosting => IsActive && _skillGauge.Value > Mathf.Epsilon;

    /// <summary>
    /// ゴースト化を試行します。起動コストを即時消費できれば開始して true を返します。
    /// </summary>
    public bool TryBegin()
    {
        float activation = GetGhostActivationCost();
        if (_skillGauge.TryConsume(activation))
        {
            SetActive(true);
            Debug.Log($"PlayerGhost: TryBegin succeeded, activation cost={activation}");
            return true;
        }
        Debug.Log($"PlayerGhost: TryBegin failed, need={activation}, have={_skillGauge.Value}");
        return false;
    }

    /// <summary>ゴースト状態を終了する（継続消費を停止）。</summary>
    public override void End()
    {
        base.End();
    }

    /// <summary>
    /// 継続消費を行う。消費に成功すれば PublishConsumed(消費量) を呼ぶ。
    /// ゲージ不足なら自動終了する。
    /// </summary>
    public override void Tick(float deltaTime)
    {
        if (!IsActive || deltaTime <= 0f) return;

        float cost = GetGhostPerSecondCost() * deltaTime;
        if (_skillGauge.TryConsume(cost))
        {
            PublishConsumed(cost); // 通知: 消費したゲージ量
            Debug.Log($"PlayerGhost: Tick consumed {cost} (remaining {_skillGauge.Value})");
            return;
        }

        // 枯渇により自動終了
        Debug.Log("PlayerGhost: Tick failed to consume, ending ghost");
        SetActive(false);
    }
}
