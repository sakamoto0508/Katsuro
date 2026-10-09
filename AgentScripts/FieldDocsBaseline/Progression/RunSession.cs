using System;
using UnityEngine;

/// <summary>勝利した挑戦者の名前・装備選択・所要時間を保存し、次回の対戦相手へ引き継ぐデータ。</summary>
[Serializable]
public sealed class ChampionTrace
{
    public int Version = 1;
    public string Name;
    public int Attack;
    public int Defense;
    public float ClearSeconds;
}

/// <summary>今回の挑戦を管理し、勝利した場合のみ保存済みの勝者を更新する。</summary>
public static class RunSession
{
    private const string SaveKey = "Katsuro.Champion.v1";
    public static bool Active { get; private set; }
    public static string PlayerName { get; private set; } = "挑戦者";
    public static int Attack { get; private set; }
    public static int Defense { get; private set; }
    public static int Lives { get; private set; }
    /// <summary>
    /// 前回の勝者の痕跡。null の場合はまだ誰も勝利していない。
    /// </summary>
    public static ChampionTrace Opponent { get; private set; }
    /// <summary>
    /// 今回の挑戦が開始された時刻（Time.unscaledTime）。勝利時のクリア時間計算に使う。
    /// </summary>
    public static float StartedAt { get; private set; }
    public static string Result { get; private set; }
    public static string SaveError { get; private set; }
    public static readonly string[] AttackNames = { "剛力", "吸魂", "窮地" };
    public static readonly string[] DefenseNames = { "堅守", "霊衣", "息吹" };
    /// <summary>選択した攻撃装備の倍率や低HP効果を現在の調整値から説明文へ変換する。</summary>
    /// <returns>攻撃装備の日本語説明。</returns>
    public static string AttackDescription(int id) => id == 0 ? $"与ダメージ ×{GameplayRules.Current.PowerAttack:0.##}" : id == 1 ? $"命中時のゲージ回復 ×{GameplayRules.Current.SoulHitGain:0.##}" : $"低HPほど追加威力（最大 +{GameplayRules.Current.DesperationBonus * 100:0}%）";
    /// <summary>選択した防御装備の被ダメージ・幽体化コスト・回復効果を説明文へ変換する。</summary>
    /// <returns>防御装備の日本語説明。</returns>
    public static string DefenseDescription(int id) => id == 0 ? $"被ダメージ ×{GameplayRules.Current.GuardDamage:0.##}" : id == 1 ? $"幽霊化の開始・継続消費 ×{GameplayRules.Current.SpiritCost:0.##}" : $"ゲージ自然回復 ×{GameplayRules.Current.BreathRegen:0.##}";

    /// <summary>新しい実行セッション開始時にランの状態・前回相手・結果・保存エラーを消去する。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Active = false; Opponent = null; Result = null; SaveError = null; }
    /// <summary>名前から制御文字とタグ記号を除き、長さを制限して空の場合は既定名を使用する。</summary>
    /// <returns>表示と保存に使用する整形済みの名前。</returns>
    public static string CleanName(string name)
    {
        name = (name ?? "").Trim();
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\p{C}<>]", "");
        if (name.Length > 16) name = name.Substring(0, 16);
        return string.IsNullOrWhiteSpace(name) ? "挑戦者" : name;
    }
    /// <summary>PlayerPrefsの勝者データを読み込み、欠損や不正な保存値の場合は相手なしとして扱う。</summary>
    /// <returns>検証済みの勝者データ。保存がない・不正な場合はnull。</returns>
    public static ChampionTrace ReadChampion()
    {
        try
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            var trace = JsonUtility.FromJson<ChampionTrace>(json);
            if (trace == null || trace.Version != 1 || trace.Attack < 0 || trace.Attack > 2 || trace.Defense < 0 || trace.Defense > 2) return null;
            trace.Name = CleanName(trace.Name);
            return trace;
        }
        catch (Exception) { return null; }
    }
    /// <summary>名前と装備選択を検証して残機・相手・開始時刻を初期化し、新しいランを有効にする。</summary>
    public static void Begin(string name, int attack, int defense)
    {
        PlayerName = CleanName(name);
        Attack = Mathf.Clamp(attack, 0, 2);
        Defense = Mathf.Clamp(defense, 0, 2);
        Lives = Mathf.Max(1, GameplayRules.Current.StartingLives);
        Opponent = ReadChampion();
        StartedAt = Time.unscaledTime;
        Result = null; SaveError = null; Active = true;
    }
    /// <summary>ラン未開始の場合だけ、保持している名前と装備で開始する。</summary>
    public static void EnsureRun() { if (!Active) Begin(PlayerName, Attack, Defense); }
    /// <summary>有効なランの残機を一つ減らし、復活可能かを返す。</summary>
    /// <returns>消費後に残機があり復活可能ならtrue。</returns>
    public static bool ConsumeLife()
    {
        if (!Active) return false;
        Lives = Mathf.Max(0, Lives - 1);
        return Lives > 0;
    }
    /// <summary>有効なランを一度だけ完了し、勝利時は名前・装備・実所要時間を次回相手として保存する。</summary>
    public static void Complete(bool victory)
    {
        if (!Active) return;
        Active = false;
        Result = victory ? "討伐成功 — あなたの名前と装備が次の敵に継承されます" : "敗北 — 前回の勝者の痕跡は残ります";
        if (!victory) return;
        var champion = new ChampionTrace { Name = PlayerName, Attack = Attack, Defense = Defense, ClearSeconds = Mathf.Max(0, Time.unscaledTime - StartedAt) };
        try { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(champion)); PlayerPrefs.Save(); }
        catch (Exception error) { SaveError = "痕跡を保存できませんでした"; Result = "討伐成功（痕跡の保存に失敗）"; Debug.LogException(error); }
    }
    /// <summary>攻撃装備と現在HP割合からPlayerの与ダメージ倍率を求める。</summary>
    /// <returns>現在の装備・HPに対応する与ダメージ倍率。</returns>
    public static float DamageMultiplier(float hp) => Attack == 0 ? GameplayRules.Current.PowerAttack : Attack == 2 ? 1f + (1f - Mathf.Clamp01(hp)) * GameplayRules.Current.DesperationBonus : 1f;
    public static float IncomingMultiplier => Defense == 0 ? GameplayRules.Current.GuardDamage : 1f;
    public static float GhostCostMultiplier => Defense == 1 ? GameplayRules.Current.SpiritCost : 1f;
    public static float RegenMultiplier => Defense == 2 ? GameplayRules.Current.BreathRegen : 1f;
    public static float HitGainMultiplier => Attack == 1 ? GameplayRules.Current.SoulHitGain : 1f;
    /// <summary>前回勝者の攻撃装備とEnemyのHP割合から継承された攻撃倍率を求める。</summary>
    /// <returns>Enemyの与ダメージ倍率。継承相手がなければ1。</returns>
    public static float EnemyDamage(float hp)
    {
        if (Opponent == null) return 1f;
        return Opponent.Attack == 0 ? GameplayRules.Current.PowerAttack : Opponent.Attack == 1 ? GameplayRules.Current.SoulBossDamage : 1f + (1f - Mathf.Clamp01(hp)) * GameplayRules.Current.DesperationBonus;
    }
    public static float EnemyDefense => Opponent != null && Opponent.Defense == 0 ? GameplayRules.Current.GuardDamage : 1f;
    public static float EnemyHealth => Opponent != null && Opponent.Defense == 2 ? GameplayRules.Current.BreathBossHealth : 1f;
    public static float EnemyMoveSpeed => Opponent != null && Opponent.Defense == 1 ? GameplayRules.Current.SpiritBossSpeed : 1f;
}
