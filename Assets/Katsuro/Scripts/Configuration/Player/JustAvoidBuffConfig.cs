using UnityEngine;

/// <summary>ジャスト回避成功で蓄積する攻撃倍率の加算量と最大スタック数を定義する。</summary>
[CreateAssetMenu(fileName = "JustAvoidBuffConfig", menuName = "ScriptableObjects/Player/JustAvoidBuffConfig")]
public sealed class JustAvoidBuffConfig : ScriptableObject
{
    /// <summary>1スタックあたりの攻撃倍率加算（例: 0.05 = +5% / stack）</summary>
    [Tooltip("1スタックあたりの攻撃倍率加算（例: 0.05 = +5% / stack）")]
    [SerializeField, Min(0f)] private float _damageMultiplierPerStack = 0.05f;

    /// <summary>スタックの最大数（これを超える分は無視）</summary>
    [Tooltip("スタックの最大数（これを超える分は無視）")]
    [SerializeField, Min(0)] private int _maxStacks = 5;

    /// <summary>1スタックあたりの攻撃倍率加算（例: 0.05 = +5% / stack）</summary>
    public float DamageMultiplierPerStack => _damageMultiplierPerStack;
    /// <summary>スタックの最大数（これを超える分は無視）</summary>
    public int MaxStacks => _maxStacks;
}
