using UnityEngine;

[CreateAssetMenu(menuName = "Katsuro/Gameplay Rules")]
public sealed class GameplayRules : ScriptableObject
{
    [Min(1)] public int StartingLives = 2;
    [Min(0)] public float ReviveInvulnerability = 2f;
    [Min(0.01f)] public float ShortGhostDuration = 0.08f;
    [Min(0.01f)] public float GhostCooldown = 0.35f;
    [Min(0)] public float JustBuffDuration = 8f;
    public float FullHpRegenMultiplier = 1.5f;
    public float LowHpRegenMultiplier = 0.5f;
    public float FullHpSelfCostMultiplier = 0.5f;
    public float LowHpSelfCostMultiplier = 1f;
    public float FullHpAvoidMultiplier = 1.5f;
    public float LowHpAvoidMultiplier = 0.75f;
    [Header("Equipment multipliers")]
    [Min(0)] public float PowerAttack = 1.2f;
    [Min(0)] public float SoulHitGain = 2f;
    [Min(0)] public float DesperationBonus = 0.35f;
    [Range(0, 1)] public float GuardDamage = 0.8f;
    [Range(0, 1)] public float SpiritCost = 0.7f;
    [Min(0)] public float BreathRegen = 1.3f;
    [Header("Inherited boss equivalents")]
    [Min(0)] public float SoulBossDamage = 1.1f;
    [Min(0)] public float SpiritBossSpeed = 1.1f;
    [Min(0)] public float BreathBossHealth = 1.2f;
    private static GameplayRules _instance;
    public static GameplayRules Current
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameplayRules>("KatsuroRules");
            if (_instance == null) _instance = CreateInstance<GameplayRules>();
            return _instance;
        }
    }
    public float Regen(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpRegenMultiplier, FullHpRegenMultiplier, Mathf.Clamp01(hp)));
    public float SelfCost(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpSelfCostMultiplier, FullHpSelfCostMultiplier, Mathf.Clamp01(hp)));
    public float AvoidWindow(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpAvoidMultiplier, FullHpAvoidMultiplier, Mathf.Clamp01(hp)));
}
