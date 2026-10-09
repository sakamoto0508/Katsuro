using UnityEngine;

/// <summary>命中、追撃、ジャスト回避のPrefabと表示量を定義する。命中判定やダメージ計算は担当しない。</summary>
[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    [Header("Just Avoid / Screen Distortion")]
    [SerializeField] private bool _screenDistortionEnable = true;
    [SerializeField, Range(.1f, .18f)] private float _distortionDuration = .14f;
    [Tooltip("画面高さを1とした最終半径。画面の隅まで届く半径は自動で保証する。")]
    [SerializeField, Min(.1f)] private float _distortionMaxRadius = 1.15f;
    [Tooltip("拡大速度の曲線。大きいほど最初に速く広がる。")]
    [SerializeField, Range(1f, 3f)] private float _distortionRadiusSpeed = 1.6f;
    [SerializeField, Range(.005f, .08f)] private float _distortionRingWidth = .03f;
    [SerializeField, Range(0f, .02f)] private float _distortionStrength = .006f;
    [SerializeField, Range(0f, .004f)] private float _distortionNoiseStrength = .001f;
    [SerializeField, Range(1f, 60f)] private float _distortionNoiseScale = 24f;
    [SerializeField, Range(.5f, 4f)] private float _distortionFadePower = 1.5f;
    [SerializeField] private Color _distortionEdgeTint = new Color(.9f, .95f, 1f, 1f);
    [SerializeField, Range(0f, .05f)] private float _distortionEdgeTintStrength;
    public bool ScreenDistortionEnable => _screenDistortionEnable;
    public float DistortionDuration => _distortionDuration;
    public float DistortionMaxRadius => _distortionMaxRadius;
    public float DistortionRadiusSpeed => _distortionRadiusSpeed;
    public float DistortionRingWidth => _distortionRingWidth;
    public float DistortionStrength => _distortionStrength;
    public float DistortionNoiseStrength => _distortionNoiseStrength;
    public float DistortionNoiseScale => _distortionNoiseScale;
    public float DistortionFadePower => _distortionFadePower;
    public Color DistortionEdgeTint => _distortionEdgeTint;
    public float DistortionEdgeTintStrength => _distortionEdgeTintStrength;
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
    [Header("Just Avoid Counter / Blood")]
    [SerializeField, Range(1f, 1.4f)] private float _counterHitVFXSpeed = 1.3f;
    [SerializeField, Range(1f, 1.2f)] private float _counterHitVFXSize = 1.05f;
    public float CounterHitVFXSpeed => _counterHitVFXSpeed;
    public float CounterHitVFXSize => _counterHitVFXSize;
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
