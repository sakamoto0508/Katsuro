using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStateConfig", menuName = "ScriptableObjects/Player/PlayerStateConfig")]
public class PlayerStateConfig : ScriptableObject
{
    public float MaxSkillGauge => Mathf.Max(1f, _maxSkillGauge);
    public float DashGaugeCostPerSecond => Mathf.Max(0.01f, _dashGaugeCostPerSecond);
    public float SkillGaugeRecoveryPerSecond => Mathf.Max(0f, _skillGaugeRecoveryPerSecond);

    [SerializeField] private float _maxSkillGauge = 100f;

    [Header("Dash")]
    [SerializeField] private float _dashGaugeCostPerSecond = 25f;
    [SerializeField] private float _skillGaugeRecoveryPerSecond = 10f;
}
