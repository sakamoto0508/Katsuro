using UnityEngine;

/// <summary>刀身の根元と切先を時間方向につなぎ、刀が通過した面を短く残す。</summary>
[DisallowMultipleComponent]
public sealed class SwordTrail : MonoBehaviour
{
    [Header("刀身の軌跡")]
    [SerializeField, Min(.01f)] private float _duration = .16f;
    [SerializeField] private Transform _bladeBase;
    [SerializeField] private Transform _tip;
    [Tooltip("同じ刀の複数Colliderは、代表となるSwordTrailを共有する。")]
    [SerializeField] private SwordTrail _sharedTrail;
    [SerializeField] private Material _trailMaterial;
    [SerializeField, Range(0f, 2f)] private float _brightness = .8f;
    [SerializeField, Min(.1f)] private float _fade = 1.5f;
    [SerializeField] private Color _color = new Color(.78f, .9f, 1f, .32f);
    // 旧Prefabの幅設定を保持する。端点未設定の場合だけ使う。
    [SerializeField, HideInInspector] private float _width = .045f;
    private const int Capacity = 40;
    private readonly Vector3[] bases = new Vector3[Capacity], tips = new Vector3[Capacity];
    private readonly float[] times = new float[Capacity];
    private readonly Vector3[] vertices = new Vector3[Capacity * 2];
    private readonly Vector2[] uvs = new Vector2[Capacity * 2];
    private readonly Color[] colors = new Color[Capacity * 2];
    private readonly int[] triangles = new int[(Capacity - 1) * 6];
    private int count;
    private bool initialized, emitting;
    private Mesh mesh;
    private GameObject surface;
    private Material material;
    private bool ownsMaterial;
    public void SetActive(bool active)
    {
        if (_sharedTrail != null && _sharedTrail != this) { _sharedTrail.SetActive(active); return; }
        if (!initialized || emitting == active) return;
        emitting = active;
        if (active) { count = 0; mesh.Clear(); Sample(); }
    }
    public bool Init()
    {
        if (initialized) return true;
        // 既存TrailRendererは保持し、線状の二重表示だけ止める。
        var legacy = GetComponent<TrailRenderer>();
        if (legacy != null) { legacy.emitting = false; legacy.Clear(); legacy.enabled = false; }
        if (_sharedTrail != null && _sharedTrail != this) { initialized = _sharedTrail.Init(); return initialized; }
        var shader = Resources.Load<Shader>("BladeTrail");
        if (_trailMaterial == null && shader == null) return false;
        material = _trailMaterial;
        if (material == null) { material = new Material(shader); ownsMaterial = true; }
        mesh = new Mesh { name = "Blade Trail Surface" }; mesh.MarkDynamic();
        surface = new GameObject("Blade Trail Surface", typeof(MeshFilter), typeof(MeshRenderer));
        surface.layer = gameObject.layer;
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        initialized = true;
        return true;
    }
    private void Sample()
    {
        Vector3 a = _bladeBase != null ? _bladeBase.position : transform.position;
        Vector3 b = _tip != null ? _tip.position : transform.position + transform.up * _width;
        if (count > 0 && (a - bases[count - 1]).sqrMagnitude + (b - tips[count - 1]).sqrMagnitude < .000004f) return;
        if (count == Capacity) RemoveOldest();
        bases[count] = a; tips[count] = b; times[count] = Time.unscaledTime; count++;
    }
    private void RemoveOldest()
    {
        for (int i = 1; i < count; i++) { bases[i - 1] = bases[i]; tips[i - 1] = tips[i]; times[i - 1] = times[i]; }
        count--;
    }
    private void LateUpdate()
    {
        if (!initialized || mesh == null) return;
        while (count > 0 && Time.unscaledTime - times[0] >= _duration) RemoveOldest();
        if (emitting) Sample();
        mesh.Clear();
        if (count < 2) return;
        for (int i = 0; i < count; i++)
        {
            int v = i * 2;
            vertices[v] = bases[i]; vertices[v + 1] = tips[i];
            uvs[v] = new Vector2(0, 0); uvs[v + 1] = new Vector2(0, 1);
            Color color = _color; color.r *= _brightness; color.g *= _brightness; color.b *= _brightness;
            color.a *= Mathf.Pow(1 - Mathf.Clamp01((Time.unscaledTime - times[i]) / Mathf.Max(.01f, _duration)), _fade);
            colors[v] = colors[v + 1] = color;
            if (i == count - 1) continue;
            int t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        mesh.SetVertices(vertices, 0, count * 2);
        mesh.SetUVs(0, uvs, 0, count * 2);
        mesh.SetColors(colors, 0, count * 2);
        mesh.SetTriangles(triangles, 0, (count - 1) * 6, 0);
        mesh.RecalculateBounds();
    }
    private void OnDisable() { emitting = false; count = 0; if (mesh != null) mesh.Clear(); }
    private void OnDestroy() { if (mesh != null) Destroy(mesh); if (surface != null) Destroy(surface); if (ownsMaterial) Destroy(material); }
}
