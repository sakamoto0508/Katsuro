using UnityEngine;

/// <summary>
/// 腰から下の外套を風に合わせて揺らす。帯で固定し、裾に向かうほど大きく、遅れて動かして重い布に見せる。
/// 風の向きと突風はTitleWindの値をそのまま使い、霧・火の粉と同じ風で揺れる。
/// </summary>
public sealed class TitleClothSway : TitleImageWarp
{
    /// <summary>風がない時の裾のゆらぎ幅（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("風がない時の裾のゆらぎ幅（Canvas基準解像度のpx）。")]
    [SerializeField, Range(0f, 8f)] private float _swayAmplitude = 1.2f;
    /// <summary>風速1px/秒あたりに裾が流される量（px）。大きいほど風に敏感になる。</summary>
    [UnityEngine.Tooltip("風速1px/秒あたりに裾が流される量（px）。大きいほど風に敏感になる。")]
    [SerializeField, Range(0f, 0.2f)] private float _windInfluence = 0.03f;
    /// <summary>突風時にゆらぎ幅へ上乗せする割合。</summary>
    [UnityEngine.Tooltip("突風時にゆらぎ幅へ上乗せする割合。")]
    [SerializeField, Range(0f, 2f)] private float _gustBoost = 0.8f;
    /// <summary>裾が流される量の上限（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("裾が流される量の上限（Canvas基準解像度のpx）。")]
    [SerializeField, Range(0f, 10f)] private float _maxOffset = 3.5f;
    /// <summary>ゆらぎの速さ。小さいほどゆったり揺れる。</summary>
    [UnityEngine.Tooltip("ゆらぎの速さ。小さいほどゆったり揺れる。")]
    [SerializeField, Min(0f)] private float _swaySpeed = 0.35f;
    /// <summary>帯から裾まで揺れが伝わる遅れ（秒）。</summary>
    [UnityEngine.Tooltip("帯から裾まで揺れが伝わる遅れ（秒）。")]
    [SerializeField, Range(0f, 2f)] private float _waveDelay = 0.6f;
    /// <summary>風の変化に布が追従する時間（秒）。大きいほど重い布になる。</summary>
    [UnityEngine.Tooltip("風の変化に布が追従する時間（秒）。大きいほど重い布になる。")]
    [SerializeField, Min(0.01f)] private float _weight = 1.4f;
    /// <summary>裾の高さ（範囲比、0=範囲の下端、1=帯）。ここで揺れが最大になり、下端で0に戻る。</summary>
    [UnityEngine.Tooltip("裾の高さ（範囲比、0=範囲の下端、1=帯）。ここで揺れが最大になり、下端で0に戻る。")]
    [SerializeField, Range(0.02f, 0.9f)] private float _hemHeight = 0.14f;
    /// <summary>左右の縁で動きを0へ戻す幅（範囲比）。手や柄を引きずらないよう広めにする。</summary>
    [UnityEngine.Tooltip("左右の縁で動きを0へ戻す幅（範囲比）。手や柄を引きずらないよう広めにする。")]
    [SerializeField, Range(0.01f, 0.5f)] private float _sideFade = 0.25f;
    /// <summary>流された裾が持ち上がる割合。</summary>
    [UnityEngine.Tooltip("流された裾が持ち上がる割合。")]
    [SerializeField, Range(0f, 0.5f)] private float _lift = 0.15f;
    /// <summary>ノイズの開始位置。</summary>
    [UnityEngine.Tooltip("ノイズの開始位置。")]
    [SerializeField] private int _seed = 23;

    private float _time, _windOffset, _windVelocity, _gust;

    /// <summary>風による流れを重さに合わせて補間し、ゆらぎの時間を進める。</summary>
    protected override void Advance(in TitleAmbientContext context)
    {
        _time += context.DeltaTime;
        float target = Mathf.Clamp(context.Wind.x * _windInfluence, -_maxOffset, _maxOffset);
        _windOffset = Mathf.SmoothDamp(_windOffset, target, ref _windVelocity, _weight, Mathf.Infinity, context.DeltaTime);
        _gust = context.Gust;
    }

    /// <summary>裾ほど大きく、遅れて伝わる横方向のずれ。</summary>
    protected override Vector2 Offset(float u, float v)
    {
        // 帯（v=1）から裾までで立ち上がり、裾より下は脚を引きずらないよう0へ戻す
        float vertical = v >= _hemHeight
            ? Mathf.Pow(Mathf.Clamp01((1f - v) / (1f - _hemHeight)), 1.6f)
            : Mathf.SmoothStep(0f, 1f, v / _hemHeight);
        float weight = vertical * EdgeWeight(u, _sideFade) * Intensity;
        if (weight <= 0f) return Vector2.zero;
        float lag = (1f - v) * _waveDelay;
        float noise = Mathf.PerlinNoise(_seed * 0.17f, (_time - lag) * _swaySpeed) * 2f - 1f;
        float x = Mathf.Clamp(_windOffset + noise * _swayAmplitude * (1f + _gust * _gustBoost), -_maxOffset, _maxOffset) * weight;
        return new Vector2(x, Mathf.Abs(x) * _lift);
    }
}
