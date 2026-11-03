using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RawImageのUVを流して霧を動かす1レイヤー。継ぎ目が出ないよう周期的なノイズテクスチャを生成し、
/// 高さ方向の帯で濃度を絞って人物や鳥居を覆わないようにする。遠景・近景はレイヤーを分けて速度を変える。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public sealed class TitleFogLayer : MonoBehaviour, ITitleAmbientElement
{
    [Header("Texture")]
    /// <summary>使用する霧テクスチャ。未設定なら継ぎ目のないノイズを生成する。差し替える場合は横方向にタイル可能なものを使う。</summary>
    [UnityEngine.Tooltip("使用する霧テクスチャ。未設定なら継ぎ目のないノイズを生成する。差し替える場合は横方向にタイル可能なものを使う。")]
    [SerializeField] private Texture _overrideTexture;
    /// <summary>非再生中もSceneビューに霧を表示するか。RawImageの値を書き換えるため、確認後はオフに戻す。</summary>
    [UnityEngine.Tooltip("非再生中もSceneビューに霧を表示するか。RawImageの値を書き換えるため、確認後はオフに戻す。")]
    [SerializeField] private bool _previewInEditMode;
    /// <summary>生成するノイズテクスチャの一辺（px）。霧は拡大表示するため小さくてよい。</summary>
    [UnityEngine.Tooltip("生成するノイズテクスチャの一辺（px）。霧は拡大表示するため小さくてよい。")]
    [SerializeField, Range(64, 512)] private int _textureSize = 256;
    /// <summary>テクスチャ1枚あたりの大きな雲の数。大きいほど細かい霧になる。</summary>
    [UnityEngine.Tooltip("テクスチャ1枚あたりの大きな雲の数。大きいほど細かい霧になる。")]
    [SerializeField, Range(1, 16)] private int _noisePeriod = 3;
    /// <summary>重ねるノイズの段数。多いほど縁が細かくなる。</summary>
    [UnityEngine.Tooltip("重ねるノイズの段数。多いほど縁が細かくなる。")]
    [SerializeField, Range(1, 6)] private int _octaves = 4;
    /// <summary>濃淡の強さ。大きいほど薄い部分が抜け、塊感が出る。</summary>
    [UnityEngine.Tooltip("濃淡の強さ。大きいほど薄い部分が抜け、塊感が出る。")]
    [SerializeField, Range(0.5f, 4f)] private float _contrast = 1.6f;
    /// <summary>霧が最も濃い高さ（0=画面下端、1=上端）。</summary>
    [UnityEngine.Tooltip("霧が最も濃い高さ（0=画面下端、1=上端）。")]
    [SerializeField, Range(0f, 1f)] private float _bandCenter = 0.25f;
    /// <summary>霧の帯の半分の厚さ（画面高さ比）。この外側は透明になる。</summary>
    [UnityEngine.Tooltip("霧の帯の半分の厚さ（画面高さ比）。この外側は透明になる。")]
    [SerializeField, Range(0.02f, 1f)] private float _bandHalfHeight = 0.3f;
    /// <summary>ノイズの乱数シード。レイヤーごとに変えて模様の重なりを避ける。</summary>
    [UnityEngine.Tooltip("ノイズの乱数シード。レイヤーごとに変えて模様の重なりを避ける。")]
    [SerializeField] private int _seed = 1;

    [Header("Motion")]
    /// <summary>画面幅に並べるテクスチャの枚数。大きいほど模様が小さく見える。</summary>
    [UnityEngine.Tooltip("画面幅に並べるテクスチャの枚数。大きいほど模様が小さく見える。")]
    [SerializeField, Range(0.25f, 4f)] private float _tiling = 1.2f;
    /// <summary>風がないときに霧が流れる速さ（Canvas基準解像度のpx/秒、正で右へ）。</summary>
    [UnityEngine.Tooltip("風がないときに霧が流れる速さ（Canvas基準解像度のpx/秒、正で右へ）。")]
    [SerializeField] private float _scrollSpeed = 8f;
    /// <summary>風速が霧の流れに加わる割合。遠景ほど小さくすると奥行きが出る。</summary>
    [UnityEngine.Tooltip("風速が霧の流れに加わる割合。遠景ほど小さくすると奥行きが出る。")]
    [SerializeField, Range(0f, 2f)] private float _windInfluence = 0.3f;
    /// <summary>霧が上下に揺らぐ幅（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("霧が上下に揺らぐ幅（Canvas基準解像度のpx）。")]
    [SerializeField, Min(0f)] private float _swayAmplitude = 12f;
    /// <summary>上下の揺らぎの速さ。</summary>
    [UnityEngine.Tooltip("上下の揺らぎの速さ。")]
    [SerializeField, Min(0f)] private float _swaySpeed = 0.05f;

    [Header("Color")]
    /// <summary>霧の色。透明度は _opacity で調整する。</summary>
    [UnityEngine.Tooltip("霧の色。透明度は _opacity で調整する。")]
    [SerializeField] private Color _tint = new Color(0.75f, 0.72f, 0.72f, 1f);
    /// <summary>霧の基本不透明度。人物や鳥居を隠さないよう低めにする。</summary>
    [UnityEngine.Tooltip("霧の基本不透明度。人物や鳥居を隠さないよう低めにする。")]
    [SerializeField, Range(0f, 1f)] private float _opacity = 0.3f;
    /// <summary>不透明度がゆっくり揺らぐ割合（基本不透明度に対する比）。</summary>
    [UnityEngine.Tooltip("不透明度がゆっくり揺らぐ割合（基本不透明度に対する比）。")]
    [SerializeField, Range(0f, 1f)] private float _opacityPulse = 0.25f;
    /// <summary>不透明度の揺らぎの速さ。</summary>
    [UnityEngine.Tooltip("不透明度の揺らぎの速さ。")]
    [SerializeField, Min(0f)] private float _pulseSpeed = 0.07f;
    /// <summary>突風時に増える不透明度の割合（基本不透明度に対する比）。</summary>
    [UnityEngine.Tooltip("突風時に増える不透明度の割合（基本不透明度に対する比）。")]
    [SerializeField, Range(0f, 1f)] private float _gustOpacityBoost = 0.2f;

    private RawImage _image;
    private RectTransform _rect;
    private Texture2D _generated;
    private float _offsetX;
    private float _localTime;
    private bool _dirty = true;

    /// <summary>RawImageを入力に干渉しない状態にし、テクスチャの再生成を予約する。</summary>
    private void OnEnable()
    {
        _image = GetComponent<RawImage>();
        _rect = (RectTransform)transform;
        _image.raycastTarget = false;
        _dirty = true;
    }

    /// <summary>現在RawImageに設定されている不透明度。</summary>
    public float CurrentOpacity => _image != null ? _image.color.a : 0f;
    /// <summary>霧のテクスチャがRawImageへ割り当て済みか。</summary>
    public bool HasTexture => _image != null && _image.texture != null;
    /// <summary>基本不透明度。検証時の一時的な強調に使う。</summary>
    public float Opacity { get => _opacity; set => _opacity = Mathf.Clamp01(value); }

    /// <summary>Inspectorの変更はOnValidate中に描画状態を触れないため、次のUpdateで反映する。</summary>
    private void OnValidate() => _dirty = true;

    /// <summary>設定変更があればテクスチャと静止表示を作り直す。編集中のプレビューも兼ねる。</summary>
    private void Update()
    {
        if (!_dirty) return;
        _dirty = false;
        Rebuild();
    }

    /// <summary>生成したテクスチャを破棄する。</summary>
    private void OnDestroy()
    {
        if (_generated == null) return;
        if (Application.isPlaying) Destroy(_generated);
        else DestroyImmediate(_generated);
        _generated = null;
    }

    /// <summary>風に合わせてUVを流し、上下の揺らぎと不透明度の変化を加える。</summary>
    public void Tick(in TitleAmbientContext context)
    {
        if (!isActiveAndEnabled || _image == null) return;
        float width = Mathf.Max(1f, _rect.rect.width);
        float height = Mathf.Max(1f, _rect.rect.height);
        _localTime += context.DeltaTime;
        // UVを増やすと模様は左へ動くため、右向きの風では減らす。精度を保つため0〜1に丸める
        float speed = _scrollSpeed + context.Wind.x * _windInfluence;
        _offsetX = Mathf.Repeat(_offsetX - speed / width * _tiling * context.DeltaTime, 1f);
        float sway = (Mathf.PerlinNoise(_seed * 0.131f, _localTime * _swaySpeed) * 2f - 1f) * _swayAmplitude;
        float pulse = (Mathf.PerlinNoise(_localTime * _pulseSpeed, _seed * 0.293f + 3.7f) * 2f - 1f) * _opacityPulse;
        float opacity = _opacity * (1f + pulse + context.Gust * _gustOpacityBoost) * context.Intensity;
        Apply(opacity, sway / height);
    }

    /// <summary>テクスチャを割り当て直し、停止中の見た目を整える。</summary>
    private void Rebuild()
    {
        if (_image == null) return;
        // 非再生中に毎回書き換えるとシーンが変更扱いになるため、プレビュー指定時だけ表示する
        if (!Application.isPlaying && !_previewInEditMode)
        {
            if (_image.texture != null && _image.texture == _generated) _image.texture = null;
            return;
        }
        Texture texture = _overrideTexture;
        if (texture == null)
        {
            GenerateTexture();
            texture = _generated;
        }
        _image.texture = texture;
        // 実行前のシーン表示でも霧の配置を確認できるよう、基本不透明度で表示しておく
        if (!Application.isPlaying) Apply(_opacity, 0f);
    }

    /// <summary>UVと色をRawImageへ反映する。</summary>
    private void Apply(float opacity, float swayUv)
    {
        _image.uvRect = new Rect(_offsetX, -swayUv, _tiling, 1f);
        var color = _tint;
        color.a = Mathf.Clamp01(opacity);
        _image.color = color;
    }

    /// <summary>横方向に継ぎ目のないfBmノイズを作り、高さの帯で濃度を絞る。</summary>
    private void GenerateTexture()
    {
        int size = _textureSize;
        if (_generated == null || _generated.width != size)
        {
            if (_generated != null) DestroyImmediate(_generated);
            _generated = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"{name}_FogNoise",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                // 横は流すため繰り返し、縦は帯の外を透明に保つため端で止める
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
            };
        }

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size;
            float band = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Abs(v - _bandCenter) / _bandHalfHeight);
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                // fBmは0.5付近に偏るため、よく出る範囲を0〜1へ広げてから濃淡を付ける
                float density = band <= 0f ? 0f : Mathf.Pow(Mathf.InverseLerp(0.28f, 0.78f, Fbm(u, v)), _contrast) * band;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(density) * 255f));
            }
        }
        _generated.SetPixels32(pixels);
        _generated.Apply(false, false);
    }

    /// <summary>周期を倍々にした値ノイズを重ね、0〜1の雲状の濃度を返す。</summary>
    /// <returns>0〜1の濃度。</returns>
    private float Fbm(float u, float v)
    {
        float sum = 0f, amplitude = 0.5f, total = 0f;
        int period = _noisePeriod;
        for (int i = 0; i < _octaves; i++)
        {
            sum += PeriodicNoise(u * period, v * period, period, _seed + i * 101) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            period *= 2;
        }
        return sum / total;
    }

    /// <summary>格子を周期で折り返す値ノイズ。端同士が同じ格子値を参照するため継ぎ目が出ない。</summary>
    /// <returns>0〜1のノイズ値。</returns>
    private static float PeriodicNoise(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        int xa = Wrap(x0, period), xb = Wrap(x0 + 1, period);
        int ya = Wrap(y0, period), yb = Wrap(y0 + 1, period);
        float u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
        float w = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
        float bottom = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), u);
        float top = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), u);
        return Mathf.Lerp(bottom, top, w);
    }

    /// <summary>負数も含めて0〜period-1へ折り返す。</summary>
    private static int Wrap(int value, int period) => ((value % period) + period) % period;

    /// <summary>格子点ごとの擬似乱数。</summary>
    /// <returns>0〜1の値。</returns>
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }
}
