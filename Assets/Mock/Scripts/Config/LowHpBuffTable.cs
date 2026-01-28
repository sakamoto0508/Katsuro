using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 低HP時のバフ定義を ScriptableObject として管理するテーブル。
/// - HP 割合（0..1）に対応する倍率やゲージ回復ボーナスを定義する。
/// </summary>
[CreateAssetMenu(fileName = "LowHpBuffTable", menuName = "ScriptableObjects/Player/LowHpBuffTable")]
public sealed class LowHpBuffTable : ScriptableObject
{
    [Serializable]
    public sealed class Tier
    {
        [Tooltip("この割合以下のときに該当する (0..1)。例: 0.25 = 25%")]
        [Range(0f, 1f)] public float HpBelowRatio = 1f;

        [Tooltip("ダメージに掛ける乗算倍率（例: 1.10 = +10%）")]
        public float DamageMultiplier = 1f;

        [Tooltip("スキルゲージ回復ボーナス（割合加算、例: 0.05 = +5%）")]
        public float SkillGaugeRegenBonus = 0f;

        [Tooltip("表示用ラベル（任意）。")]
        public string Label;
    }

    [SerializeField] private List<Tier> _tiers = new();

    /// <summary>登録済みティア（Inspector 順序をそのまま保持）。</summary>
    public IReadOnlyList<Tier> Tiers => _tiers;

    /// <summary>
    /// 指定の currentHpRatio（0..1）に対する適用ティアを返します。
    /// 最初にマッチした Tier を返す（Inspector 上で小さい方から大きい方へ並べるか、上から評価する運用にして下さい）。
    /// マッチなしの場合は null を返します。
    /// </summary>
    public Tier GetTier(float currentHpRatio)
    {
        if (_tiers == null || _tiers.Count == 0) return null;
        foreach (var tier in _tiers)
        {
            if (currentHpRatio <= tier.HpBelowRatio)
                return tier;
        }
        return null;
    }

    /// <summary>
    /// 指定の currentHpRatio に応じたダメージ乗算係数を返します（デフォルト 1.0）。
    /// </summary>
    public float EvaluateDamageMultiplier(float currentHpRatio)
    {
        var tier = GetTier(currentHpRatio);
        return tier != null ? Mathf.Max(0f, tier.DamageMultiplier) : 1f;
    }

    /// <summary>
    /// 指定の currentHpRatio に応じたスキルゲージ回復ボーナス（加算割合）を返します（デフォルト 0）。
    /// 返値は 0..1 の割合（例: 0.05 = +5% 回復量）です。
    /// </summary>
    public float EvaluateSkillGaugeRegenBonus(float currentHpRatio)
    {
        var tier = GetTier(currentHpRatio);
        return tier != null ? tier.SkillGaugeRegenBonus : 0f;
    }
}
