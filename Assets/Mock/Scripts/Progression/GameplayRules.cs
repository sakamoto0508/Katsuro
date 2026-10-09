using UnityEngine;

/// <summary>装備倍率、残機、HP依存の能力倍率などラン全体の調整値を提供する設定。</summary>
[CreateAssetMenu(menuName = "Katsuro/Gameplay Rules")]
public sealed class GameplayRules : ScriptableObject
{
    /// <summary>新しいRunを開始したときの残機数。</summary>
    [UnityEngine.Tooltip("新しいRunを開始したときの残機数。")]
    [Min(1)] public int StartingLives = 2;
    /// <summary>復活後にダメージを無効化する時間（秒）。</summary>
    [UnityEngine.Tooltip("復活後にダメージを無効化する時間（秒）。")]
    [Min(0)] public float ReviveInvulnerability = 2f;
    /// <summary>短い幽体化回避を維持する時間（秒）。</summary>
    [UnityEngine.Tooltip("短い幽体化回避を維持する時間（秒）。")]
    [Min(0.01f)] public float ShortGhostDuration = 0.08f;
    /// <summary>幽体化回避を再使用できるまでの時間（秒）。</summary>
    [UnityEngine.Tooltip("幽体化回避を再使用できるまでの時間（秒）。")]
    [Min(0.01f)] public float GhostCooldown = 0.35f;
    /// <summary>Just Avoid成功で得る攻撃ボーナスの有効時間（秒）。</summary>
    [UnityEngine.Tooltip("Just Avoid成功で得る攻撃ボーナスの有効時間（秒）。")]
    [Min(0)] public float JustBuffDuration = 8f;
    /// <summary>満HP時の自然ゲージ回復倍率。低HP時の倍率からHP比率で補間する。</summary>
    [UnityEngine.Tooltip("満HP時の自然ゲージ回復倍率。低HP時の倍率からHP比率で補間する。")]
    public float FullHpRegenMultiplier = 1.5f;
    /// <summary>HP比率が0のときの自然ゲージ回復倍率。満HP時の倍率まで補間する。</summary>
    [UnityEngine.Tooltip("HP比率が0のときの自然ゲージ回復倍率。満HP時の倍率まで補間する。")]
    public float LowHpRegenMultiplier = 0.5f;
    /// <summary>満HP時の自傷ゲージ消費倍率。低HP時の倍率から補間する。</summary>
    [UnityEngine.Tooltip("満HP時の自傷ゲージ消費倍率。低HP時の倍率から補間する。")]
    public float FullHpSelfCostMultiplier = 0.5f;
    /// <summary>HP比率が0のときの自傷ゲージ消費倍率。満HP時の倍率まで補間する。</summary>
    [UnityEngine.Tooltip("HP比率が0のときの自傷ゲージ消費倍率。満HP時の倍率まで補間する。")]
    public float LowHpSelfCostMultiplier = 1f;
    /// <summary>満HP時のJust Avoid受付時間倍率。低HP時の倍率から補間する。</summary>
    [UnityEngine.Tooltip("満HP時のJust Avoid受付時間倍率。低HP時の倍率から補間する。")]
    public float FullHpAvoidMultiplier = 1.5f;
    /// <summary>HP比率が0のときのJust Avoid受付時間倍率。満HP時の倍率まで補間する。</summary>
    [UnityEngine.Tooltip("HP比率が0のときのJust Avoid受付時間倍率。満HP時の倍率まで補間する。")]
    public float LowHpAvoidMultiplier = 0.75f;
    /// <summary>攻撃装備「剛力」の与ダメージ倍率。1で補正なし。</summary>
    [UnityEngine.Tooltip("攻撃装備「剛力」の与ダメージ倍率。1で補正なし。")]
    [Header("Equipment multipliers")]
    [Min(0)] public float PowerAttack = 1.2f;
    /// <summary>攻撃装備「吸魂」の命中時ゲージ回復倍率。1で補正なし。</summary>
    [UnityEngine.Tooltip("攻撃装備「吸魂」の命中時ゲージ回復倍率。1で補正なし。")]
    [Min(0)] public float SoulHitGain = 2f;
    /// <summary>攻撃装備「窮地」の最大追加ダメージ倍率。HPが低いほど増え、0.35で最大35%増。</summary>
    [UnityEngine.Tooltip("攻撃装備「窮地」の最大追加ダメージ倍率。HPが低いほど増え、0.35で最大35%増。")]
    [Min(0)] public float DesperationBonus = 0.35f;
    /// <summary>防御装備「堅守」の被ダメージ倍率。0.8で通常の80%。</summary>
    [UnityEngine.Tooltip("防御装備「堅守」の被ダメージ倍率。0.8で通常の80%。")]
    [Range(0, 1)] public float GuardDamage = 0.8f;
    /// <summary>防御装備「霊衣」の幽体化開始・継続ゲージ消費倍率。</summary>
    [UnityEngine.Tooltip("防御装備「霊衣」の幽体化開始・継続ゲージ消費倍率。")]
    [Range(0, 1)] public float SpiritCost = 0.7f;
    /// <summary>防御装備「息吹」の自然ゲージ回復倍率。</summary>
    [UnityEngine.Tooltip("防御装備「息吹」の自然ゲージ回復倍率。")]
    [Min(0)] public float BreathRegen = 1.3f;
    /// <summary>前回勝者が「吸魂」の場合に継承Enemyへ適用する攻撃倍率。</summary>
    [UnityEngine.Tooltip("前回勝者が「吸魂」の場合に継承Enemyへ適用する攻撃倍率。")]
    [Header("Inherited boss equivalents")]
    [Min(0)] public float SoulBossDamage = 1.1f;
    /// <summary>前回勝者が「霊衣」の場合に継承Enemyへ適用する移動速度倍率。</summary>
    [UnityEngine.Tooltip("前回勝者が「霊衣」の場合に継承Enemyへ適用する移動速度倍率。")]
    [Min(0)] public float SpiritBossSpeed = 1.1f;
    /// <summary>前回勝者が「息吹」の場合に継承Enemyへ適用する最大HP倍率。</summary>
    [UnityEngine.Tooltip("前回勝者が「息吹」の場合に継承Enemyへ適用する最大HP倍率。")]
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
    /// <summary>HP割合に応じて低HP時と満HP時のゲージ回復倍率を補間する。</summary>
    /// <returns>0以上のゲージ自然回復倍率。</returns>
    /// <param name="hp">現在HPの最大HPに対する0から1の割合。</param>
    public float Regen(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpRegenMultiplier, FullHpRegenMultiplier, Mathf.Clamp01(hp)));
    /// <summary>HP割合に応じて低HP時と満HP時の自傷消費倍率を補間する。</summary>
    /// <returns>0以上の自傷コスト倍率。</returns>
    /// <param name="hp">現在HPの最大HPに対する0から1の割合。</param>
    public float SelfCost(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpSelfCostMultiplier, FullHpSelfCostMultiplier, Mathf.Clamp01(hp)));
    /// <summary>HP割合に応じてジャスト回避受付時間の倍率を補間する。</summary>
    /// <returns>0以上の回避受付倍率。</returns>
    /// <param name="hp">現在HPの最大HPに対する0から1の割合。</param>
    public float AvoidWindow(float hp) => Mathf.Max(0f, Mathf.Lerp(LowHpAvoidMultiplier, FullHpAvoidMultiplier, Mathf.Clamp01(hp)));
}
