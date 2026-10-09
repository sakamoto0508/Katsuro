using UnityEngine;

/// <summary>命中、追撃、ジャスト回避のPrefabと表示量を定義する。命中判定やダメージ計算は担当しない。</summary>
[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    /// <summary>Just Avoid成功時の画面歪みを有効にする。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時の画面歪みを有効にする。")]
    [Header("Just Avoid / Screen Distortion")]
    [SerializeField] private bool _screenDistortionEnable = true;
    /// <summary>Just Avoid画面歪みが消えるまでの時間（秒）。</summary>
    [UnityEngine.Tooltip("Just Avoid画面歪みが消えるまでの時間（秒）。")]
    [SerializeField, Range(.1f, .18f)] private float _distortionDuration = .14f;
    /// <summary>画面高さを1とした最終半径。画面の隅まで届く半径は自動で保証する。</summary>
    [Tooltip("画面高さを1とした最終半径。画面の隅まで届く半径は自動で保証する。")]
    [SerializeField, Min(.1f)] private float _distortionMaxRadius = 1.15f;
    /// <summary>拡大速度の曲線。大きいほど最初に速く広がる。</summary>
    [Tooltip("拡大速度の曲線。大きいほど最初に速く広がる。")]
    [SerializeField, Range(1f, 3f)] private float _distortionRadiusSpeed = 1.6f;
    /// <summary>画面歪みのリング幅。画面UVを基準にした値。</summary>
    [UnityEngine.Tooltip("画面歪みのリング幅。画面UVを基準にした値。")]
    [SerializeField, Range(.005f, .08f)] private float _distortionRingWidth = .03f;
    /// <summary>リング付近の画面UVをずらす強さ。大きいほど歪みが強くなる。</summary>
    [UnityEngine.Tooltip("リング付近の画面UVをずらす強さ。大きいほど歪みが強くなる。")]
    [SerializeField, Range(0f, .02f)] private float _distortionStrength = .006f;
    /// <summary>画面歪みに重ねるノイズの強さ。</summary>
    [UnityEngine.Tooltip("画面歪みに重ねるノイズの強さ。")]
    [SerializeField, Range(0f, .004f)] private float _distortionNoiseStrength = .001f;
    /// <summary>画面歪みのノイズの細かさを調整する倍率。</summary>
    [UnityEngine.Tooltip("画面歪みのノイズの細かさを調整する倍率。")]
    [SerializeField, Range(1f, 60f)] private float _distortionNoiseScale = 24f;
    /// <summary>画面歪みの減衰曲線の指数。消え方の速さを調整する。</summary>
    [UnityEngine.Tooltip("画面歪みの減衰曲線の指数。消え方の速さを調整する。")]
    [SerializeField, Range(.5f, 4f)] private float _distortionFadePower = 1.5f;
    /// <summary>画面歪みの縁に加える色。</summary>
    [UnityEngine.Tooltip("画面歪みの縁に加える色。")]
    [SerializeField] private Color _distortionEdgeTint = new Color(.9f, .95f, 1f, 1f);
    /// <summary>画面歪みの縁色を加える強さ。</summary>
    [UnityEngine.Tooltip("画面歪みの縁色を加える強さ。")]
    [SerializeField, Range(0f, .05f)] private float _distortionEdgeTintStrength;
    /// <summary>Just Avoid成功時の画面歪みを有効にする。</summary>
    public bool ScreenDistortionEnable => _screenDistortionEnable;
    /// <summary>Just Avoid画面歪みが消えるまでの時間（秒）。</summary>
    public float DistortionDuration => _distortionDuration;
    /// <summary>画面高さを1とした最終半径。画面の隅まで届く半径は自動で保証する。</summary>
    public float DistortionMaxRadius => _distortionMaxRadius;
    /// <summary>拡大速度の曲線。大きいほど最初に速く広がる。</summary>
    public float DistortionRadiusSpeed => _distortionRadiusSpeed;
    /// <summary>画面歪みのリング幅。画面UVを基準にした値。</summary>
    public float DistortionRingWidth => _distortionRingWidth;
    /// <summary>リング付近の画面UVをずらす強さ。大きいほど歪みが強くなる。</summary>
    public float DistortionStrength => _distortionStrength;
    /// <summary>画面歪みに重ねるノイズの強さ。</summary>
    public float DistortionNoiseStrength => _distortionNoiseStrength;
    /// <summary>画面歪みのノイズの細かさを調整する倍率。</summary>
    public float DistortionNoiseScale => _distortionNoiseScale;
    /// <summary>画面歪みの減衰曲線の指数。消え方の速さを調整する。</summary>
    public float DistortionFadePower => _distortionFadePower;
    /// <summary>画面歪みの縁に加える色。</summary>
    public Color DistortionEdgeTint => _distortionEdgeTint;
    /// <summary>画面歪みの縁色を加える強さ。</summary>
    public float DistortionEdgeTintStrength => _distortionEdgeTintStrength;
    /// <summary>薄い円形衝撃波のParticleSystem Prefab。未設定なら衝撃波のみ省略。自動破棄スクリプトは付けない。</summary>
    [Header("ジャスト回避")]
    [Tooltip("薄い円形衝撃波のParticleSystem Prefab。未設定なら衝撃波のみ省略。自動破棄スクリプトは付けない。")]
    [SerializeField] private GameObject _justAvoidShockwavePrefab;
    /// <summary>従来Particle Shockwaveの寿命（秒）。旧演出互換の設定。</summary>
    [UnityEngine.Tooltip("従来Particle Shockwaveの寿命（秒）。旧演出互換の設定。")]
    [SerializeField, Min(.01f)] private float _shockwaveDuration = .25f;
    /// <summary>従来Particle Shockwaveの有効設定。画面歪みとは別の旧演出設定。</summary>
    [UnityEngine.Tooltip("従来Particle Shockwaveの有効設定。画面歪みとは別の旧演出設定。")]
    [SerializeField] private bool _shockwaveEnable;
    /// <summary>従来Particle Shockwaveの強度設定。</summary>
    [UnityEngine.Tooltip("従来Particle Shockwaveの強度設定。")]
    [SerializeField, Range(0f, 1f)] private float _shockwaveStrength = .2f;
    /// <summary>従来Particle Shockwaveのサイズ倍率。</summary>
    [UnityEngine.Tooltip("従来Particle Shockwaveのサイズ倍率。")]
    [SerializeField, Min(.01f)] private float _shockwaveSize = .5f;
    /// <summary>追加のParticleSystem Prefab。未設定でも小さい接触フラッシュは表示する。</summary>
    [Header("命中")]
    [Tooltip("追加のParticleSystem Prefab。未設定でも小さい接触フラッシュは表示する。")]
    [SerializeField] private GameObject _hitVFX;
    /// <summary>命中VFXの再生後に片付けるまでの時間（秒）。</summary>
    [UnityEngine.Tooltip("命中VFXの再生後に片付けるまでの時間（秒）。")]
    [SerializeField, Min(.01f)] private float _hitVFXDuration = .32f;
    /// <summary>強攻撃の粒子数倍率。PrefabのBurst数を基準に毎回適用する。</summary>
    [Tooltip("強攻撃の粒子数倍率。PrefabのBurst数を基準に毎回適用する。")]
    [SerializeField, Range(1f, 1.6f)] private float _heavyHitVFXAmount = 1.45f;
    /// <summary>Heavy命中VFXの粒子の広がり補正。</summary>
    [UnityEngine.Tooltip("Heavy命中VFXの粒子の広がり補正。")]
    [SerializeField, Range(1f, 1.3f)] private float _heavyHitVFXSpread = 1.15f;
    /// <summary>Heavy命中VFXの粒子サイズ補正。</summary>
    [UnityEngine.Tooltip("Heavy命中VFXの粒子サイズ補正。")]
    [SerializeField, Range(1f, 1.2f)] private float _heavyHitVFXSize = 1.1f;
    /// <summary>Heavy命中VFXオブジェクト全体のScale倍率。</summary>
    [UnityEngine.Tooltip("Heavy命中VFXオブジェクト全体のScale倍率。")]
    [SerializeField, Range(1f, 1.4f)] private float _heavyHitVFXScale = 1.3f;
    /// <summary>Heavy命中VFXの粒子速度補正。</summary>
    [UnityEngine.Tooltip("Heavy命中VFXの粒子速度補正。")]
    [SerializeField, Range(1f, 1.3f)] private float _heavyHitVFXSpeed = 1.2f;
    /// <summary>Heavy命中VFXオブジェクト全体のScale倍率。</summary>
    public float HeavyHitVFXScale => _heavyHitVFXScale;
    /// <summary>Heavy命中VFXの粒子速度補正。</summary>
    public float HeavyHitVFXSpeed => _heavyHitVFXSpeed;
    /// <summary>強攻撃の粒子数倍率。PrefabのBurst数を基準に毎回適用する。</summary>
    public float HeavyHitVFXAmount => _heavyHitVFXAmount;
    /// <summary>Heavy命中VFXの粒子の広がり補正。</summary>
    public float HeavyHitVFXSpread => _heavyHitVFXSpread;
    /// <summary>Heavy命中VFXの粒子サイズ補正。</summary>
    public float HeavyHitVFXSize => _heavyHitVFXSize;
    /// <summary>Just Avoid追撃命中VFXの粒子速度補正。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃命中VFXの粒子速度補正。")]
    [Header("Just Avoid Counter / Blood")]
    [SerializeField, Range(1f, 1.4f)] private float _counterHitVFXSpeed = 1.3f;
    /// <summary>Just Avoid追撃命中VFXの粒子サイズ補正。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃命中VFXの粒子サイズ補正。")]
    [SerializeField, Range(1f, 1.2f)] private float _counterHitVFXSize = 1.05f;
    /// <summary>Just Avoid追撃命中VFXの粒子速度補正。</summary>
    public float CounterHitVFXSpeed => _counterHitVFXSpeed;
    /// <summary>Just Avoid追撃命中VFXの粒子サイズ補正。</summary>
    public float CounterHitVFXSize => _counterHitVFXSize;
    /// <summary>追加のParticleSystem Prefab。未設定でも小さい接触フラッシュは表示する。</summary>
    public GameObject HitVFX => _hitVFX;
    /// <summary>命中VFXの再生後に片付けるまでの時間（秒）。</summary>
    public float HitVFXDuration => _hitVFXDuration;
    /// <summary>従来Particle Shockwaveの有効設定。画面歪みとは別の旧演出設定。</summary>
    public bool ShockwaveEnable => _shockwaveEnable;
    /// <summary>従来Particle Shockwaveの強度設定。</summary>
    public float ShockwaveStrength => _shockwaveStrength;
    /// <summary>従来Particle Shockwaveのサイズ倍率。</summary>
    public float ShockwaveSize => _shockwaveSize;
    /// <summary>薄い円形衝撃波のParticleSystem Prefab。未設定なら衝撃波のみ省略。自動破棄スクリプトは付けない。</summary>
    public GameObject JustAvoidShockwavePrefab => _justAvoidShockwavePrefab;
    /// <summary>従来Particle Shockwaveの寿命（秒）。旧演出互換の設定。</summary>
    public float ShockwaveDuration => _shockwaveDuration;
    /// <summary>CharacterEffectに登録した回復Effectの再生名。</summary>
    public string PlayEffectHeal => _playEffectHeal;
    /// <summary>CharacterEffectに登録した幽体化Effectの再生名。</summary>
    public string PlayEffectGhost => _playEffectGhost;
    /// <summary>CharacterEffectに登録したバフEffectの再生名。</summary>
    public string PlayEffectBuff => _playEffectBuff;
    /// <summary>CharacterEffectに登録した回復Effectの再生名。</summary>
    [UnityEngine.Tooltip("CharacterEffectに登録した回復Effectの再生名。")]
    [SerializeField] private string _playEffectHeal;
    /// <summary>CharacterEffectに登録した幽体化Effectの再生名。</summary>
    [UnityEngine.Tooltip("CharacterEffectに登録した幽体化Effectの再生名。")]
    [SerializeField] private string _playEffectGhost;
    /// <summary>CharacterEffectに登録したバフEffectの再生名。</summary>
    [UnityEngine.Tooltip("CharacterEffectに登録したバフEffectの再生名。")]
    [SerializeField] private string _playEffectBuff;
}
