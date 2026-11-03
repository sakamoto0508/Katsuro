using UnityEngine;

/// <summary>
/// 侍の上半身を呼吸に合わせてわずかに持ち上げる。帯から下は動かさず、肩で最大、範囲の上下左右の縁で0になる。
/// 周期と深さをノイズで少しずつ揺らし、同じ呼吸の繰り返しに見えないようにする。
/// </summary>
public sealed class TitleCharacterBreath : TitleImageWarp
{
    /// <summary>息を吸い切った時に肩が上がる量（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("息を吸い切った時に肩が上がる量（Canvas基準解像度のpx）。")]
    [SerializeField, Range(0f, 6f)] private float _lift = 1.4f;
    /// <summary>息を吸った時に肩が左右へ広がる量（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("息を吸った時に肩が左右へ広がる量（Canvas基準解像度のpx）。")]
    [SerializeField, Range(0f, 3f)] private float _widen = 0.4f;
    /// <summary>1回の呼吸の平均時間（秒）。</summary>
    [UnityEngine.Tooltip("1回の呼吸の平均時間（秒）。")]
    [SerializeField, Min(0.5f)] private float _period = 4.6f;
    /// <summary>呼吸の周期が揺らぐ割合。0で一定の周期になる。</summary>
    [UnityEngine.Tooltip("呼吸の周期が揺らぐ割合。0で一定の周期になる。")]
    [SerializeField, Range(0f, 0.5f)] private float _periodJitter = 0.18f;
    /// <summary>呼吸の深さが揺らぐ割合。0で毎回同じ深さになる。</summary>
    [UnityEngine.Tooltip("呼吸の深さが揺らぐ割合。0で毎回同じ深さになる。")]
    [SerializeField, Range(0f, 0.6f)] private float _depthJitter = 0.3f;
    /// <summary>動きが最大になる高さ（範囲比、0=帯、1=範囲の上端）。肩の位置に合わせる。</summary>
    [UnityEngine.Tooltip("動きが最大になる高さ（範囲比、0=帯、1=範囲の上端）。肩の位置に合わせる。")]
    [SerializeField, Range(0.1f, 0.95f)] private float _shoulderHeight = 0.62f;
    /// <summary>範囲の上端で動きを0へ戻す幅（範囲比）。頭巾の上の背景を引きずらない程度に狭くする。</summary>
    [UnityEngine.Tooltip("範囲の上端で動きを0へ戻す幅（範囲比）。頭巾の上の背景を引きずらない程度に狭くする。")]
    [SerializeField, Range(0.02f, 0.5f)] private float _topFade = 0.08f;
    /// <summary>左右の縁で動きを0へ戻す幅（範囲比）。</summary>
    [UnityEngine.Tooltip("左右の縁で動きを0へ戻す幅（範囲比）。")]
    [SerializeField, Range(0.01f, 0.5f)] private float _sideFade = 0.1f;
    /// <summary>肩より上で動かす頭巾の中心（範囲比、左右）。</summary>
    [UnityEngine.Tooltip("肩より上で動かす頭巾の中心（範囲比、左右）。")]
    [SerializeField, Range(0f, 1f)] private float _headCenter = 0.49f;
    /// <summary>肩より上で動かす頭巾の幅（範囲比）。外側の背景（塔や雨）は呼吸で動かさない。</summary>
    [UnityEngine.Tooltip("肩より上で動かす頭巾の幅（範囲比）。外側の背景（塔や雨）は呼吸で動かさない。")]
    [SerializeField, Range(0.05f, 1f)] private float _headWidth = 0.44f;
    /// <summary>ノイズの開始位置。</summary>
    [UnityEngine.Tooltip("ノイズの開始位置。")]
    [SerializeField] private int _seed = 11;

    private float _phase, _breath, _time;

    /// <summary>周期を揺らしながら呼吸の位相を進め、吸う・吐くの深さを求める。</summary>
    protected override void Advance(in TitleAmbientContext context)
    {
        _time += context.DeltaTime;
        float speed = 1f + _periodJitter * (Mathf.PerlinNoise(_seed * 0.31f, _time * 0.05f) * 2f - 1f);
        _phase = Mathf.Repeat(_phase + context.DeltaTime / _period * speed, 1f);
        float depth = 1f + _depthJitter * (Mathf.PerlinNoise(_time * 0.04f, _seed * 0.53f) * 2f - 1f);
        // 吸う側をゆっくり、吐く側を少し早くして機械的な正弦波に見せない
        float shaped = Mathf.Pow(0.5f - 0.5f * Mathf.Cos(_phase * Mathf.PI * 2f), 1.3f);
        _breath = shaped * depth * Intensity;
    }

    /// <summary>肩で最大となる上向きのずれと、わずかな左右への広がり。肩より上は頭巾の幅だけ動かす。</summary>
    protected override Vector2 Offset(float u, float v)
    {
        float vertical = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / _shoulderHeight))
            * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - v) / _topFade));
        float half = _headWidth * 0.5f;
        float head = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((half - Mathf.Abs(u - _headCenter)) / (half * 0.5f)));
        float aboveShoulder = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - _shoulderHeight) / 0.12f));
        float weight = vertical * Mathf.Lerp(EdgeWeight(u, _sideFade), head, aboveShoulder) * _breath;
        return new Vector2((u - 0.5f) * 2f * _widen * weight, _lift * weight);
    }
}
