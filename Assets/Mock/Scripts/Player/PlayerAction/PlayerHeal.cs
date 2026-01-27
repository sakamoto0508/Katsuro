using UnityEngine;

public class PlayerHeal
{
    public PlayerHeal(SkillGauge skillGauge, SkillGaugeCostConfig costConfig)
    {
        _skillGauge = skillGauge ?? throw new System.ArgumentNullException(nameof(skillGauge));
        _costConfig = costConfig;
    }

    /// <summary>
    /// 現在回復中か。
    /// </summary>
    public bool IsHealing => _isActive && _skillGauge.Value > Mathf.Epsilon;

    private readonly SkillGauge _skillGauge;
    private readonly SkillGaugeCostConfig _costConfig;
    private bool _isActive = false;

    /// <summary>回復起動コスト（ワンタイム）。</summary>
    public float HealActivationCost()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.HealGaugePerPercent) : 2f;

    /// <summary>1% 回復あたりのゲージ消費量。</summary>
    public float GetHealGaugePerPercent()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.HealGaugePerPercent) : 2f;

    /// <summary>
    /// 回復化を指向する
    /// </summary>
    /// <returns></returns>
    public bool TryBegin()
    {
        float cost = HealActivationCost();
        if (_skillGauge.TryConsume(cost))
        {
            _isActive = true;
            return true;
        }
        return false;
    }

    /// <summary>回復を終了する。</summary>
    public void End() => _isActive = false;

    /// <summary>
    /// 指定パーセント分の回復に必要なゲージを消費する（percent は 0..100）。
    /// 成功したら true を返す。呼び出し側で実際に HP を増やしてください。
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        float cost = GetHealGaugePerPercent() * deltaTime;
        if (!_skillGauge.TryConsume(cost))
        {
            _isActive = false;
        }
    }
}
