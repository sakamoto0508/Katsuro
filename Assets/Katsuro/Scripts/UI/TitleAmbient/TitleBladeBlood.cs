using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 刀身に付いた血がゆっくり伝い、数秒おきに滴が膨らんでから落ちる演出。
/// 位置は自身の矩形（背景と同じ矩形）に対する割合で指定し、背景の刀に合わせる。
/// </summary>
public sealed class TitleBladeBlood : MaskableGraphic, ITitleAmbientElement
{
    /// <summary>血の粒の状態。</summary>
    private struct Bead
    {
        public float Distance, Speed, Size, Alpha;
    }

    /// <summary>落下中・成長中の滴の状態。</summary>
    private struct Drop
    {
        public Vector2 Position, Velocity;
        public float Age, GrowTime, Size, GroundY;
        public bool Falling, Active;
    }

    [Header("Blade")]
    /// <summary>血が伝い始める刀身上の位置（矩形比、左下原点）。柄元側に置く。</summary>
    [UnityEngine.Tooltip("血が伝い始める刀身上の位置（矩形比、左下原点）。柄元側に置く。")]
    [SerializeField] private Vector2 _bladeStart = new Vector2(0.606f, 0.35f);
    /// <summary>血が伝い終わる刀身上の位置（矩形比、左下原点）。切っ先側に置く。</summary>
    [UnityEngine.Tooltip("血が伝い終わる刀身上の位置（矩形比、左下原点）。切っ先側に置く。")]
    [SerializeField] private Vector2 _bladeEnd = new Vector2(0.678f, 0.234f);
    /// <summary>滴が膨らんで落ちる刀身の下側の位置（矩形比、左下原点）。</summary>
    [UnityEngine.Tooltip("滴が膨らんで落ちる刀身の下側の位置（矩形比、左下原点）。")]
    [SerializeField] private Vector2[] _dripPoints = { new Vector2(0.637f, 0.274f), new Vector2(0.657f, 0.249f) };
    /// <summary>滴が消える地面の高さ（矩形比、0=下端）。</summary>
    [UnityEngine.Tooltip("滴が消える地面の高さ（矩形比、0=下端）。")]
    [SerializeField, Range(0f, 1f)] private float _groundHeight = 0.177f;

    [Header("Trickle")]
    /// <summary>刀身を同時に伝う血の粒の数。</summary>
    [UnityEngine.Tooltip("刀身を同時に伝う血の粒の数。")]
    [SerializeField, Range(0, 6)] private int _beadCount = 2;
    /// <summary>血の粒が伝う速さの範囲（Canvas基準解像度のpx/秒）。</summary>
    [UnityEngine.Tooltip("血の粒が伝う速さの範囲（Canvas基準解像度のpx/秒）。")]
    [SerializeField] private Vector2 _beadSpeed = new Vector2(5f, 11f);
    /// <summary>血の粒の太さの範囲（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("血の粒の太さの範囲（Canvas基準解像度のpx）。")]
    [SerializeField] private Vector2 _beadSize = new Vector2(3.5f, 5.5f);
    /// <summary>血の粒を刀身方向へ伸ばす倍率。</summary>
    [UnityEngine.Tooltip("血の粒を刀身方向へ伸ばす倍率。")]
    [SerializeField, Range(1f, 6f)] private float _beadStretch = 2.6f;

    [Header("Drop")]
    /// <summary>滴が落ちる間隔の範囲（秒）。</summary>
    [UnityEngine.Tooltip("滴が落ちる間隔の範囲（秒）。")]
    [SerializeField] private Vector2 _dropInterval = new Vector2(3f, 6.5f);
    /// <summary>滴が膨らんでから離れるまでの時間の範囲（秒）。</summary>
    [UnityEngine.Tooltip("滴が膨らんでから離れるまでの時間の範囲（秒）。")]
    [SerializeField] private Vector2 _growTime = new Vector2(0.9f, 1.6f);
    /// <summary>離れる直前の滴の大きさの範囲（Canvas基準解像度のpx）。</summary>
    [UnityEngine.Tooltip("離れる直前の滴の大きさの範囲（Canvas基準解像度のpx）。")]
    [SerializeField] private Vector2 _dropSize = new Vector2(4.5f, 6.5f);
    /// <summary>落下の重力加速度（Canvas基準解像度のpx/秒²）。</summary>
    [UnityEngine.Tooltip("落下の重力加速度（Canvas基準解像度のpx/秒²）。")]
    [SerializeField, Min(0f)] private float _gravity = 1100f;
    /// <summary>落下中に速度方向へ伸ばす割合。</summary>
    [UnityEngine.Tooltip("落下中に速度方向へ伸ばす割合。")]
    [SerializeField, Range(0f, 0.02f)] private float _fallStretch = 0.006f;
    /// <summary>風が落下中の滴を流す割合。</summary>
    [UnityEngine.Tooltip("風が落下中の滴を流す割合。")]
    [SerializeField, Range(0f, 0.2f)] private float _windInfluence = 0.03f;

    [Header("Color")]
    /// <summary>血の色。背景の刀身の赤より暗めにして浮かせない。</summary>
    [UnityEngine.Tooltip("血の色。背景の刀身の赤より暗めにして浮かせない。")]
    [SerializeField] private Color _bloodColor = new Color(0.5f, 0.03f, 0.04f, 0.95f);
    /// <summary>粒と滴の上側に重ねる艶の色。</summary>
    [UnityEngine.Tooltip("粒と滴の上側に重ねる艶の色。")]
    [SerializeField] private Color _glossColor = new Color(1f, 0.42f, 0.36f, 0.5f);

    private static Texture2D s_dropTexture;
    private Bead[] _beads;
    private readonly Drop[] _drops = new Drop[4];
    private float _nextDrop, _intensity;
    private int _nextDripPoint;
    private bool _initialized;

    /// <summary>生成した縁の締まった丸を使う。</summary>
    public override Texture mainTexture => GetDropTexture();

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

    /// <summary>血の粒を刀身へ散らして置き、最初の滴の時刻を決める。</summary>
    private void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _beads = new Bead[_beadCount];
        float length = BladeLength();
        for (int i = 0; i < _beads.Length; i++) ResetBead(ref _beads[i], Random.Range(0f, length));
        _nextDrop = Random.Range(_dropInterval.x * 0.3f, _dropInterval.x);
    }

    /// <summary>粒と滴を進め、メッシュを作り直す。</summary>
    public void Tick(in TitleAmbientContext context)
    {
        if (!isActiveAndEnabled) return;
        Init();
        float deltaTime = context.DeltaTime;
        _intensity = context.Intensity;
        float length = BladeLength();
        for (int i = 0; i < _beads.Length; i++)
        {
            ref var bead = ref _beads[i];
            bead.Distance += bead.Speed * deltaTime;
            if (bead.Distance > length) ResetBead(ref bead, 0f);
        }
        _nextDrop -= deltaTime;
        if (_nextDrop <= 0f && _dripPoints.Length > 0)
        {
            _nextDrop = Random.Range(_dropInterval.x, _dropInterval.y);
            StartDrop(_dripPoints[_nextDripPoint]);
            // 同じ位置から続けて落ちないよう、滴の位置を順番に回す
            _nextDripPoint = (_nextDripPoint + 1) % _dripPoints.Length;
        }
        for (int i = 0; i < _drops.Length; i++)
        {
            ref var drop = ref _drops[i];
            if (!drop.Active) continue;
            drop.Age += deltaTime;
            if (!drop.Falling)
            {
                if (drop.Age >= drop.GrowTime) drop.Falling = true;
                continue;
            }
            drop.Velocity += new Vector2(context.Wind.x * _windInfluence, -_gravity) * deltaTime;
            drop.Position += drop.Velocity * deltaTime;
            if (drop.Position.y < drop.GroundY) drop.Active = false;
        }
        SetVerticesDirty();
    }

    /// <summary>粒を刀身の指定位置から伝わせ直す。</summary>
    private void ResetBead(ref Bead bead, float distance)
    {
        bead.Distance = distance;
        bead.Speed = Random.Range(_beadSpeed.x, _beadSpeed.y);
        bead.Size = Random.Range(_beadSize.x, _beadSize.y);
        bead.Alpha = Random.Range(0.7f, 1f);
    }

    /// <summary>空いている枠に滴を作り、刀身の下で膨らませ始める。</summary>
    private void StartDrop(Vector2 point)
    {
        for (int i = 0; i < _drops.Length; i++)
        {
            if (_drops[i].Active) continue;
            var rect = rectTransform.rect;
            _drops[i] = new Drop
            {
                Position = ToLocal(point),
                Velocity = Vector2.zero,
                GrowTime = Random.Range(_growTime.x, _growTime.y),
                Size = Random.Range(_dropSize.x, _dropSize.y),
                GroundY = Mathf.Lerp(rect.yMin, rect.yMax, _groundHeight),
                Active = true,
            };
            return;
        }
    }

    /// <summary>刀身に沿った粒と、膨らむ・落ちる滴を1メッシュにまとめる。</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!Application.isPlaying || _beads == null || _intensity <= 0.003f) return;
        var start = ToLocal(_bladeStart);
        var end = ToLocal(_bladeEnd);
        float length = Mathf.Max(1f, Vector2.Distance(start, end));
        var dir = (end - start) / length;
        for (int i = 0; i < _beads.Length; i++)
        {
            ref var bead = ref _beads[i];
            float t = bead.Distance / length;
            // 伝い始めと終わりで溶けるように出入りさせ、粒が突然現れないようにする
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.15f, (1f - t) / 0.25f)) * bead.Alpha;
            AddBlob(vh, start + dir * bead.Distance, dir, bead.Size * _beadStretch, bead.Size, fade);
        }
        for (int i = 0; i < _drops.Length; i++)
        {
            ref var drop = ref _drops[i];
            if (!drop.Active) continue;
            if (!drop.Falling)
            {
                // 刀身の下側で垂れ下がりながら膨らむ
                float grow = Mathf.SmoothStep(0f, 1f, drop.Age / drop.GrowTime);
                float size = drop.Size * Mathf.Lerp(0.35f, 1f, grow);
                AddBlob(vh, drop.Position + Vector2.down * size * 0.5f * grow, Vector2.down, size * Mathf.Lerp(1f, 1.5f, grow), size, 1f);
                continue;
            }
            float speed = drop.Velocity.magnitude;
            var fallDir = speed > 0.001f ? drop.Velocity / speed : Vector2.down;
            float height = Mathf.Max(1f, drop.Position.y - drop.GroundY);
            float alpha = Mathf.Clamp01(height / 30f);
            AddBlob(vh, drop.Position, fallDir, drop.Size * (1f + speed * _fallStretch), drop.Size * 0.8f, alpha);
        }
    }

    /// <summary>血の楕円と、その上側の小さな艶を追加する。</summary>
    private void AddBlob(VertexHelper vh, Vector2 center, Vector2 dir, float length, float width, float alpha)
    {
        var tint = color;
        var blood = _bloodColor * tint;
        blood.a *= alpha * _intensity;
        if (blood.a <= 0.003f) return;
        AddQuad(vh, center, dir, length, width, blood);
        var gloss = _glossColor * tint;
        gloss.a *= alpha * _intensity;
        var side = new Vector2(-dir.y, dir.x);
        // 艶は光源側（左上）へ少しずらした小さな点にする
        var glossCenter = center + (side.y > 0f ? side : -side) * width * 0.18f - dir * length * 0.1f;
        AddQuad(vh, glossCenter, dir, length * 0.45f, width * 0.4f, gloss);
    }

    /// <summary>向きと大きさを指定した四角形を追加する。</summary>
    private static void AddQuad(VertexHelper vh, Vector2 center, Vector2 dir, float length, float width, Color color)
    {
        var along = dir * (length * 0.5f);
        var side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        Color32 c32 = color;
        int index = vh.currentVertCount;
        vh.AddVert(center - along - side, c32, new Vector4(0f, 0f, 0f, 0f));
        vh.AddVert(center - along + side, c32, new Vector4(0f, 1f, 0f, 0f));
        vh.AddVert(center + along + side, c32, new Vector4(1f, 1f, 0f, 0f));
        vh.AddVert(center + along - side, c32, new Vector4(1f, 0f, 0f, 0f));
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    /// <summary>矩形比の位置をこの矩形のローカル座標へ変換する。</summary>
    /// <returns>ローカル座標。</returns>
    private Vector2 ToLocal(Vector2 normalized)
    {
        var rect = rectTransform.rect;
        return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, normalized.x), Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
    }

    /// <summary>血が伝う刀身の長さ（ローカル座標）。</summary>
    /// <returns>刀身の始点から終点までの距離。</returns>
    private float BladeLength() => Vector2.Distance(ToLocal(_bladeStart), ToLocal(_bladeEnd));

    /// <summary>縁の締まった丸を一度だけ生成して共有する。液体らしく、火の粉のような光暈は付けない。</summary>
    /// <returns>生成済みの丸テクスチャ。</returns>
    private static Texture2D GetDropTexture()
    {
        if (s_dropTexture != null) return s_dropTexture;
        const int size = 32;
        s_dropTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "TitleBloodDrop",
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
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - r) / 0.45f)) * 255f));
            }
        }
        s_dropTexture.SetPixels32(pixels);
        s_dropTexture.Apply(false, true);
        return s_dropTexture;
    }
}
