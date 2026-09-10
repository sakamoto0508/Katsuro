using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TrailRenderer))]
public sealed class SwordTrail : MonoBehaviour
{
    [Header("Subtle blade path")]
    /// <summary> スイングの軌跡が残る時間。 </summary>
    [SerializeField, Min(.01f)] private float _duration = .09f;
    /// <summary>
    /// スイングの軌跡の幅。値が小さいほど細くなる。
    /// </summary>
    [SerializeField, Min(.001f)] private float _width = .045f;
    [SerializeField] private Color _color = new Color(.78f, .8f, .82f, .3f);
    /// <summary> 別の位置から軌跡を出す場合、その位置に配置済みのTrailRendererのTransformを指定します。 </summary>
    [SerializeField] private Transform _tip;
    private TrailRenderer trail;
    private Material material;
    private bool initialized;

    /// <summary>武器の当たり判定から軌跡の表示を切り替えます。</summary>
    public static void SetActive(Collider blade, bool active)
    {
        var effect = blade.GetComponent<SwordTrail>();
        if (effect != null) effect.SetEmitting(active);
    }

    /// <summary>武器の準備時に呼びます。複数回呼んでも初期化は一度だけ行います。</summary>
    public bool Init()
    {
        if (initialized) return true;

        var localTrail = GetComponent<TrailRenderer>();
        trail = _tip != null ? _tip.GetComponent<TrailRenderer>() : localTrail;
        // 別位置を使う場合も、同じオブジェクトにある軌跡を二重表示させません。
        if (localTrail != null)
        {
            localTrail.emitting = false;
            localTrail.Clear();
        }
        if (trail == null) return false;
        trail.emitting = false;
        trail.Clear();

        var shader = Resources.Load<Shader>("CombatGlow");
        if (shader == null) return false;
        material = CreateTrailMaterial(shader);
        trail.sharedMaterial = material;
        ConfigureTrailRenderer();
        ApplyStyle();
        initialized = true;
        return true;
    }
    private static Material CreateTrailMaterial(Shader shader)
    {
        var trailMaterial = new Material(shader);
        trailMaterial.SetFloat("_Ghost", -1f);
        trailMaterial.SetColor("_Tint", Color.white);
        return trailMaterial;
    }

    /// <summary>軌跡の形状と描画設定を初期化します。</summary>
    private void ConfigureTrailRenderer()
    {
        trail.minVertexDistance = .025f;
        trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;
    }

    private void SetEmitting(bool active)
    {
        if (!initialized || trail == null) return;

        // 次の攻撃に前の軌跡をつなげないよう、開始時だけ消去します。
        if (active && !trail.emitting) trail.Clear();
        trail.emitting = active;
    }

    private void OnValidate()
    {
        if (trail != null) ApplyStyle();
    }

    /// <summary>Inspectorで指定した時間・幅・色を反映します。</summary>
    private void ApplyStyle()
    {
        trail.time = Mathf.Max(.01f, _duration);
        trail.widthMultiplier = Mathf.Max(.001f, _width);
        trail.startColor = _color;
        trail.endColor = new Color(_color.r, _color.g, _color.b, 0);
    }

    private void OnDisable()
    {
        if (trail == null) return;
        trail.emitting = false;
        trail.Clear();
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
