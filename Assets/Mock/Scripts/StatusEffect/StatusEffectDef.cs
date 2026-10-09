using UnityEngine;

/// <summary>状態効果のID、持続・重複方式とステータス倍率を定義する共有データ。</summary>
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

    /// <summary>同じ状態効果の再付与時に、更新・加算などどの重複処理を行うか識別する。</summary>
    public enum StackPolicy
    {
        Refresh,
        Replace,
        Stack
    }

    /// <summary>状態効果を識別するID。同じIDの追加時にStackPolicyで扱いを決める。</summary>
    [UnityEngine.Tooltip("状態効果を識別するID。同じIDの追加時にStackPolicyで扱いを決める。")]
    [SerializeField] private string _id = "Slow";
    /// <summary>状態効果の有効時間（秒）。経過後に解除する。</summary>
    [UnityEngine.Tooltip("状態効果の有効時間（秒）。経過後に解除する。")]
    [SerializeField] private float _duration = 2f;
    /// <summary>状態効果中の移動速度倍率。1で補正なし。</summary>
    [UnityEngine.Tooltip("状態効果中の移動速度倍率。1で補正なし。")]
    [SerializeField, Range(0f, 1f)] private float _speedMultiplier = 0.5f;
    /// <summary>状態効果中のAnimator速度倍率。HitStopの一時倍率とは別に合成する。</summary>
    [UnityEngine.Tooltip("状態効果中のAnimator速度倍率。HitStopの一時倍率とは別に合成する。")]
    [SerializeField, Range(0f, 2f)] private float _animationSpeedMultiplier = 0.5f;
    /// <summary>同じ状態効果を重ねられる最大スタック数。</summary>
    [UnityEngine.Tooltip("同じ状態効果を重ねられる最大スタック数。")]
    [SerializeField, Min(1)] private int _maxStacks = 1;
    /// <summary>同じIDの状態効果を追加した際の更新・加算・置換の方針。</summary>
    [UnityEngine.Tooltip("同じIDの状態効果を追加した際の更新・加算・置換の方針。")]
    [SerializeField] private StackPolicy _stacking = StackPolicy.Refresh;
    /// <summary>状態効果中に表示する任意のVFX Prefab。未設定ならVFXを生成しない。</summary>
    [UnityEngine.Tooltip("状態効果中に表示する任意のVFX Prefab。未設定ならVFXを生成しない。")]
    [SerializeField] private GameObject _vfxPrefab;
}
