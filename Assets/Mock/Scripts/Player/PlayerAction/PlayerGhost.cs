using UnityEngine;

/// <summary>ボタンを押している間は幽体化する。開始時のゲージ不足では、延長できない短時間の幽体化を行う。</summary>
public sealed class PlayerGhost : AbilityBase
{
    /// <summary>幽体化に使用するゲージと開始・継続コスト設定を基底能力へ渡す。</summary>
    public PlayerGhost(SkillGauge gauge, SkillGaugeCostConfig costConfig = null) : base(gauge, costConfig) { }
    private float _briefRemaining;
    private float _cooldownRemaining;
    private bool _brief;
    public bool IsGhosting => IsActive;
    public bool IsBrief => _brief;
    /// <summary>再使用待ちを確認し、開始コストが足りなければ短時間の幽体化として開始する。</summary>
    /// <returns>幽体化を新しく開始できた場合はtrue。</returns>
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
    /// <summary>幽体化を終了し、有効だった場合は再使用待ちを設定して短時間モードを解除する。</summary>
    public override void End()
    {
        if (IsActive) _cooldownRemaining = Mathf.Max(0.01f, GameplayRules.Current.GhostCooldown);
        SetActive(false);
        _briefRemaining = 0f;
        _brief = false;
    }
    /// <summary>再使用待ちと短時間モードを進め、通常モードでは継続コスト不足で幽体化を終了する。</summary>
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
