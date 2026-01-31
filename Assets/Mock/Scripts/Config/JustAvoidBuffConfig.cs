using UnityEngine;

[CreateAssetMenu(fileName = "JustAvoidBuffConfig", menuName = "ScriptableObjects/Player/JustAvoidBuffConfig")]
public sealed class JustAvoidBuffConfig : ScriptableObject
{
    [Tooltip("1スタックあたりの攻撃倍率加算（例: 0.05 = +5% / stack）")]
    [SerializeField, Min(0f)] private float _damageMultiplierPerStack = 0.05f;

    [Tooltip("スタックの最大数（これを超える分は無視）")]
    [SerializeField, Min(0)] private int _maxStacks = 5;

    public float DamageMultiplierPerStack => _damageMultiplierPerStack;
    public int MaxStacks => _maxStacks;
}
