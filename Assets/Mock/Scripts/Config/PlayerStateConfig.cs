using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStateConfig", menuName = "ScriptableObjects/Player/PlayerStateConfig")]
public class PlayerStateConfig : ScriptableObject
{
    public float MaxSkillGauge => _maxSkillGauge;
    public float DashGaugeCostPerSecond => _dashGaugeCostPerSecond;
    public float SkillGaugeRecoveryPerSecond => _skillGaugeRecoveryPerSecond;
    public float LightAttackDuration => _lightAttackDuration;
    public float StrongAttackDuration => _strongAttackDuration;
    public float JustAvoidAttackDuration => _justAvoidAttackDuration;

    [SerializeField, Min(1f)] private float _maxSkillGauge = 100f;

    [Header("Dash")]
    [SerializeField, Min(0.01f)] private float _dashGaugeCostPerSecond = 25f;
    [SerializeField, Min(0f)] private float _skillGaugeRecoveryPerSecond = 10f;

    [Header("Attack Durations")]
    [SerializeField, Min(0.1f)] private float _lightAttackDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float _strongAttackDuration = 1.0f;
    [SerializeField, Min(0.1f)] private float _justAvoidAttackDuration = 0.9f;
}
