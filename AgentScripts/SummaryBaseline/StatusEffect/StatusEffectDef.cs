using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectDef", menuName = "ScriptableObjects/StatusEffect/StatusEffectDef")]
public class StatusEffectDef : ScriptableObject
{
    /// <summary>
    /// 効果の一意識別子
    /// </summary>
    public string Id => _id;
    /// <summary>
    /// 効果の持続時間（秒）
    /// </summary>
    public float Duration => _duration;
    /// <summary>
    /// 速度倍率（移動速度に乗算される）
    /// </summary>
    public float SpeedMultiplier => _speedMultiplier;
    /// <summary>
    /// アニメーション速度倍率（Animator.speed に乗算される）
    /// </summary>
    public float AnimationSpeedMultiplier => _animationSpeedMultiplier;
    /// <summary>
    /// 効果の最大スタック数
    /// </summary>
    public int MaxStacks => _maxStacks;
    /// <summary>
    /// スタックポリシー（Refresh / Replace / Stack）
    /// </summary>
    public StackPolicy Stacking => _stacking;
    /// <summary>
    /// 効果の表示用 VFX プレハブ
    /// </summary>
    public GameObject VfxPrefab => _vfxPrefab;

    public enum StackPolicy
    {
        Refresh,
        Replace,
        Stack
    }

    [SerializeField] private string _id = "Slow";
    [SerializeField] private float _duration = 2f;
    [SerializeField, Range(0f, 1f)] private float _speedMultiplier = 0.5f;
    [SerializeField, Range(0f, 2f)] private float _animationSpeedMultiplier = 0.5f;
    [SerializeField, Min(1)] private int _maxStacks = 1;
    [SerializeField] private StackPolicy _stacking = StackPolicy.Refresh;
    [SerializeField] private GameObject _vfxPrefab;
}
