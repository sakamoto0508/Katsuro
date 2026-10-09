using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPassiveBuffSet"
    , menuName = "ScriptableObjects/Player/PlayerPassiveBuffSet")]
/// <summary>Player装備の攻撃倍率・加算威力と命中時Effectをまとめる設定。</summary>
public class PlayerPassiveBuffSet : ScriptableObject
{
    /// <summary>装備による攻撃倍率・固定加算威力・任意の命中Effectの一覧。</summary>
    public IReadOnlyList<PassiveBuffEntry> Buffs => _buffs;
    /// <summary>装備による攻撃倍率・固定加算威力・任意の命中Effectの一覧。</summary>
    [UnityEngine.Tooltip("装備による攻撃倍率・固定加算威力・任意の命中Effectの一覧。")]
    [SerializeField] private List<PassiveBuffEntry> _buffs = new();

    /// <summary>登録済みパッシブを積算した乗算ダメージ係数。</summary>
    /// <returns>登録装備の攻撃倍率を掛け合わせた値。未登録なら1。</returns>
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
    /// <returns>登録装備の固定攻撃力を合計した加算値。</returns>
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

/// <summary>装備の表示名、攻撃倍率・加算威力と任意の命中Effectを組として保持する。</summary>
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

    /// <summary>Inspector上で装備ボーナスを識別する表示用ラベル。</summary>
    [UnityEngine.Tooltip("Inspector上で装備ボーナスを識別する表示用ラベル。")]
    [SerializeField] private string _label = "PassiveBuff";
    /// <summary>この装備の与ダメージ倍率。1で補正なし。他の装備倍率と乗算する。</summary>
    [UnityEngine.Tooltip("この装備の与ダメージ倍率。1で補正なし。他の装備倍率と乗算する。")]
    [SerializeField, Min(0f)] private float _attackPowerMultiplier = 1f;
    /// <summary>この装備が加える固定攻撃力。他の装備の加算値と合算する。</summary>
    [UnityEngine.Tooltip("この装備が加える固定攻撃力。他の装備の加算値と合算する。")]
    [SerializeField] private float _flatAttackBonus;
    /// <summary>この装備の命中時に使うEffect Prefab参照。未設定も許容する。</summary>
    [UnityEngine.Tooltip("この装備の命中時に使うEffect Prefab参照。未設定も許容する。")]
    [SerializeField] private GameObject _onHitEffectPrefab;
}
