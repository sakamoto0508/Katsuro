using UnityEngine;

/// <summary>追撃二段だけの表示設定。入力・ダメージ・Animation Eventには使用しない。</summary>
[CreateAssetMenu(menuName = "Config/Just Avoid Counter", fileName = "JustAvoidCounterConfig")]
public sealed class JustAvoidCounterConfig : ScriptableObject
{
    [System.Serializable]
    public sealed class Strike
    {
        [Header("Sword-following silver slash")]
        [Tooltip("追撃専用RibbonのMaterial。通常Light / Heavyには使用しません。")]
        public Material SlashMaterial;
        [Range(0f, .9f), Tooltip("刀身の何割目から面を作るか。0は刀の根元、1は切先。")]
        public float BladeStart = .28f;
        [Range(0f, .35f), Tooltip("刀身長に対する切先の表示延長。判定や刀Transformは変更しません。")]
        public float TipExtension = .06f;
        [Min(.01f)] public float TrailDuration = .085f;
        [Range(0f, 2f)] public float TrailBrightness = 1.05f;
        [Range(.1f, 1.6f), Tooltip("切先を固定したまま根元側へ広げるRibbonの幅倍率。")]
        public float TrailWidth = .7f;
        [Range(0f, 4f)] public float CoreIntensity = 1.2f;
        [Range(.033f, .3f), Tooltip("各軌跡サンプルのコア寿命。命中時の表示保持はHitStop時間を読み取り別途行います。")]
        public float CoreDuration = .055f;
        [Range(0f, 1f)] public float Afterglow = .22f;
        [Header("Impact straight blade (presentation only)")]
        public Material StrokeMaterial;
        [Range(.2f, 3f), Tooltip("命中点を通る直線の長さ(m)。前方へ移動しません。")]
        public float StrokeLength = 1.35f;
        [Range(.01f, .4f), Tooltip("先細り斬撃板の最大幅(m)。芯の幅はStroke Materialで調整します。")]
        public float StrokeWidth = .09f;
        [Range(.033f, .2f), Tooltip("白い芯が消えるまでの秒数。既存HitStopは変更しません。")]
        public float StrokeCoreDuration = .055f;
        [Range(.05f, .4f), Tooltip("青い面と残光も消えるまでの秒数。Pause中は停止します。")]
        public float StrokeDuration = .14f;
        [Range(0f, 3f)] public float StrokeIntensity = 1f;
        [Min(0f)] public float HitStop = .045f;
        [Header("Counter impact pose (presentation only)")]
        [Range(0f, .25f), Tooltip("被弾Clipを命中時に評価する正規化位置。Eventや拘束時間は変更しません。")]
        public float ImpactPoseTime = .05f;
        [Range(.1f, .4f), Tooltip("表示上の反応時間。敵の行動拘束時間は維持します。")]
        public float PoseDuration = .18f;
        [Range(0f, 25f)] public float TorsoImpact = 9f;
        [Range(0f, 30f)] public float TorsoFollowThrough = 4f;
        [Range(0f, 12f)] public float HeadRecoil = 5f;
        [Range(0f, 8f)] public float HipFollowThrough = 1.5f;
        [Range(.03f, .25f)] public float FollowThroughPeak = .07f;
    }

    public Strike First = new Strike();
    public Strike Second = new Strike { TrailDuration = .16f, TrailBrightness = 1.2f,
        TrailWidth = .8f, CoreIntensity = 3.2f, CoreDuration = .09f, Afterglow = .38f, HitStop = .155f,
        TorsoImpact = 14f, TorsoFollowThrough = 24f, HeadRecoil = 9f, HipFollowThrough = 4f, FollowThroughPeak = .14f, PoseDuration = .4f };
    [Tooltip("1撃目の白銀の切断線・接触光・深紅の血。通常HitVFXとは別に再利用する。")]
    public JustAvoidCounterCutVFX FirstCFX;
    [Tooltip("2撃目の実命中だけで再生する専用CFX。")]
    public JustAvoidCounterCutVFX FinisherCFX;
    [Tooltip("専用FirstCFX未設定時だけ使う互換Flash: サイズ(m) / 寿命(s) / 強度。")]
    public Vector3 FirstContact = new Vector3(.09f, .045f, .5f);
    [Range(.1f, 1f), Tooltip("専用FirstCFX未設定時だけ使う旧HitVFXの血粒子倍率。")]
    public float FirstBloodAmount = .5f;
    [Range(0f, 1f)] public float FirstReactionWeight = .22f;
    [Range(.01f, .12f)] public float SecondReactionBlendIn = .035f;
    [Range(0f, 1f)] public float SecondReactionImpactWeight = .5f;

    public Strike GetStrike(JustAvoidCounterStrike strike) => strike == JustAvoidCounterStrike.First ? First : Second;
    public JustAvoidCounterCutVFX GetCFX(JustAvoidCounterStrike strike) => strike == JustAvoidCounterStrike.First ? FirstCFX : FinisherCFX;
}

/// <summary>命中窓を開いたStateが確定する演出段階。通常コンボ段数とは独立。</summary>
public enum JustAvoidCounterStrike { None, First, Second }
