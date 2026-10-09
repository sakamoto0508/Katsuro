using UnityEngine;

/// <summary>刀身の切先側だけを時間方向につなぎ、細い軌跡を短く残す。</summary>
[DisallowMultipleComponent]
public sealed class SwordTrail : MonoBehaviour
{
    public enum AttackStyle { Light, Heavy, JustAvoidCounter }
    [Header("刀身の軌跡")]
    [SerializeField, Min(.01f)] private float _duration = .11f;
    [SerializeField] private Transform _bladeBase;
    [SerializeField] private Transform _tip;
    [Tooltip("実Blade BaseからTipへ寄せる割合。0.48なら刀身の外側52%だけを使用する。")]
    [SerializeField, Range(0f, 1f)] private float _bladeTrailStart = .48f;
    [Tooltip("同じ刀の複数Colliderは、代表となるSwordTrailを共有する。")]
    [SerializeField] private SwordTrail _sharedTrail;
    [SerializeField] private Material _trailMaterial;
    [SerializeField, Range(0f, 2f)] private float _brightness = .8f;
    [SerializeField, Min(.1f)] private float _fade = 1.5f;
    [SerializeField] private Color _color = new Color(.78f, .9f, 1f, .32f);
    [Header("振りの速度 / 攻撃差")]
    [SerializeField, Min(0f)] private float _minimumSpeed = 3.5f;
    [SerializeField, Min(.1f)] private float _fullSpeed = 16f;
    [SerializeField, Min(.01f)] private float _heavyDuration = .13f;
    [SerializeField, Min(.01f)] private float _counterDuration = .15f;
    [SerializeField, Range(1f, 2f)] private float _heavyBrightness = 1.18f;
    [SerializeField, Range(1f, 2f)] private float _counterBrightness = 1.35f;
    // 旧Prefabの幅設定を保持する。端点未設定の場合だけ使う。
    [SerializeField, HideInInspector] private float _width = .045f;
    private const int Capacity = 40;
    private readonly Vector3[] bases = new Vector3[Capacity], tips = new Vector3[Capacity];
    private readonly float[] times = new float[Capacity];
    private readonly float[] lifetimes = new float[Capacity], strengths = new float[Capacity], sampleWidths = new float[Capacity];
    private readonly Vector3[] vertices = new Vector3[Capacity * 2];
    private readonly Vector2[] uvs = new Vector2[Capacity * 2];
    private readonly Vector2[] dynamics = new Vector2[Capacity * 2];
    private readonly Color[] colors = new Color[Capacity * 2];
    private readonly int[] triangles = new int[(Capacity - 1) * 6];
    private int count;
    private bool initialized, emitting;
    private Mesh mesh;
    private GameObject surface;
    private Material material;
    private bool ownsMaterial;
    private AttackStyle style;
    private Transform owner;
    private Vector3 previousTip, previousOwnerPosition;
    private float previousTime, speedStrength;
    private bool hasPrevious;

    public void SetStyle(AttackStyle value)
    {
        if (_sharedTrail != null && _sharedTrail != this) { _sharedTrail.SetStyle(value); return; }
        style = value;
    }
    public void SetActive(bool active)
    {
        if (_sharedTrail != null && _sharedTrail != this) { _sharedTrail.SetActive(active); return; }
        if (!initialized || emitting == active) return;
        emitting = active;
        if (active) { count = 0; mesh.Clear(); hasPrevious = false; speedStrength = 0f; Sample(); }
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
        owner = GetComponentInParent<Animator>()?.transform;
        surface = new GameObject("Blade Trail Surface", typeof(MeshFilter), typeof(MeshRenderer));
        surface.layer = gameObject.layer;
        // Keep sampled vertices in world space; do not transform the history with the blade.
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        initialized = true;
        return true;
    }
    private void Sample()
    {
        Vector3 realBase = _bladeBase != null ? _bladeBase.position : transform.position;
        Vector3 b = _tip != null ? _tip.position : transform.position + transform.up * _width;
        Vector3 a = Vector3.Lerp(realBase, b, _bladeTrailStart);
        float now = Time.unscaledTime;
        Vector3 ownerPosition = owner != null ? owner.position : Vector3.zero;
        float delta = now - previousTime;
        float speed = hasPrevious && delta > .0001f ? ((b - previousTip) - (ownerPosition - previousOwnerPosition)).magnitude / delta : 0f;
        previousTip = b; previousOwnerPosition = ownerPosition; previousTime = now; hasPrevious = true;
        float target = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_minimumSpeed, Mathf.Max(_minimumSpeed + .1f, _fullSpeed), speed));
        speedStrength = Mathf.MoveTowards(speedStrength, target, Mathf.Max(0f, delta) * (target > speedStrength ? 30f : 60f));
        if (count > 0 && (a - bases[count - 1]).sqrMagnitude + (b - tips[count - 1]).sqrMagnitude < .000004f) return;
        if (speedStrength < .025f)
        {
            // Keep one invisible seed at the current blade, rather than bridging
            // a long pause to the next fast sample.
            if (count == 0) { bases[0] = a; tips[0] = b; times[0] = now; lifetimes[0] = .01f; strengths[0] = 0f; sampleWidths[0] = 1f; count = 1; }
            return;
        }
        if (count == Capacity) RemoveOldest();
        float duration = style == AttackStyle.JustAvoidCounter ? _counterDuration : style == AttackStyle.Heavy ? _heavyDuration : _duration;
        float power = style == AttackStyle.JustAvoidCounter ? _counterBrightness : style == AttackStyle.Heavy ? _heavyBrightness : 1f;
        bases[count] = a; tips[count] = b; times[count] = now;
        lifetimes[count] = duration * Mathf.Lerp(.6f, 1f, speedStrength);
        strengths[count] = speedStrength * power;
        sampleWidths[count] = style == AttackStyle.Light ? 1f : style == AttackStyle.Heavy ? 1.04f : 1.08f;
        count++;
    }
    private void RemoveOldest()
    {
        for (int i = 1; i < count; i++) CopySample(i, i - 1);
        count--;
    }
    private void CopySample(int source, int destination)
    {
        bases[destination] = bases[source]; tips[destination] = tips[source]; times[destination] = times[source];
        lifetimes[destination] = lifetimes[source]; strengths[destination] = strengths[source]; sampleWidths[destination] = sampleWidths[source];
    }
    private void LateUpdate()
    {
        if (!initialized || mesh == null) return;
        int live = 0;
        for (int i = 0; i < count; i++)
            if (Time.unscaledTime - times[i] < lifetimes[i]) { CopySample(i, live); live++; }
        count = live;
        if (emitting) Sample();
        mesh.Clear();
        if (count < 2) return;
        for (int i = 0; i < count; i++)
        {
            int v = i * 2;
            vertices[v] = bases[i]; vertices[v + 1] = tips[i];
            float age = Mathf.Clamp01((Time.unscaledTime - times[i]) / Mathf.Max(.01f, lifetimes[i]));
            uvs[v] = new Vector2(age, 0); uvs[v + 1] = new Vector2(age, 1);
            dynamics[v] = dynamics[v + 1] = new Vector2(Mathf.Clamp01(strengths[i]), sampleWidths[i]);
            Color color = _color; color.r *= _brightness * strengths[i]; color.g *= _brightness * strengths[i]; color.b *= _brightness * strengths[i];
            color.a *= Mathf.Clamp01(strengths[i]) * Mathf.Pow(1f - age, _fade);
            colors[v] = colors[v + 1] = color;
            if (i == count - 1) continue;
            int t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }
        mesh.SetVertices(vertices, 0, count * 2);
        mesh.SetUVs(0, uvs, 0, count * 2);
        mesh.SetUVs(1, dynamics, 0, count * 2);
        mesh.SetColors(colors, 0, count * 2);
        mesh.SetTriangles(triangles, 0, (count - 1) * 6, 0);
        mesh.RecalculateBounds();
    }
    private void OnDisable() { emitting = false; count = 0; if (mesh != null) mesh.Clear(); }
    private void OnDestroy() { if (mesh != null) Destroy(mesh); if (surface != null) Destroy(surface); if (ownsMaterial) Destroy(material); }
}
