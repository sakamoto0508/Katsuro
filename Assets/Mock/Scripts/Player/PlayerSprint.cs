using UnityEngine;

public class PlayerSprint
{
    private readonly float _maxSkillGauge;
    private readonly float _dashGaugeCostPerSecond;
    private readonly float _skillGaugeRecoveryPerSecond;

    private float _currentSkillGauge;
    private bool _isConsuming;

    public PlayerSprint(float maxSkillGauge, float dashGaugeCostPerSecond, float skillGaugeRecoveryPerSecond)
    {
        _maxSkillGauge = Mathf.Max(1f, maxSkillGauge);
        _dashGaugeCostPerSecond = Mathf.Max(0.01f, dashGaugeCostPerSecond);
        _skillGaugeRecoveryPerSecond = Mathf.Max(0f, skillGaugeRecoveryPerSecond);

        _currentSkillGauge = _maxSkillGauge;
    }

    public float SkillGauge => _currentSkillGauge;
    public float SkillGaugeNormalized => _currentSkillGauge / _maxSkillGauge;
    public bool CanDash => _currentSkillGauge > Mathf.Epsilon;
    public bool IsDashing => _isConsuming && _currentSkillGauge > Mathf.Epsilon;

    public void BeginDash()
    {
        if (CanDash)
        {
            _isConsuming = true;
        }
    }

    public void EndDash()
    {
        _isConsuming = false;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (_isConsuming)
        {
            _currentSkillGauge -= _dashGaugeCostPerSecond * deltaTime;
            if (_currentSkillGauge <= 0f)
            {
                _currentSkillGauge = 0f;
                _isConsuming = false;
            }
        }
        else if (_skillGaugeRecoveryPerSecond > 0f && _currentSkillGauge < _maxSkillGauge)
        {
            _currentSkillGauge = Mathf.Min(_maxSkillGauge,
                _currentSkillGauge + _skillGaugeRecoveryPerSecond * deltaTime);
        }
    }
}
