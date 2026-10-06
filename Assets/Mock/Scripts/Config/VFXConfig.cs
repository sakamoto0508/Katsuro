using UnityEngine;

[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    [Header("ジャスト回避")]
    [Tooltip("薄い円形衝撃波のParticleSystem Prefab。未設定なら衝撃波のみ省略。自動破棄スクリプトは付けない。")]
    [SerializeField] private GameObject _justAvoidShockwavePrefab;
    [SerializeField, Min(.01f)] private float _shockwaveDuration = .25f;
    [SerializeField] private bool _shockwaveEnable;
    [SerializeField, Range(0f, 1f)] private float _shockwaveStrength = .2f;
    [SerializeField, Min(.01f)] private float _shockwaveSize = .5f;
    [Header("命中")]
    [Tooltip("追加のParticleSystem Prefab。未設定でも小さい接触フラッシュは表示する。")]
    [SerializeField] private GameObject _hitVFX;
    [SerializeField, Min(.01f)] private float _hitVFXDuration = .32f;
    [Tooltip("強攻撃の粒子数倍率。PrefabのBurst数を基準に毎回適用する。")]
    [SerializeField, Range(1f, 1.6f)] private float _heavyHitVFXAmount = 1.45f;
    [SerializeField, Range(1f, 1.3f)] private float _heavyHitVFXSpread = 1.15f;
    [SerializeField, Range(1f, 1.2f)] private float _heavyHitVFXSize = 1.1f;
    [SerializeField, Range(1f, 1.4f)] private float _heavyHitVFXScale = 1.3f;
    [SerializeField, Range(1f, 1.3f)] private float _heavyHitVFXSpeed = 1.2f;
    public float HeavyHitVFXScale => _heavyHitVFXScale;
    public float HeavyHitVFXSpeed => _heavyHitVFXSpeed;
    public float HeavyHitVFXAmount => _heavyHitVFXAmount;
    public float HeavyHitVFXSpread => _heavyHitVFXSpread;
    public float HeavyHitVFXSize => _heavyHitVFXSize;
    public GameObject HitVFX => _hitVFX;
    public float HitVFXDuration => _hitVFXDuration;
    public bool ShockwaveEnable => _shockwaveEnable;
    public float ShockwaveStrength => _shockwaveStrength;
    public float ShockwaveSize => _shockwaveSize;
    public GameObject JustAvoidShockwavePrefab => _justAvoidShockwavePrefab;
    public float ShockwaveDuration => _shockwaveDuration;
    public string PlayEffectHeal => _playEffectHeal;
    public string PlayEffectGhost => _playEffectGhost;
    public string PlayEffectBuff => _playEffectBuff;
    [SerializeField] private string _playEffectHeal;
    [SerializeField] private string _playEffectGhost;
    [SerializeField] private string _playEffectBuff;
}
