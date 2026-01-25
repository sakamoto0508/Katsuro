using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPassiveBuffSet"
    , menuName = "ScriptableObjects/Player/PlayerPassiveBuffSet")]
public class PlayerPassiveBuffSet : ScriptableObject
{
    public IReadOnlyList<PassiveBuffEntry> Buffs => _buffs; 
    [SerializeField] private List<PassiveBuffEntry> _buffs = new();
}

[Serializable]
public sealed class PassiveBuffEntry
{
    public string Label => _label;
    public float AttackPowerMultiplier => _attackPowerMultiplier;
    public float FlatAttackBonus => _flatAttackBonus;
    public GameObject OnHitEffectPrefab => _onHitEffectPrefab;
    [SerializeField] private string _label = "PassiveBuff";
    [SerializeField, Min(0f)] private float _attackPowerMultiplier = 1f;
    [SerializeField] private float _flatAttackBonus;
    [SerializeField] private GameObject _onHitEffectPrefab;

}