using UnityEngine;

/// <summary>ボタンを押している間は幽体化する。開始時のゲージ不足では、延長できない短時間の幽体化を行う。</summary>
public sealed class PlayerGhost : AbilityBase
{
    public PlayerGhost(SkillGauge gauge, SkillGaugeCostConfig costConfig = null) : base(gauge, costConfig) { }
    private float _briefRemaining;
    private float _cooldownRemaining;
    private bool _brief;
    public bool IsGhosting => IsActive;
    public bool IsBrief => _brief;
    public bool TryBegin()
    {
        if (IsActive || _cooldownRemaining > 0f) return false;
        float activation = GetGhostActivationCost() * RunSession.GhostCostMultiplier;
        _brief = !_skillGauge.TryConsume(activation);
        if (_brief)
        {
            _skillGauge.TryConsume(_skillGauge.Value);
            _briefRemaining = Mathf.Max(0.01f, GameplayRules.Current.ShortGhostDuration);
        }
        SetActive(true);
        return true;
    }
    public override void End()
    {
        if (IsActive) _cooldownRemaining = Mathf.Max(0.01f, GameplayRules.Current.GhostCooldown);
        SetActive(false);
        _briefRemaining = 0f;
        _brief = false;
    }
    public override void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
        if (!IsActive) return;
        if (_brief)
        {
            _briefRemaining -= deltaTime;
            if (_briefRemaining <= 0f) End();
            return;
        }
        float cost = GetGhostPerSecondCost() * RunSession.GhostCostMultiplier * deltaTime;
        if (!_skillGauge.TryConsume(cost) || _skillGauge.Value <= Mathf.Epsilon) { End(); return; }
        PublishConsumed(cost);
    }
}
