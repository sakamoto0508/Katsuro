using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttackClipList
{
    [SerializeField] private List<AnimationClip> _clips = new();
    [SerializeField] private List<float> _comboWindowDelaySeconds = new();

    public IReadOnlyList<AnimationClip> Clips => _clips;

    public AnimationClip GetClip(int index)
    {
        if (_clips == null || _clips.Count == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, _clips.Count - 1);
        return _clips[index];
    }

    public float GetDuration(int index, float fallbackSeconds)
    {
        var clip = GetClip(index);
        if (clip == null)
        {
            return Mathf.Max(0.1f, fallbackSeconds);
        }

        return Mathf.Max(0.1f, clip.length);
    }

    public float GetComboWindowDelay(int index, float fallbackSeconds)
    {
        if (_comboWindowDelaySeconds == null || _comboWindowDelaySeconds.Count == 0)
        {
            return Mathf.Max(0f, fallbackSeconds);
        }

        index = Mathf.Clamp(index, 0, _comboWindowDelaySeconds.Count - 1);
        float value = _comboWindowDelaySeconds[index];
        return value >= 0f ? value : Mathf.Max(0f, fallbackSeconds);
    }
}

[CreateAssetMenu(fileName = "PlayerStateConfig", menuName = "ScriptableObjects/Player/PlayerStateConfig")]
public class PlayerStateConfig : ScriptableObject
{
    public float MaxSkillGauge => _maxSkillGauge;
    public float DashGaugeCostPerSecond => _dashGaugeCostPerSecond;
    public float SkillGaugeRecoveryPerSecond => _skillGaugeRecoveryPerSecond;
    public float GhostCost => _ghostCost;
    public float GhostCostPerSecond => _ghostCostPerSecond;
    public float HealCost => _healCost;
    public float HealCostPerSecond => _healCostPerSecond;
    public float SelfSacrificeCost => _selfSacrificeCost;
    public float SelfSacrificeCostPerSecond => _selfSacrificeCostPerSecond;
    public float JustAvoidTime => _justAvoidTime;

    public IReadOnlyList<AnimationClip> LightAttackClips => _lightAttackClips.Clips;
    public IReadOnlyList<AnimationClip> LockOnLightAttackClips => _lockOnLightAttackClips.Clips;
    public IReadOnlyList<AnimationClip> StrongAttackClips => _strongAttackClips.Clips;
    public IReadOnlyList<AnimationClip> JustAvoidAttackClips => _justAvoidAttackClips.Clips;

    public float GetLightAttackDuration(int comboIndex = 0) => GetLightAttackDuration(false, comboIndex);

    public float GetLightAttackDuration(bool isLockOn, int comboIndex = 0)
        => SelectLightAttackList(isLockOn).GetDuration(comboIndex, 0.8f);

    public float GetLightAttackComboWindowDelay(bool isLockOn, int comboIndex = 0)
        => SelectLightAttackList(isLockOn).GetComboWindowDelay(comboIndex, _defaultLightComboWindowDelay);

    public int GetLightAttackComboCount(bool isLockOn)
    {
        var clips = SelectLightAttackList(isLockOn).Clips;
        return Mathf.Max(1, clips?.Count ?? 0);
    }

    public IReadOnlyList<AnimationClip> GetLightAttackClips(bool isLockOn)
        => SelectLightAttackList(isLockOn).Clips;

    public float GetStrongAttackDuration(int comboIndex = 0) => _strongAttackClips.GetDuration(comboIndex, 1.0f);

    public float GetStrongAttackComboWindowDelay(int comboIndex = 0)
        => _strongAttackClips.GetComboWindowDelay(comboIndex, _defaultStrongComboWindowDelay);

    public float GetJustAvoidAttackDuration(int comboIndex = 0) => _justAvoidAttackClips.GetDuration(comboIndex, 0.9f);

    [SerializeField, Min(1f)] private float _maxSkillGauge = 100f;

    [Header("Dash")]
    [SerializeField, Min(0.01f)] private float _dashGaugeCostPerSecond = 25f;
    [SerializeField, Min(0f)] private float _skillGaugeRecoveryPerSecond = 10f;

    [Header("Ghost")]
    [SerializeField, Min(0.1f)] private float _ghostCost = 10f;
    [SerializeField, Min(0f)] private float _ghostCostPerSecond = 5f;

    [Header("Heal")]
    [SerializeField, Min(0.1f)] private float _healCost = 10f;
    [SerializeField, Min(0f)] private float _healCostPerSecond = 5f;

    [Header("SelfSacrifice")]
    [SerializeField, Min(0.1f)] private float _selfSacrificeCost = 10f;
    [SerializeField, Min(0f)] private float _selfSacrificeCostPerSecond = 5f;

    [Header("Avoid")]
    [SerializeField, Min(0.01f)] private float _justAvoidTime = 0.2f;

    [Header("Combo Window Timing")]
    [SerializeField, Min(0f)] private float _defaultLightComboWindowDelay = 0.05f;
    [SerializeField, Min(0f)] private float _defaultStrongComboWindowDelay = 0.1f;

    [Header("Attack Clips")]
    [SerializeField] private AttackClipList _lightAttackClips = new();
    [SerializeField] private AttackClipList _lockOnLightAttackClips = new();
    [SerializeField] private AttackClipList _strongAttackClips = new();
    [SerializeField] private AttackClipList _justAvoidAttackClips = new();

    private AttackClipList SelectLightAttackList(bool isLockOn)
    {
        bool hasLockOnVariant = _lockOnLightAttackClips != null
            && _lockOnLightAttackClips.Clips != null
            && _lockOnLightAttackClips.Clips.Count > 0;

        if (isLockOn && hasLockOnVariant)
        {
            return _lockOnLightAttackClips;
        }

        return _lightAttackClips;
    }
}
