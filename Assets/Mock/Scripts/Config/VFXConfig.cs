using UnityEngine;

/// <summary>命中、追撃、ジャスト回避のPrefabと表示量を定義する。命中判定やダメージ計算は担当しない。</summary>
[CreateAssetMenu(fileName = "VFXConfig", menuName = "Config/VFXConfig")]
public class VFXConfig : ScriptableObject
{
    /// <summary>敵への通常攻撃の血しぶき表示を定義する。追撃専用演出には適用しない。</summary>
    [System.Serializable]
    public sealed class NormalBloodStyle
    {
        /// <summary>SwordHitVFX/BloodStreaksの最初のBurst数。敵への通常攻撃だけ。</summary>
        [Min(0), Tooltip("SwordHitVFX/BloodStreaksの最初のBurst数。敵への通常攻撃だけ。")]
        public int StreakCount = 3;
        /// <summary>SwordHitVFX/BloodDropletsの最初のBurst数。</summary>
        [Min(0), Tooltip("SwordHitVFX/BloodDropletsの最初のBurst数。")]
        public int DropletCount = 4;
        /// <summary>通常BloodのPrefabサイズ倍率。Heavyでは既存VFXルートScaleも掛かります。Slashには適用しません。</summary>
        [Range(.25f, 2f), Tooltip("通常BloodのPrefabサイズ倍率。Heavyでは既存VFXルートScaleも掛かります。Slashには適用しません。")]
        public float Width = 1.25f;
        /// <summary>通常BloodのPrefab初速倍率。</summary>
        [Range(.25f, 2f), Tooltip("通常BloodのPrefab初速倍率。")]
        public float Speed = 1f;
        /// <summary>通常BloodのPrefab Stretch長倍率。</summary>
        [Range(.25f, 2f), Tooltip("通常BloodのPrefab Stretch長倍率。")]
        public float Length = 1f;
        /// <summary>通常BloodのPrefab寿命倍率。Pool返却時間内に収めてください。</summary>
        [Range(.5f, 1.5f), Tooltip("通常BloodのPrefab寿命倍率。Pool返却時間内に収めてください。")]
        public float Lifetime = 1f;
        /// <summary>斬撃軸に直交する、攻撃者から離れる成分を追加する割合。</summary>
        [Range(0f, .5f), Tooltip("斬撃軸に直交する、攻撃者から離れる成分を追加する割合。")]
        public float DirectionBias = .15f;
        /// <summary>Bloodだけの表示位置(m)。X=斬撃横成分の接線、Y=上、Z=攻撃者から離れる水平軸。合計最大0.10m。0でHitPointを維持。</summary>
        [Tooltip("Bloodだけの表示位置(m)。X=斬撃横成分の接線、Y=上、Z=攻撃者から離れる水平軸。合計最大0.10m。0でHitPointを維持。")]
        public Vector3 SpawnOffset;
    }
    /// <summary>敵への通常軽攻撃の血しぶき設定。追撃の専用血しぶきや一閃には適用しない。</summary>
    [Header("Enemy normal hit / Blood only (Counter uses its dedicated prefabs)")]
    [SerializeField] private NormalBloodStyle _lightBlood = new NormalBloodStyle();
    /// <summary>敵への通常強攻撃の血しぶき設定。追撃の専用血しぶきや一閃には適用しない。</summary>
    [SerializeField] private NormalBloodStyle _heavyBlood = new NormalBloodStyle
        { StreakCount = 4, DropletCount = 6, Width = 1.5f, Speed = 1.15f, Length = 1.25f, Lifetime = 1.1f, DirectionBias = .2f };
    /// <summary>通常攻撃の強弱に対応する血しぶき設定を取得する。追撃の専用演出には使用しない。</summary>
    public NormalBloodStyle GetNormalBloodStyle(bool heavy) => heavy ? _heavyBlood : _lightBlood;
    /// <summary>追撃の段別演出を定義する既存の設定アセット。</summary>
    [Header("Just Avoid Counter / Two strikes")]
    [SerializeField] private JustAvoidCounterConfig _counterPresentation;
    /// <summary>追撃1撃目・2撃目の既存の演出設定。</summary>
    public JustAvoidCounterConfig CounterPresentation => _counterPresentation;
    /// <summary>追撃2撃の命中点に出す一閃VFX。未設定なら出さない。</summary>
    [Header("Just Avoid Counter / Issen slash (Assets/Art)")]
    [SerializeField, Tooltip("追撃2撃の命中点に出す一閃VFX。未設定なら出さない。")]
    private KatsuroTimedVFX _counterIssenSlash;
    /// <summary>追撃1撃目の一閃の大きさ（基準長2m）。</summary>
    [SerializeField, Range(.3f, 2f), Tooltip("追撃1撃目の一閃の大きさ（基準長2m）。")]
    private float _counterFirstIssenScale = .75f;
    /// <summary>追撃2撃目（締め）の一閃の大きさ（基準長2m）。</summary>
    [SerializeField, Range(.3f, 2f), Tooltip("追撃2撃目（締め）の一閃の大きさ（基準長2m）。")]
    private float _counterSecondIssenScale = 1.05f;
    /// <summary>追撃の各段に独立して適用するArt一閃の表示設定。</summary>
    [System.Serializable]
    public sealed class CounterIssenStyle
    {
        /// <summary>命中位置からのオフセット。X=斬撃方向、Y=面内上方向、Z=カメラ方向。</summary>
        [Tooltip("命中位置からのオフセット。X=斬撃方向、Y=面内上方向、Z=カメラ方向。")]
        public Vector3 PositionOffset;
        /// <summary>命中方向とカメラから算出した基準回転に追加するローカルEuler角（度）。</summary>
        public Vector3 RotationOffset;
        /// <summary>各Trackの発光ピークに乗算する一閃の発光倍率。</summary>
        [Min(0f)] public float EmissionIntensity = 1f;
        /// <summary>各Trackの時間配分を維持して伸縮する、一閃全体の再生時間（秒）。</summary>
        [Min(.01f)] public float Duration = .22f;
    }
    /// <summary>追撃1撃目専用の位置・回転・発光・持続時間の設定。</summary>
    [SerializeField] private CounterIssenStyle _counterFirstIssenStyle = new CounterIssenStyle { EmissionIntensity = .75f, Duration = .19f };
    /// <summary>追撃2撃目専用の位置・回転・発光・持続時間の設定。</summary>
    [SerializeField] private CounterIssenStyle _counterSecondIssenStyle = new CounterIssenStyle { EmissionIntensity = 1f, Duration = .24f };
    /// <summary>追撃1撃目または2撃目のArt一閃の位置・回転・発光・持続時間の設定を取得する。</summary>
    public CounterIssenStyle GetCounterIssenStyle(bool first) => first ? _counterFirstIssenStyle : _counterSecondIssenStyle;
    /// <summary>命中演出側から参照するArt一閃Prefab。</summary>
    public KatsuroTimedVFX CounterIssenSlash => _counterIssenSlash;
    /// <summary>追撃1撃目または2撃目のArt一閃のサイズ倍率を取得する。</summary>
    public float GetCounterIssenScale(bool first) => first ? _counterFirstIssenScale : _counterSecondIssenScale;

    /// <summary>鬼武者の一閃のような追撃演出（画面の白飛び・巨大な弧・放射状の光の筋・白いしぶき・青い火花）。</summary>
    [System.Serializable]
    public sealed class IssenFinisher
    {
        [Tooltip("一閃演出を出す。")] public bool Enabled = true;
        [Header("画面の白飛び")]
        [ColorUsage(false, true), Tooltip("白飛びの色。")] public Color FlashColor = new Color(.86f, .93f, 1f, 1f);
        [Range(0f, 3f), Tooltip("1撃目の白飛びの強さ。")] public float FirstFlash = .45f;
        [Range(0f, 3f), Tooltip("2撃目（締め）の白飛びの強さ。")] public float SecondFlash = 1.1f;
        [Range(.02f, .5f), Tooltip("白飛びが引くまでの秒数。")] public float FlashDuration = .14f;
        [Header("巨大な弧（体の周りを走る光の面）")]
        [Tooltip("1撃目に弧を出す。")] public bool FirstArc = true;
        [Tooltip("2撃目に弧を出す。")] public bool SecondArc;
        [ColorUsage(false, true), Tooltip("弧の色。")] public Color ArcColor = new Color(.9f, .95f, 1f, 1f);
        [Range(0f, 10f), Tooltip("弧の明るさ。")] public float ArcIntensity = 2.2f;
        [Range(30f, 300f), Tooltip("弧の角度（度）。")] public float ArcAngle = 150f;
        [Range(0f, 3f), Tooltip("命中点より外へ広げる半径の余白（m）。")] public float ArcRadiusPadding = .8f;
        [Range(.01f, .3f), Tooltip("弧が尾から先端まで走る秒数。")] public float ArcSweep = .06f;
        [Range(.05f, .8f), Tooltip("走り切ってから消えるまでの秒数。")] public float ArcFade = .22f;
        [Header("放射状の光の筋")]
        [Range(0, 80)] public int StreakCount = 28;
        [Range(1f, 60f), Tooltip("筋が飛ぶ速さ（m/s）。")] public float StreakSpeed = 22f;
        [ColorUsage(false, true)] public Color StreakColor = new Color(.9f, .96f, 1f, 1f);
        [Header("白いしぶき")]
        [Range(0, 60)] public int MistCount = 16;
        public Color MistColor = new Color(1f, 1f, 1f, .35f);
        [Header("青い火花（余韻）")]
        [Range(0, 120)] public int SparkCount = 40;
        [ColorUsage(false, true)] public Color SparkColor = new Color(.45f, .75f, 1f, 1f);
        [Range(0f, 10f)] public float SparkIntensity = 3f;
    }
    [Header("Just Avoid Counter / Onimusha-style finisher")]
    [SerializeField] private IssenFinisher _issenFinisher = new IssenFinisher();
    [SerializeField, Tooltip("巨大な弧に使う Material（一閃の光の面：M_Issen_LightSheet）。")] private Material _issenArcMaterial;
    [SerializeField, Tooltip("しぶき・光の筋・火花の元にする Material（Katsuro/VFXParticle の手続き的な粒：M_Rush_Dust）。")] private Material _issenParticleMaterial;
    public IssenFinisher IssenFinisherStyle => _issenFinisher;
    public Material IssenArcMaterial => _issenArcMaterial;
    public Material IssenParticleMaterial => _issenParticleMaterial;
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
    [SerializeField, Range(.7f, 1.2f)] private float _counterHitVFXSize = .9f;
    /// <summary>Just Avoid追撃命中VFXの粒子速度補正。</summary>
    public float CounterHitVFXSpeed => _counterHitVFXSpeed;
    /// <summary>Just Avoid追撃命中VFXの粒子サイズ補正。</summary>
    public float CounterHitVFXSize => _counterHitVFXSize;
    /// <summary>追撃の血粒子数倍率。Slash Lineには掛けない。</summary>
    [SerializeField, Range(1f, 2f), Tooltip("Just Avoid追撃の血粒子数倍率。")]
    private float _counterHitVFXAmount = 1.6f;
    /// <summary>追撃の血の広がり。通常Heavyよりまとまった斬り抜け方向を作る。</summary>
    [SerializeField, Range(.2f, 1.3f), Tooltip("追撃の血飛沫Cone角度へ掛ける倍率。")]
    private float _counterHitVFXSpread = .85f;
    /// <summary>追撃の血粒子数倍率。</summary>
    public float CounterHitVFXAmount => _counterHitVFXAmount;
    /// <summary>追撃の血のCone角度倍率。</summary>
    public float CounterHitVFXSpread => _counterHitVFXSpread;
    /// <summary>追撃の接触点だけに与えるHDR輝度。サイズや透明度とは独立する。</summary>
    [SerializeField, Range(1f, 6f), Tooltip("追撃Contact FlashのHDR輝度。小さい接触点だけを一瞬強調します。")]
    private float _counterContactIntensity = 3.2f;
    /// <summary>追撃の接触Flashに掛けるHDR輝度。</summary>
    public float CounterContactIntensity => _counterContactIntensity;
    /// <summary>追撃の血の寿命倍率。白い線が消えた後に短い血の余韻を残す。</summary>
    [SerializeField, Range(.5f, 1.2f), Tooltip("追撃の血飛沫寿命倍率。Slash Lineには適用しません。")]
    private float _counterBloodLifetime = .78f;
    /// <summary>追撃の方向性を示す血の筋だけを伸ばす倍率。</summary>
    [SerializeField, Range(1f, 2f), Tooltip("追撃BloodStreaksの長さ倍率。粒子数や白い線の幅は増やしません。")]
    private float _counterBloodStreakLength = 1.3f;
    /// <summary>追撃の血飛沫の赤黒い基準色。煙状粒子には適用しない。</summary>
    [SerializeField, Tooltip("追撃の血の色。通常攻撃の色とMaterialは維持します。")]
    private Color _counterBloodTint = new Color(.55f, .024f, .035f, .85f);
    /// <summary>追撃の血飛沫寿命倍率。</summary>
    public float CounterBloodLifetime => _counterBloodLifetime;
    /// <summary>追撃の方向性を示す血の筋の長さ倍率。</summary>
    public float CounterBloodStreakLength => _counterBloodStreakLength;
    /// <summary>追撃の血の基準色。</summary>
    public Color CounterBloodTint => _counterBloodTint;
    /// <summary>接触点のFlashサイズ。X=Light、Y=Heavy、Z=追撃。</summary>
    [SerializeField, Tooltip("接触Flashのサイズ（m）。命中点だけを強調します。")]
    private Vector3 _contactFlashSizes = new Vector3(.065f, .085f, .11f);
    /// <summary>接触Flashの実時間寿命。血飛沫と被弾姿勢を早く見せる。</summary>
    [SerializeField, Tooltip("接触Flash寿命（秒）。X=Light、Y=Heavy、Z=追撃。")]
    private Vector3 _contactFlashDurations = new Vector3(.018f, .028f, .036f);
    /// <summary>接触Flashの透明度。X=Light、Y=Heavy、Z=追撃。</summary>
    [SerializeField, Tooltip("接触Flashの強度。X=Light、Y=Heavy、Z=追撃。")]
    private Vector3 _contactFlashStrengths = new Vector3(.2f, .32f, .42f);
    /// <summary>攻撃種別に対応する接触Flashのサイズ・寿命・強度を返す。</summary>
    public Vector3 GetContactFlashStyle(bool heavy, bool counter)
    {
        int index = counter ? 2 : heavy ? 1 : 0;
        return new Vector3(Mathf.Max(.01f, _contactFlashSizes[index]), Mathf.Max(.01f, _contactFlashDurations[index]), Mathf.Clamp01(_contactFlashStrengths[index]));
    }
    /// <summary>斬撃線のワールド長。XはLight、YはHeavy、Zは追撃。</summary>
    [SerializeField, Tooltip("命中斬撃線の長さ（m）。X=Light、Y=Heavy、Z=Just Avoid追撃。")]
    private Vector3 _slashLineLengths = new Vector3(.72f, .95f, 1.1f);
    /// <summary>斬撃線の幅。XはLight、YはHeavy、Zは追撃。</summary>
    [SerializeField, Tooltip("命中斬撃線の幅（m）。X=Light、Y=Heavy、Z=追撃。強い発光リボンにはしません。")]
    private Vector3 _slashLineWidths = new Vector3(.022f, .03f, .026f);
    /// <summary>斬撃線の総寿命。約前半が強い線、後半が淡い残像になる。</summary>
    [SerializeField, Tooltip("斬撃線の総寿命（実時間秒）。X=Light、Y=Heavy、Z=追撃。Pause中は停止します。")]
    private Vector3 _slashLineDurations = new Vector3(.09f, .12f, .105f);
    /// <summary>攻撃種別に対応する斬撃線の長さ・幅・総寿命を返す。判定や攻撃速度には使用しない。</summary>
    public Vector3 GetSlashLineStyle(bool heavy, bool counter)
    {
        int index = counter ? 2 : heavy ? 1 : 0;
        return new Vector3(Mathf.Max(.01f, _slashLineLengths[index]), Mathf.Max(.001f, _slashLineWidths[index]), Mathf.Max(.01f, _slashLineDurations[index]));
    }
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
