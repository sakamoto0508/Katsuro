using System;
using UnityEngine;

/// <summary>
/// ゴースト（幽霊化）処理：起動コスト／継続コストのチェックとゲージ消費を担当します。
/// 実際の当たり判定無効化や演出は呼び出し側（PlayerController / ステート）が行う。
/// </summary>
public class PlayerChost
{
    public PlayerChost(SkillGauge skillGauge, SkillGaugeCostConfig costConfig
        , PlayerStateConfig fallbackStateConfig = null)
    {
        _skillGauge = skillGauge ?? throw new ArgumentNullException(nameof(skillGauge));
        _costConfig = costConfig;
        _fallbackStateConfig = fallbackStateConfig;
    }

    /// <summary>現在ゴースト中か。</summary>
    public bool IsGhosting => _isActive && _skillGauge.Value > Mathf.Epsilon;

    private readonly SkillGauge _skillGauge;
    private readonly SkillGaugeCostConfig _costConfig;
    private readonly PlayerStateConfig _fallbackStateConfig;
    private bool _isActive;

    /// <summary>ゴースト起動コスト（ワンタイム）。</summary>
    public float GetActivationCost()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.GhostActivationCost) : 20f;

    /// <summary>ゴーストの継続コスト（1秒あたり）。</summary>
    public float GetPerSecondCost()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.GhostPerSecondCost) : 5f;

    /// <summary>
    /// ゴースト化を試行します。起動コストを即時消費できれば開始して true を返します。
    /// </summary>
    public bool TryBegin()
    {
        float cost = GetActivationCost();
        if (_skillGauge.TryConsume(cost))
        {
            _isActive = true;
            return true;
        }
        return false;
    }

    /// <summary>ゴーストを終了する（継続消費を止める）。</summary>
    public void End()=>_isActive = false;

    /// <summary>
    /// 毎フレームの進行処理。継続コストを消費し、ゲージ不足であれば自動終了します。
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (_isActive)
        {
            float cost = GetPerSecondCost() * deltaTime;
            if (!_skillGauge.TryConsume(cost))
            {
                // ゲージ枯渇でゴースト解除
                _isActive = false;
            }
        }
    }
}
