using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPassiveBuffSet"
    , menuName = "ScriptableObjects/Player/PlayerPassiveBuffSet")]
public class PlayerPassiveBuffSet : ScriptableObject
{
    public IReadOnlyList<PassiveBuffEntry> Buffs => _buffs;
    [SerializeField] private List<PassiveBuffEntry> _buffs = new();

    /// <summary>登録済みパッシブを積算した乗算ダメージ係数。</summary>
    public float EvaluateDamageMultiplier()
    {
        if (_buffs == null || _buffs.Count == 0)
            return 1f;

        float multiplier = 1f;
        foreach (var entry in _buffs)
        {
            if (entry == null) continue;
            multiplier *= Mathf.Max(0f, entry.AttackPowerMultiplier);
        }
        return multiplier;
    }

    /// <summary>登録済みパッシブを合算した加算ダメージ値。</summary>
    public float EvaluateFlatDamageBonus()
    {
        if (_buffs == null || _buffs.Count == 0)
            return 0f;

        float bonus = 0f;
        foreach (var entry in _buffs)
        {
            if (entry == null) continue;
            bonus += entry.FlatAttackBonus;
        }
        return bonus;
    }
}

[Serializable]
public sealed class PassiveBuffEntry
{
    /// <summary>インスペクター表示用のラベル。</summary>
    public string Label => _label;

    /// <summary>装備がもたらす攻撃力の乗算倍率。</summary>
    public float AttackPowerMultiplier => _attackPowerMultiplier;

    /// <summary>装備がもたらす攻撃力の加算値。</summary>
    public float FlatAttackBonus => _flatAttackBonus;

    /// <summary>ヒット時に再生するエフェクトのプレハブ（任意）。</summary>
    public GameObject OnHitEffectPrefab => _onHitEffectPrefab;

    [SerializeField] private string _label = "PassiveBuff";
    [SerializeField, Min(0f)] private float _attackPowerMultiplier = 1f;
    [SerializeField] private float _flatAttackBonus;
    [SerializeField] private GameObject _onHitEffectPrefab;
}