using UnityEngine;

/// <summary>
/// 牙突風 RushAttack 専用の演出設定。アニメーション・Root Motion・攻撃判定には触れず、見た目だけを決める。
/// 時刻は「RushAttack の元Clip秒」で指定し、実時間への換算は Pattern の構え時間・突進速度から自動で行う。
/// </summary>
[CreateAssetMenu(fileName = "GatotsuRushVFX", menuName = "Config/Gatotsu Rush VFX")]
public sealed class GatotsuRushVFXConfig : ScriptableObject
{
    /// <summary>構え中の刀先の収束光を定義する。</summary>
    [System.Serializable]
    public sealed class TipGlint
    {
        /// <summary>構え中に刀先へ出す収束光（KatsuroTimedVFX）。未設定なら出さない。</summary>
        [Tooltip("構え中に刀先へ出す収束光（KatsuroTimedVFX）。未設定なら出さない。")]
        public KatsuroTimedVFX Prefab;
        /// <summary>収束光の色（Prefab の各Materialの色に乗算）。</summary>
        [ColorUsage(true, true), Tooltip("収束光の色（Prefab の各Materialの色に乗算）。")]
        public Color Color = new Color(.85f, .93f, 1f, 1f);
        /// <summary>明るさの倍率。予兆を隠さないよう控えめが基本。</summary>
        [Range(0f, 3f), Tooltip("明るさの倍率。予兆を隠さないよう控えめが基本。")]
        public float Intensity = .7f;
        /// <summary>大きさ（m換算の倍率）。</summary>
        [Range(.05f, 1f), Tooltip("大きさ（m換算の倍率）。")]
        public float Size = .28f;
        /// <summary>突進開始の何秒前から収束を始めるか（実時間）。構え時間より長い場合は構えの開始から。</summary>
        [Range(.05f, 1.5f), Tooltip("突進開始の何秒前から収束を始めるか（実時間）。構え時間より長い場合は構えの開始から。")]
        public float LeadTime = .32f;
    }

    /// <summary>蹴り出しの砂埃を定義する。</summary>
    [System.Serializable]
    public sealed class Dust
    {
        /// <summary>この演出要素を有効にする。</summary>
        public bool Enabled = true;
        /// <summary>砂埃の色と最大の濃さ（Alpha）。</summary>
        [Tooltip("砂埃の色と最大の濃さ（Alpha）。")]
        public Color Color = new Color(.52f, .47f, .40f, .55f);
        /// <summary>一度に出る粒の数。</summary>
        [Range(1, 40), Tooltip("一度に出る粒の数。")]
        public int Count = 14;
        /// <summary>粒の大きさ（m）。</summary>
        [Range(.05f, 1.5f), Tooltip("粒の大きさ（m）。")]
        public float Size = .35f;
        /// <summary>粒が消えるまでの秒数。</summary>
        [Range(.1f, 2f), Tooltip("粒が消えるまでの秒数。")]
        public float Lifetime = .55f;
        /// <summary>後方・上方向へ飛び散る速さ（m/s）。</summary>
        [Range(0f, 8f), Tooltip("後方・上方向へ飛び散る速さ（m/s）。")]
        public float Speed = 2.4f;
        /// <summary>蹴り出しからの遅れ（実時間・秒）。</summary>
        [Range(0f, .3f), Tooltip("蹴り出しからの遅れ（実時間・秒）。")]
        public float Delay = 0f;
    }

    /// <summary>突進中の風の軌跡と発生位置を定義する。</summary>
    [System.Serializable]
    public sealed class Wind
    {
        /// <summary>この演出要素を有効にする。</summary>
        public bool Enabled = true;
        /// <summary>風の軌跡の色（Alphaが濃さ）。</summary>
        [ColorUsage(true, true), Tooltip("風の軌跡の色（Alphaが濃さ）。")]
        public Color Color = new Color(.78f, .88f, 1f, .5f);
        /// <summary>明るさ。</summary>
        [Range(0f, 10f), Tooltip("明るさ。")]
        public float Intensity = 1.6f;
        /// <summary>線の太さ（m）。細く鋭く。</summary>
        [Range(.003f, .1f), Tooltip("線の太さ（m）。細く鋭く。")]
        public float Width = .018f;
        /// <summary>軌跡の長さ＝残る時間（秒）。突進速度×この値がおおよその長さ。</summary>
        [Range(.03f, .6f), Tooltip("軌跡の長さ＝残る時間（秒）。突進速度×この値がおおよその長さ。")]
        public float Length = .12f;
        /// <summary>軌跡を出す位置（Humanoidボーン）。刀先の線は Tip で別途出る。</summary>
        [Tooltip("軌跡を出す位置（Humanoidボーン）。刀先の線は Tip で別途出る。")]
        public HumanBodyBones[] Bones = { HumanBodyBones.Head, HumanBodyBones.LeftShoulder, HumanBodyBones.Hips, HumanBodyBones.RightLowerLeg };
        /// <summary>刀先（TrailBladeTip）にも軌跡を出す。</summary>
        [Tooltip("刀先（TrailBladeTip）にも軌跡を出す。")]
        public bool Tip = true;
    }

    /// <summary>突進中の本体残像を定義する。</summary>
    [System.Serializable]
    public sealed class AfterImage
    {
        /// <summary>この演出要素を有効にする。</summary>
        public bool Enabled = true;
        /// <summary>残像の色と初期の濃さ（CombatGlow の _Tint）。</summary>
        [Tooltip("残像の色と初期の濃さ（CombatGlow の _Tint）。")]
        public Color Color = new Color(.70f, .82f, .95f, .22f);
        /// <summary>突進中に残す残像の数。</summary>
        [Range(0, 6), Tooltip("突進中に残す残像の数。")]
        public int Count = 3;
        /// <summary>各残像が消えるまでの秒数。</summary>
        [Range(.03f, .6f), Tooltip("各残像が消えるまでの秒数。")]
        public float Duration = .16f;
    }

    /// <summary>刺突の直線VFXと命中時の強調表示を定義する。</summary>
    [System.Serializable]
    public sealed class Thrust
    {
        /// <summary>刺突の白銀の直線＋青鋼色の残光（KatsuroTimedVFX、一閃と同じ構成）。未設定なら出さない。</summary>
        [Tooltip("刺突の白銀の直線＋青鋼色の残光（KatsuroTimedVFX、一閃と同じ構成）。未設定なら出さない。")]
        public KatsuroTimedVFX Prefab;
        /// <summary>色（各Materialの色に乗算）。青鋼色の残光は光の面に出る。</summary>
        [ColorUsage(true, true), Tooltip("色（各Materialの色に乗算）。青鋼色の残光は光の面に出る。")]
        public Color Color = new Color(.92f, .97f, 1f, 1f);
        /// <summary>明るさの倍率。</summary>
        [Range(0f, 3f), Tooltip("明るさの倍率。")]
        public float Intensity = 1f;
        /// <summary>直線の長さ（倍率。基準長2m）。</summary>
        [Range(.2f, 2f), Tooltip("直線の長さ（倍率。基準長2m）。")]
        public float Length = .85f;
        /// <summary>刀先から後ろへずらす量（m）。正で線が体側に寄る。</summary>
        [Range(-1f, 1f), Tooltip("刀先から後ろへずらす量（m）。正で線が体側に寄る。")]
        public float BackOffset = .45f;
        /// <summary>出てから消えるまでの秒数（実時間）。</summary>
        [Range(.05f, 1f), Tooltip("出てから消えるまでの秒数（実時間）。")]
        public float Duration = .26f;
        /// <summary>出す瞬間（RushAttack の元Clip秒）。V08では右腕が伸び切る直前 0.62 秒。</summary>
        [Range(.3f, 1f), Tooltip("出す瞬間（RushAttack の元Clip秒）。V08では右腕が伸び切る直前 0.62 秒。")]
        public float ClipTime = .62f;
        /// <summary>命中時、HitStop の間だけ保持する明るさ。</summary>
        [Range(1f, 3f), Tooltip("命中時、HitStop の間だけ保持する明るさ。")]
        public float HitHoldBoost = 1.35f;
    }

    /// <summary>構え中に刀先へ出す収束光の設定。</summary>
    public TipGlint Glint = new TipGlint();
    /// <summary>後脚の蹴り出しに合わせて再生する砂埃の設定。</summary>
    public Dust GroundDust = new Dust();
    /// <summary>突進中の身体・刀先に沿う風の軌跡の設定。</summary>
    public Wind WindTrail = new Wind();
    /// <summary>突進中に残す本体の残像の設定。</summary>
    public AfterImage AfterImages = new AfterImage();
    /// <summary>刺突の白銀の主線・残光・命中時の保持の設定。</summary>
    public Thrust ThrustLine = new Thrust();
    /// <summary>風の軌跡に使う Material（Katsuro/VFXParticle の加算）。</summary>
    [Tooltip("風の軌跡に使う Material（Katsuro/VFXParticle の加算）。")]
    public Material WindMaterial;
    /// <summary>砂埃に使う Material（Katsuro/VFXParticle のアルファ）。</summary>
    [Tooltip("砂埃に使う Material（Katsuro/VFXParticle のアルファ）。")]
    public Material DustMaterial;
}
