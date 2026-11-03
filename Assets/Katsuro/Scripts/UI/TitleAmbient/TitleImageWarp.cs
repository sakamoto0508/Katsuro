using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背景Spriteの一部を同じ位置に重ね、格子メッシュの頂点を数pxだけずらして絵の一部を動かす。
/// 切り出す範囲は自身のアンカー（親＝背景と同じ矩形に対する割合）で指定する。
/// ずれは範囲の縁で0になるように派生クラスで決めるため、背景との継ぎ目は出ない。
/// </summary>
public abstract class TitleImageWarp : MaskableGraphic, ITitleAmbientElement
{
    /// <summary>背景Imageと同じSprite。切り出しのUV計算に使う。</summary>
    [UnityEngine.Tooltip("背景Imageと同じSprite。切り出しのUV計算に使う。")]
    [SerializeField] private Sprite _source;
    /// <summary>格子の分割数。多いほど変形が滑らかになる。</summary>
    [UnityEngine.Tooltip("格子の分割数。多いほど変形が滑らかになる。")]
    [SerializeField] private Vector2Int _grid = new Vector2Int(16, 20);

    /// <summary>演出全体の強度。ずれ量に掛ける。</summary>
    protected float Intensity { get; private set; }

    /// <summary>背景Spriteのテクスチャを使う。</summary>
    public override Texture mainTexture => _source != null ? _source.texture : s_WhiteTexture;

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

    /// <summary>派生クラスの動きを進めてメッシュを作り直す。</summary>
    public void Tick(in TitleAmbientContext context)
    {
        if (!isActiveAndEnabled || _source == null) return;
        Intensity = context.Intensity;
        Advance(context);
        SetVerticesDirty();
    }

    /// <summary>1フレーム分の動きを進める。</summary>
    protected abstract void Advance(in TitleAmbientContext context);

    /// <summary>範囲内の位置（0〜1、左下原点）での頂点のずれ（Canvas基準解像度のpx）。範囲の縁では0を返す。</summary>
    /// <returns>頂点に加えるずれ。</returns>
    protected abstract Vector2 Offset(float u, float v);

    /// <summary>範囲の縁で0、内側で1になる重み。edgeは縁から内側へ立ち上がる幅（範囲比）。</summary>
    /// <returns>0〜1の重み。</returns>
    protected static float EdgeWeight(float t, float edge)
        => edge <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(t, 1f - t) / edge));

    /// <summary>自身のアンカーに対応する背景SpriteのUVを割り当てた格子メッシュを作る。</summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_source == null) return;
        var texture = _source.texture;
        var textureRect = _source.textureRect;
        var rect = rectTransform.rect;
        var anchorMin = rectTransform.anchorMin;
        var anchorMax = rectTransform.anchorMax;
        var uvMin = new Vector2((textureRect.x + anchorMin.x * textureRect.width) / texture.width,
            (textureRect.y + anchorMin.y * textureRect.height) / texture.height);
        var uvMax = new Vector2((textureRect.x + anchorMax.x * textureRect.width) / texture.width,
            (textureRect.y + anchorMax.y * textureRect.height) / texture.height);
        int columns = Mathf.Max(1, _grid.x), rows = Mathf.Max(1, _grid.y);
        Color32 tint = color;
        // 非再生中はずれを0にし、背景とまったく同じ絵として表示する
        bool animate = Application.isPlaying;
        for (int y = 0; y <= rows; y++)
        {
            float v = (float)y / rows;
            for (int x = 0; x <= columns; x++)
            {
                float u = (float)x / columns;
                var position = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, u), Mathf.Lerp(rect.yMin, rect.yMax, v));
                if (animate) position += Offset(u, v);
                vh.AddVert(position, tint, new Vector4(Mathf.Lerp(uvMin.x, uvMax.x, u), Mathf.Lerp(uvMin.y, uvMax.y, v), 0f, 0f));
            }
        }
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int i = y * (columns + 1) + x;
                vh.AddTriangle(i, i + columns + 1, i + columns + 2);
                vh.AddTriangle(i + columns + 2, i + 1, i);
            }
        }
    }
}
