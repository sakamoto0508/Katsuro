using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 火の粉と灰を1つのUIメッシュとして描くGraphic。粒子は固定長の配列で再利用し、Instantiate/Destroyを行わない。
/// 粒ごとに速度・大きさ・寿命・透明度をばらつかせ、共有の風とPerlinの乱流で漂わせる。
/// </summary>
public sealed class TitleEmberEffect : MaskableGraphic, ITitleAmbientElement
{
    /// <summary>1粒の状態。配列内で値として保持し、生成・破棄の代わりに上書きする。</summary>
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Life;
        public float Size;
        public float Rise;
        public float Alpha;
        public float Seed;
        public bool IsAsh;
    }

    [Header("Texture")]
    /// <summary>粒に使うテクスチャ。未設定なら中心が明るい柔らかな点を生成する。</summary>
    [UnityEngine.Tooltip("粒に使うテクスチャ。未設定なら中心が明るい柔らかな点を生成する。")]
    [SerializeField] private Texture _particleTexture;

    [Header("Emission")]
    /// <summary>同時に存在できる最大粒数。描画頂点数の上限にもなる。</summary>
    [UnityEngine.Tooltip("同時に存在できる最大粒数。描画頂点数の上限にもなる。")]
    [SerializeField, Range(1, 256)] private int _maxParticles = 60;
    /// <summary>1秒あたりの発生数。</summary>
    [UnityEngine.Tooltip("1秒あたりの発生数。")]
    [SerializeField, Min(0f)] private float _emissionRate = 7f;
    /// <summary>発生範囲（この矩形に対する0〜1の比率、原点は左下）。</summary>
    [UnityEngine.Tooltip("発生範囲（この矩形に対する0〜1の比率、原点は左下）。")]
    [SerializeField] private Rect _spawnArea = new Rect(0f, 0f, 1f, 0.4f);
    /// <summary>風上の画面端から流れ込ませる割合。画面外から入ってくる粒で風の流れを見せる。</summary>
    [UnityEngine.Tooltip("風上の画面端から流れ込ませる割合。画面外から入ってくる粒で風の流れを見せる。")]
    [SerializeField, Range(0f, 1f)] private float _edgeSpawnRatio = 0.3f;
    /// <summary>開始時に事前に進めておく時間（秒）。最初から粒が漂っている状態にする。</summary>
    [UnityEngine.Tooltip("開始時に事前に進めておく時間（秒）。最初から粒が漂っている状態にする。")]
    [SerializeField, Range(0f, 15f)] private float _prewarmTime = 6f;
    /// <summary>発生する粒のうち灰にする割合。</summary>
    [UnityEngine.Tooltip("発生する粒のうち灰にする割合。")]
    [SerializeField, Range(0f, 1f)] private float _ashRatio = 0.25f;

    [Header("Particle")]
    /// <summary>寿命の範囲（秒）。</summary>
    [UnityEngine.Tooltip("寿命の範囲（秒）。")]
    [SerializeField] private Vector2 _lifetime = new Vector2(4f, 9f);
    /// <summary>火の粉の大きさの範囲（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("火の粉の大きさの範囲（Canvas基準解像度のpx）。")]
    [SerializeField] private Vector2 _emberSize = new Vector2(4f, 9f);
    /// <summary>灰の大きさの範囲（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("灰の大きさの範囲（Canvas基準解像度のpx）。")]
    [SerializeField] private Vector2 _ashSize = new Vector2(5f, 11f);
    /// <summary>火の粉が上昇する速さの範囲（px/秒）。灰はこの半分になる。</summary>
    [UnityEngine.Tooltip("火の粉が上昇する速さの範囲（px/秒）。灰はこの半分になる。")]
    [SerializeField] private Vector2 _riseSpeed = new Vector2(12f, 40f);
    /// <summary>移動方向へ伸ばす量。速い粒ほど尾を引いて見える。</summary>
    [UnityEngine.Tooltip("移動方向へ伸ばす量。速い粒ほど尾を引いて見える。")]
    [SerializeField, Range(0f, 0.1f)] private float _stretch = 0.02f;

    [Header("Motion")]
    /// <summary>火の粉が風に流される割合。</summary>
    [UnityEngine.Tooltip("火の粉が風に流される割合。")]
    [SerializeField, Range(0f, 3f)] private float _windInfluence = 1f;
    /// <summary>灰が風に流される割合。軽いため火の粉より大きくする。</summary>
    [UnityEngine.Tooltip("灰が風に流される割合。軽いため火の粉より大きくする。")]
    [SerializeField, Range(0f, 3f)] private float _ashWindInfluence = 1.4f;
    /// <summary>不規則に揺れる乱流の強さ（px/秒）。</summary>
    [UnityEngine.Tooltip("不規則に揺れる乱流の強さ（px/秒）。")]
    [SerializeField, Min(0f)] private float _turbulence = 22f;
    /// <summary>乱流の向きが変わる速さ。</summary>
    [UnityEngine.Tooltip("乱流の向きが変わる速さ。")]
    [SerializeField, Min(0f)] private float _turbulenceFrequency = 0.35f;
    /// <summary>目標速度へ追従する速さ。小さいほど慣性が残りふわりと動く。</summary>
    [UnityEngine.Tooltip("目標速度へ追従する速さ。小さいほど慣性が残りふわりと動く。")]
    [SerializeField, Min(0.01f)] private float _responsiveness = 1.2f;

    [Header("Color")]
    /// <summary>発生直後の火の粉の色。</summary>
    [UnityEngine.Tooltip("発生直後の火の粉の色。")]
    [SerializeField] private Color _hotColor = new Color(1f, 0.55f, 0.18f, 1f);
    /// <summary>燃え尽きる直前の火の粉の色。</summary>
    [UnityEngine.Tooltip("燃え尽きる直前の火の粉の色。")]
    [SerializeField] private Color _coolColor = new Color(0.65f, 0.08f, 0.03f, 1f);
    /// <summary>灰の色。</summary>
    [UnityEngine.Tooltip("灰の色。")]
    [SerializeField] private Color _ashColor = new Color(0.55f, 0.52f, 0.5f, 0.55f);
    /// <summary>粒ごとの不透明度の範囲。</summary>
    [UnityEngine.Tooltip("粒ごとの不透明度の範囲。")]
    [SerializeField] private Vector2 _alphaRange = new Vector2(0.35f, 0.9f);
    /// <summary>寿命に対するフェードインの割合。</summary>
    [UnityEngine.Tooltip("寿命に対するフェードインの割合。")]
    [SerializeField, Range(0.01f, 0.5f)] private float _fadeIn = 0.12f;
    /// <summary>寿命に対するフェードアウトの割合。</summary>
    [UnityEngine.Tooltip("寿命に対するフェードアウトの割合。")]
    [SerializeField, Range(0.01f, 0.9f)] private float _fadeOut = 0.4f;
    /// <summary>火の粉の明滅の深さ。</summary>
    [UnityEngine.Tooltip("火の粉の明滅の深さ。")]
    [SerializeField, Range(0f, 1f)] private float _flickerAmount = 0.4f;
    /// <summary>火の粉の明滅の速さ。</summary>
    [UnityEngine.Tooltip("火の粉の明滅の速さ。")]
    [SerializeField, Min(0f)] private float _flickerSpeed = 5f;

    private static Texture2D s_dotTexture;
    private Particle[] _particles;
    private int _activeCount;
    private float _emitAccumulator;
    private float _intensity;
    private bool _wasVisible;
    private bool _prewarmed;

    /// <summary>未設定なら生成した柔らかな点を使う。</summary>
    public override Texture mainTexture => _particleTexture != null ? _particleTexture : GetDotTexture();
    /// <summary>現在生存している粒の数。</summary>
    public int ActiveCount => _activeCount;
    /// <summary>1秒あたりの発生数。検証時の一時的な強調に使う。</summary>
    public float EmissionRate { get => _emissionRate; set => _emissionRate = Mathf.Max(0f, value); }
    /// <summary>粒の大きさの倍率。検証時の一時的な強調に使う。</summary>
    public float SizeScale { get; set; } = 1f;

#if UNITY_EDITOR
    /// <summary>入力を遮らないよう、追加時から当たり判定を外す。</summary>
    protected override void Reset()
    {
        base.Reset();
        raycastTarget = false;
    }
#endif

    /// <summary>実行中も入力を遮らないよう当たり判定を外す。</summary>
    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    /// <summary>粒を発生・移動させ、寿命や画面外に出た粒を末尾と入れ替えて再利用する。</summary>
    public void Tick(in TitleAmbientContext context)
    {
        if (!isActiveAndEnabled) return;
        EnsurePool();
        _intensity = context.Intensity;
        if (!_prewarmed)
        {
            _prewarmed = true;
            // フェードイン前に粒を散らしておき、全粒が同じ高さから湧く不自然さを避ける
            const float step = 1f / 30f;
            for (float t = 0f; t < _prewarmTime; t += step) Simulate(step, t, context.Wind, 1f);
        }
        Simulate(context.DeltaTime, context.Time, context.Wind, context.Intensity);
        // 粒が消えた直後の1回だけ空メッシュを送り、以降は再構築を止める
        if (_activeCount > 0 || _wasVisible) SetVerticesDirty();
        _wasVisible = _activeCount > 0;
    }

    /// <summary>発生と移動を1ステップ進める。</summary>
    private void Simulate(float deltaTime, float time, Vector2 wind, float emissionScale)
    {
        if (deltaTime <= 0f) return;
        var rect = rectTransform.rect;
        _emitAccumulator += _emissionRate * emissionScale * deltaTime;
        while (_emitAccumulator >= 1f)
        {
            _emitAccumulator -= 1f;
            if (_activeCount < _particles.Length) Spawn(rect, wind);
        }

        float blend = 1f - Mathf.Exp(-_responsiveness * deltaTime);
        float margin = _emberSize.y + _ashSize.y + 40f;
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            ref var p = ref _particles[i];
            p.Age += deltaTime;
            if (p.Age >= p.Life || p.Position.y > rect.yMax + margin
                || p.Position.x < rect.xMin - margin * 4f || p.Position.x > rect.xMax + margin * 4f)
            {
                _particles[i] = _particles[--_activeCount];
                continue;
            }
            float noiseTime = time * _turbulenceFrequency;
            var turbulence = new Vector2(
                Mathf.PerlinNoise(p.Seed, noiseTime) * 2f - 1f,
                Mathf.PerlinNoise(noiseTime + 7.1f, p.Seed) * 2f - 1f) * _turbulence;
            var target = new Vector2(0f, p.Rise) + wind * (p.IsAsh ? _ashWindInfluence : _windInfluence) + turbulence;
            p.Velocity = Vector2.Lerp(p.Velocity, target, blend);
            p.Position += p.Velocity * deltaTime;
        }
    }

    /// <summary>空きスロットに新しい粒を書き込む。</summary>
    private void Spawn(Rect rect, Vector2 wind)
    {
        ref var p = ref _particles[_activeCount++];
        p.IsAsh = Random.value < _ashRatio;
        bool fromEdge = Mathf.Abs(wind.x) > 1f && Random.value < _edgeSpawnRatio;
        if (fromEdge)
        {
            // 風上側の画面外から入れ、画面端で突然湧いたように見せない
            float x = wind.x > 0f ? rect.xMin - 20f : rect.xMax + 20f;
            p.Position = new Vector2(x, Mathf.Lerp(rect.yMin, rect.yMax, Random.Range(0.05f, 0.75f)));
        }
        else
        {
            p.Position = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, _spawnArea.x + _spawnArea.width * Random.value),
                Mathf.Lerp(rect.yMin, rect.yMax, _spawnArea.y + _spawnArea.height * Random.value));
        }
        p.Life = Random.Range(_lifetime.x, _lifetime.y) * (p.IsAsh ? 1.3f : 1f);
        p.Size = p.IsAsh ? Random.Range(_ashSize.x, _ashSize.y) : Random.Range(_emberSize.x, _emberSize.y);
        p.Rise = Random.Range(_riseSpeed.x, _riseSpeed.y) * (p.IsAsh ? 0.5f : 1f);
        p.Alpha = Random.Range(_alphaRange.x, _alphaRange.y);
        p.Seed = Random.Range(0f, 1000f);
        p.Age = 0f;
        p.Velocity = new Vector2(0f, p.Rise) + wind * (p.IsAsh ? _ashWindInfluence : _windInfluence);
    }

    /// <summary>最大粒数が変わったときだけ配列を作り直す。</summary>
    private void EnsurePool()
    {
        int capacity = Mathf.Max(1, _maxParticles);
        if (_particles != null && _particles.Length == capacity) return;
        System.Array.Resize(ref _particles, capacity);
        _activeCount = Mathf.Min(_activeCount, capacity);
    }

    /// <summary>生存中の粒を速度方向へ伸ばした四角形として1メッシュにまとめる。</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_particles == null) return;
        var tint = color;
        for (int i = 0; i < _activeCount; i++)
        {
            ref var p = ref _particles[i];
            float life = p.Age / p.Life;
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(life / _fadeIn, (1f - life) / _fadeOut));
            var c = p.IsAsh ? _ashColor : Color.Lerp(_hotColor, _coolColor, life);
            float flicker = p.IsAsh ? 1f : 1f - _flickerAmount * Mathf.PerlinNoise(p.Seed * 3.1f, p.Age * _flickerSpeed);
            c.r *= tint.r;
            c.g *= tint.g;
            c.b *= tint.b;
            c.a *= tint.a * p.Alpha * fade * flicker * _intensity;
            if (c.a <= 0.003f) continue;

            float speed = p.Velocity.magnitude;
            var dir = speed > 0.001f ? p.Velocity / speed : Vector2.up;
            float size = p.Size * SizeScale;
            var along = dir * (size * (1f + speed * _stretch) * 0.5f);
            var side = new Vector2(-dir.y, dir.x) * (size * 0.5f);
            Color32 c32 = c;
            int index = vh.currentVertCount;
            vh.AddVert(p.Position - along - side, c32, new Vector4(0f, 0f, 0f, 0f));
            vh.AddVert(p.Position - along + side, c32, new Vector4(0f, 1f, 0f, 0f));
            vh.AddVert(p.Position + along + side, c32, new Vector4(1f, 1f, 0f, 0f));
            vh.AddVert(p.Position + along - side, c32, new Vector4(1f, 0f, 0f, 0f));
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }

    /// <summary>中心の芯と淡い光暈を持つ点テクスチャを一度だけ生成して共有する。</summary>
    /// <returns>生成済みの点テクスチャ。</returns>
    private static Texture2D GetDotTexture()
    {
        if (s_dotTexture != null) return s_dotTexture;
        const int size = 32;
        s_dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TitleEmberDot",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                // 芯が細すぎると数pxの粒ではほぼ見えないため、芯を広めにして光暈を少し強める
                float core = Mathf.Exp(-r * r * 7f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 2f) * 0.45f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(core + halo) * 255f));
            }
        }
        s_dotTexture.SetPixels32(pixels);
        s_dotTexture.Apply(false, true);
        return s_dotTexture;
    }
}
