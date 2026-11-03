using UnityEngine;

/// <summary>追撃二段だけの表示設定。入力・ダメージ・Animation Eventには使用しない。</summary>
[CreateAssetMenu(menuName = "Config/Just Avoid Counter", fileName = "JustAvoidCounterConfig")]
public sealed class JustAvoidCounterConfig : ScriptableObject
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>BloodOnlyは追撃1・2の血以外の命中VFXだけを隠します。白黒演出・HitStop・HitReactionは維持。比較時はPlayを停止して切り替えてください。</summary>
    [Header("Counter VFX diagnostics (Editor / Development Build only)")]
    [Tooltip("BloodOnlyは追撃1・2の血以外の命中VFXだけを隠します。白黒演出・HitStop・HitReactionは維持。比較時はPlayを停止して切り替えてください。")]
    public CounterVFXDebugMode VFXDebugMode;
#endif
    /// <summary>追撃の血以外の命中VFXを隠す診断モードか。通常ビルドでは常にfalse。</summary>
    public bool IsBloodOnlyDebug
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return VFXDebugMode == CounterVFXDebugMode.BloodOnly;
#else
            return false;
#endif
        }
    }
    /// <summary>追撃の各段に適用する刀の軌跡・命中線・HitStop・被弾ポーズの演出設定。</summary>
    [System.Serializable]
    public sealed class Strike
    {
        /// <summary>追撃専用RibbonのMaterial。通常Light / Heavyには使用しません。</summary>
        [Header("Sword-following silver slash")]
        [Tooltip("追撃専用RibbonのMaterial。通常Light / Heavyには使用しません。")]
        public Material SlashMaterial;
        /// <summary>刀身の何割目から面を作るか。0は刀の根元、1は切先。</summary>
        [Range(0f, .9f), Tooltip("刀身の何割目から面を作るか。0は刀の根元、1は切先。")]
        public float BladeStart = .28f;
        /// <summary>刀身長に対する切先の表示延長。判定や刀Transformは変更しません。</summary>
        [Range(0f, .35f), Tooltip("刀身長に対する切先の表示延長。判定や刀Transformは変更しません。")]
        public float TipExtension = .06f;
        /// <summary>刀の軌跡の各サンプルが残る基準時間（秒）。</summary>
        [Min(.01f)] public float TrailDuration = .085f;
        /// <summary>刀の軌跡の明るさの倍率。</summary>
        [Range(0f, 2f)] public float TrailBrightness = 1.05f;
        /// <summary>切先を固定したまま根元側へ広げるRibbonの幅倍率。</summary>
        [Range(.1f, 1.6f), Tooltip("切先を固定したまま根元側へ広げるRibbonの幅倍率。")]
        public float TrailWidth = .7f;
        /// <summary>刀の軌跡に含まれる白銀コアの発光強度。</summary>
        [Range(0f, 4f)] public float CoreIntensity = 1.2f;
        /// <summary>各軌跡サンプルのコア寿命。命中時の表示保持はHitStop時間を読み取り別途行います。</summary>
        [Range(.033f, .3f), Tooltip("各軌跡サンプルのコア寿命。命中時の表示保持はHitStop時間を読み取り別途行います。")]
        public float CoreDuration = .055f;
        /// <summary>刀の軌跡に含まれる残光の表示強度（0〜1）。</summary>
        [Range(0f, 1f)] public float Afterglow = .22f;
        /// <summary>命中点を通る旧直線斬撃のMaterial。Art一閃を使用する経路では旧命中線を表示しない。</summary>
        [Header("Impact straight blade (presentation only)")]
        public Material StrokeMaterial;
        /// <summary>命中点を通る直線の長さ(m)。前方へ移動しません。</summary>
        [Range(.2f, 3f), Tooltip("命中点を通る直線の長さ(m)。前方へ移動しません。")]
        public float StrokeLength = 1.35f;
        /// <summary>先細り斬撃板の最大幅(m)。芯の幅はStroke Materialで調整します。</summary>
        [Range(.01f, .4f), Tooltip("先細り斬撃板の最大幅(m)。芯の幅はStroke Materialで調整します。")]
        public float StrokeWidth = .09f;
        /// <summary>命中から白銀コアを保持する表示上の秒数。AnimatorやHitStopは操作しません。</summary>
        [Range(0f, .2f), Tooltip("命中から白銀コアを保持する表示上の秒数。AnimatorやHitStopは操作しません。")]
        public float StrokeCoreHold = .033f;
        /// <summary>命中から白い芯が消えるまでの秒数。Hold以降に減衰します。</summary>
        [Range(.033f, .3f), Tooltip("命中から白い芯が消えるまでの秒数。Hold以降に減衰します。")]
        public float StrokeCoreDuration = .055f;
        /// <summary>前縁・切先の鮮烈なピークが消えるまでの秒数。芯の保持とは独立。</summary>
        [Range(.02f, .12f), Tooltip("前縁・切先の鮮烈なピークが消えるまでの秒数。芯の保持とは独立。")]
        public float StrokePeakDuration = .05f;
        /// <summary>命中直後だけ白銀コアに掛ける倍率。1で無効。面・残光の強度は変えません。Peak Duration内に元の輝度へ戻ります。</summary>
        [Range(1f, 3f), Tooltip("命中直後だけ白銀コアに掛ける倍率。1で無効。面・残光の強度は変えません。Peak Duration内に元の輝度へ戻ります。")]
        public float StrokeHitPeak = 1f;
        /// <summary>青い面と残光も消えるまでの秒数。Pause中は停止します。</summary>
        [Range(.05f, .4f), Tooltip("青い面と残光も消えるまでの秒数。Pause中は停止します。")]
        public float StrokeDuration = .14f;
        /// <summary>命中点を通る直線斬撃の全体の発光強度。</summary>
        [Range(0f, 3f)] public float StrokeIntensity = 1f;
        /// <summary>視線平面への投影長がこの割合未満の場合だけ補正。0で無効。</summary>
        [Range(0f, .8f), Tooltip("視線平面への投影長がこの割合未満の場合だけ補正。0で無効。")]
        public float StrokeMinProjection = .55f;
        /// <summary>実斬撃方向から表示軸を回す上限角度。水平固定せず命中点も動かしません。0で無効。</summary>
        [Range(0f, 30f), Tooltip("実斬撃方向から表示軸を回す上限角度。水平固定せず命中点も動かしません。0で無効。")]
        public float StrokeMaxProjectionAngle = 20f;
        /// <summary>この段の命中時に適用するHitStopの時間（秒）。</summary>
        [Min(0f)] public float HitStop = .045f;
        /// <summary>被弾Clipを命中時に評価する正規化位置。Eventや拘束時間は変更しません。</summary>
        [Header("Counter impact pose (presentation only)")]
        [Range(0f, .25f), Tooltip("被弾Clipを命中時に評価する正規化位置。Eventや拘束時間は変更しません。")]
        public float ImpactPoseTime = .05f;
        /// <summary>表示上の反応時間。敵の行動拘束時間は維持します。</summary>
        [Range(.1f, .4f), Tooltip("表示上の反応時間。敵の行動拘束時間は維持します。")]
        public float PoseDuration = .18f;
        /// <summary>命中直後に上体へ追加する反応回転の基準角度（度）。実際の角度にはボーンの重みと時間曲線を掛ける。</summary>
        [Range(0f, 25f)] public float TorsoImpact = 9f;
        /// <summary>命中後、上体が崩れる方向へ追加する回転の基準角度（度）。</summary>
        [Range(0f, 30f)] public float TorsoFollowThrough = 4f;
        /// <summary>命中直後の頭の反動の基準角度（度）。後続の崩れにもこの値を元にした回転を使う。</summary>
        [Range(0f, 12f)] public float HeadRecoil = 5f;
        /// <summary>上体に追従して骨盤へ追加する回転の基準角度（度）。</summary>
        [Range(0f, 8f)] public float HipFollowThrough = 1.5f;
        /// <summary>命中後の崩れの動きが最大へ達する基準時間（秒）。頭と骨盤には小さな遅延を加える。</summary>
        [Range(.03f, .25f)] public float FollowThroughPeak = .07f;
        /// <summary>停止解除後、崩れの最大姿勢を短く保つ秒数。Pose Duration内で復帰し、行動不能時間は延長しません。</summary>
        [Range(0f, .08f), Tooltip("停止解除後、崩れの最大姿勢を短く保つ秒数。Pose Duration内で復帰し、行動不能時間は延長しません。")]
        public float FollowThroughHold = 0f;
    }

    /// <summary>追撃1撃目の刀の軌跡・命中・被弾演出の設定。</summary>
    public Strike First = new Strike();
    /// <summary>追撃2撃目の刀の軌跡・命中・被弾演出の設定。1撃目とは独立して調整する。</summary>
    public Strike Second = new Strike { TrailDuration = .16f, TrailBrightness = 1.2f,
        TrailWidth = .8f, CoreIntensity = 3.2f, CoreDuration = .09f, Afterglow = .38f, HitStop = .155f,
        StrokeCoreHold = .155f, StrokeCoreDuration = .205f, StrokePeakDuration = .065f, StrokeDuration = .275f,
        TorsoImpact = 14f, TorsoFollowThrough = 24f, HeadRecoil = 9f, HipFollowThrough = 4f, FollowThroughPeak = .14f, PoseDuration = .4f };
    /// <summary>1撃目の白銀の切断線・接触光・深紅の血。通常HitVFXとは別に再利用する。</summary>
    [Tooltip("1撃目の白銀の切断線・接触光・深紅の血。通常HitVFXとは別に再利用する。")]
    public JustAvoidCounterCutVFX FirstCFX;
    /// <summary>2撃目の実命中だけで再生する専用CFX。</summary>
    [Tooltip("2撃目の実命中だけで再生する専用CFX。")]
    public JustAvoidCounterCutVFX FinisherCFX;
    /// <summary>専用FirstCFX未設定時だけ使う互換Flash: サイズ(m) / 寿命(s) / 強度。</summary>
    [Tooltip("専用FirstCFX未設定時だけ使う互換Flash: サイズ(m) / 寿命(s) / 強度。")]
    public Vector3 FirstContact = new Vector3(.09f, .045f, .5f);
    /// <summary>専用FirstCFX未設定時だけ使う旧HitVFXの血粒子倍率。</summary>
    [Range(.1f, 1f), Tooltip("専用FirstCFX未設定時だけ使う旧HitVFXの血粒子倍率。")]
    public float FirstBloodAmount = .5f;
    /// <summary>追撃1撃目で適用する被弾アニメーションの重み（0〜1）。</summary>
    [Range(0f, 1f)] public float FirstReactionWeight = .22f;
    /// <summary>追撃2撃目の被弾アニメーションへブレンドする時間（秒）。</summary>
    [Range(.01f, .12f)] public float SecondReactionBlendIn = .035f;
    /// <summary>追撃2撃目の命中直後に確保する被弾アニメーションの最小の開始重み（0〜1）。</summary>
    [Range(0f, 1f)] public float SecondReactionImpactWeight = .5f;

    /// <summary>指定した追撃段の演出設定を返す。First以外の場合はSecondを返す。</summary>
    public Strike GetStrike(JustAvoidCounterStrike strike) => strike == JustAvoidCounterStrike.First ? First : Second;
    /// <summary>指定した追撃段の命中CFXを返す。First以外の場合はFinisherCFXを返す。</summary>
    public JustAvoidCounterCutVFX GetCFX(JustAvoidCounterStrike strike) => strike == JustAvoidCounterStrike.First ? FirstCFX : FinisherCFX;
}

/// <summary>命中窓を開いたStateが確定する演出段階。通常コンボ段数とは独立。</summary>
public enum JustAvoidCounterStrike { None, First, Second }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>追撃の命中VFXの診断表示。Normalは通常表示、BloodOnlyは血以外の命中VFXを隠す。</summary>
public enum CounterVFXDebugMode { Normal, BloodOnly }
#endif
