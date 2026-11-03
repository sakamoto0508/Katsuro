using UnityEngine;

/// <summary>タイトル画面の風。強弱と向きをPerlinノイズで緩やかに揺らし、SmoothDampで補間して急な反転を防ぐ。</summary>
public sealed class TitleWind : MonoBehaviour
{
    /// <summary>基本の風向き（度）。0で右向き、正の値で上向きに傾く。</summary>
    [UnityEngine.Tooltip("基本の風向き（度）。0で右向き、正の値で上向きに傾く。")]
    [SerializeField] private float _baseAngle = 6f;
    /// <summary>基本の風向きから振れる最大角度（度）。</summary>
    [UnityEngine.Tooltip("基本の風向きから振れる最大角度（度）。")]
    [SerializeField, Range(0f, 180f)] private float _angleRange = 25f;
    /// <summary>突風がないときの風速（Canvas基準解像度のpx/秒）。</summary>
    [UnityEngine.Tooltip("突風がないときの風速（Canvas基準解像度のpx/秒）。")]
    [SerializeField, Min(0f)] private float _baseStrength = 25f;
    /// <summary>突風で上乗せされる最大風速（Canvas基準解像度のpx/秒）。</summary>
    [UnityEngine.Tooltip("突風で上乗せされる最大風速（Canvas基準解像度のpx/秒）。")]
    [SerializeField, Min(0f)] private float _gustStrength = 45f;
    /// <summary>強弱と向きが変化する速さ。小さいほどゆったり変わる。</summary>
    [UnityEngine.Tooltip("強弱と向きが変化する速さ。小さいほどゆったり変わる。")]
    [SerializeField, Min(0f)] private float _variationSpeed = 0.08f;
    /// <summary>目標の風へ追従する時間（秒）。大きいほど変化が滑らかになる。</summary>
    [UnityEngine.Tooltip("目標の風へ追従する時間（秒）。大きいほど変化が滑らかになる。")]
    [SerializeField, Min(0.01f)] private float _smoothTime = 1.8f;
    /// <summary>ノイズの開始位置。変えると同じ設定でも別の吹き方になる。</summary>
    [UnityEngine.Tooltip("ノイズの開始位置。変えると同じ設定でも別の吹き方になる。")]
    [SerializeField] private int _seed = 17;

    /// <summary>補間済みの現在の風ベクトル（px/秒）。</summary>
    public Vector2 Current { get; private set; }
    /// <summary>現在の突風の強さ（0〜1）。</summary>
    public float Gust { get; private set; }

    private Vector2 _velocity;
    private float _noiseTime;
    private bool _initialized;

    /// <summary>開始直後から目標の風が吹いている状態にして、最初の数秒の加速を見せない。</summary>
    private void Awake() => Init();

    /// <summary>現在の風を目標値で初期化する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        Current = Evaluate(out float gust);
        Gust = gust;
    }

    /// <summary>風のノイズを進め、目標へ補間する。</summary>
    public void Tick(float deltaTime)
    {
        if (!_initialized) Init();
        if (deltaTime <= 0f) return;
        _noiseTime += deltaTime * _variationSpeed;
        var target = Evaluate(out float gust);
        Current = Vector2.SmoothDamp(Current, target, ref _velocity, _smoothTime, Mathf.Infinity, deltaTime);
        Gust = Mathf.MoveTowards(Gust, gust, deltaTime / _smoothTime);
    }

    /// <summary>現在のノイズ位置から目標の風を求める。</summary>
    /// <returns>目標の風ベクトル（px/秒）。</returns>
    private Vector2 Evaluate(out float gust)
    {
        // 強弱と向きを別のノイズ軸で揺らし、強まる瞬間と向きが変わる瞬間をずらす
        gust = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.PerlinNoise(_seed * 0.37f, _noiseTime)));
        float swing = Mathf.Clamp01(Mathf.PerlinNoise(_noiseTime * 0.6f, _seed * 0.71f + 5.3f)) * 2f - 1f;
        float angle = (_baseAngle + swing * _angleRange) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (_baseStrength + _gustStrength * gust);
    }
}
