using UnityEngine;

/// <summary>
/// WarpAttack の「黒い幽霊」表現。予兆で体に黒い揺らぎを重ね、消失ではその瞬間の姿勢を黒い影として足元から崩し、
/// 出現先では同じ影が足元から形を取ってから実体に替わり、出現後しばらく揺らぎが残って抜ける。
/// 体の Material は差し替えず、骨に追従する重ね表示（CombatGlow）と焼き付けた影だけを使う。移動・判定には触れない。
/// </summary>
[DisallowMultipleComponent]
public sealed class WarpGhostPresenter : MonoBehaviour
{
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int GhostId = Shader.PropertyToID("_Ghost");
    private static readonly int GhostTimeId = Shader.PropertyToID("_GhostTime");
    private static readonly int GhostSwayId = Shader.PropertyToID("_GhostSway");
    private static readonly int GhostFlowId = Shader.PropertyToID("_GhostFlowSpeed");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int InvertId = Shader.PropertyToID("_Invert");
    private static readonly int BottomId = Shader.PropertyToID("_BottomY");
    private static readonly int TopId = Shader.PropertyToID("_TopY");
    private static readonly int DriftId = Shader.PropertyToID("_Drift");
    private static readonly int FlowId = Shader.PropertyToID("_FlowTime");

    /// <summary>このコンポーネントが作る重ね表示の名前。焼き付けや他の演出の対象から外す目印にもなる。</summary>
    public const string OverlayName = "Warp Ghost Overlay";

    private enum Overlay { Off, Warning, AppearFade }

    private EnemyAttackPattern.WarpGhostSettings _s;
    private SkinnedMeshRenderer[] _sources = System.Array.Empty<SkinnedMeshRenderer>();
    private SkinnedMeshRenderer[] _overlays = System.Array.Empty<SkinnedMeshRenderer>();
    private Material _overlayMaterial;
    private Shadow _vanishShadow, _formShadow;
    private Mesh[] _baked = System.Array.Empty<Mesh>();
    private Overlay _overlay;
    private float _overlayStart, _overlayLength;
    private float _figureHeight = 2.4f;

    private sealed class Shadow
    {
        public GameObject Root; public MeshRenderer[] Parts; public Material Material;
        public float Start = -1f, Length = 1f; public bool Forming;
    }

    public void Initialize(EnemyAttackPattern.WarpGhostSettings settings)
    {
        _s = settings;
        var glow = Resources.Load<Shader>("CombatGlow");
        // Loaded from a Resources folder so the shader is always included in builds.
        var dissolve = Resources.Load<Shader>("KatsuroGhostDissolve");
        if (dissolve == null) dissolve = Shader.Find("Katsuro/GhostDissolve");
        var list = new System.Collections.Generic.List<SkinnedMeshRenderer>();
        // Only the visible body meshes; skip overlays made by this or other effects (JustAvoid flash etc.).
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null && smr.enabled && !smr.name.StartsWith(OverlayName) && !smr.name.StartsWith("JustAvoid")) list.Add(smr);
        _sources = list.ToArray();
        _baked = new Mesh[_sources.Length];
        for (int i = 0; i < _baked.Length; i++) { _baked[i] = new Mesh { name = "Warp Ghost Pose" }; _baked[i].MarkDynamic(); }

        if (glow != null)
        {
            _overlayMaterial = new Material(glow) { name = "Warp Ghost Overlay" };
            _overlayMaterial.SetFloat(GhostId, 1f);
            _overlayMaterial.SetFloat(GhostFlowId, 1.4f);
            _overlays = new SkinnedMeshRenderer[_sources.Length];
            for (int i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                var go = new GameObject(OverlayName, typeof(SkinnedMeshRenderer));
                go.layer = source.gameObject.layer;
                go.transform.SetParent(source.transform, false);
                var overlay = go.GetComponent<SkinnedMeshRenderer>();
                overlay.sharedMesh = source.sharedMesh; overlay.bones = source.bones; overlay.rootBone = source.rootBone;
                overlay.localBounds = source.localBounds;
                var mats = new Material[source.sharedMesh.subMeshCount];
                for (int m = 0; m < mats.Length; m++) mats[m] = _overlayMaterial;
                overlay.sharedMaterials = mats;
                overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlay.receiveShadows = false;
                overlay.enabled = false;
                _overlays[i] = overlay;
            }
        }
        if (dissolve != null)
        {
            _vanishShadow = CreateShadow(dissolve, "Warp Ghost Vanish");
            _formShadow = CreateShadow(dissolve, "Warp Ghost Form");
        }
        var bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var smr in _sources) bounds.Encapsulate(smr.bounds);
        _figureHeight = Mathf.Max(1f, bounds.max.y - transform.position.y);
    }

    /// <summary>消える前の構え。構え時間をかけて黒い揺らぎを体に乗せる（予兆を見せる）。</summary>
    public void BeginWarning(float seconds)
    {
        if (!_s.Enabled) return;
        StartOverlay(Overlay.Warning, Mathf.Max(.05f, seconds));
    }

    /// <summary>消失の瞬間。今の姿勢を黒い影に焼き付け、足元から崩す。実体の表示はこの直後に呼び出し側が消す。</summary>
    public void Vanish()
    {
        StopOverlay();
        if (!_s.Enabled || _vanishShadow == null) return;
        Bake();
        PlaceShadow(_vanishShadow, transform.position, transform.rotation);
        StartShadow(_vanishShadow, false, _s.VanishDuration);
    }

    /// <summary>出現先で、焼き付けた影を足元から形作る。seconds は実体が現れるまでの残り時間。</summary>
    public void Form(Vector3 feet, Quaternion facing, float seconds)
    {
        if (!_s.Enabled || _formShadow == null) return;
        PlaceShadow(_formShadow, feet, facing);
        StartShadow(_formShadow, true, Mathf.Max(.03f, seconds));
    }

    /// <summary>実体が現れた。形成中の影を消し、体に残った黒い揺らぎを AppearFade 秒で抜く。</summary>
    public void Appear()
    {
        HideShadow(_formShadow);
        if (!_s.Enabled) return;
        StartOverlay(Overlay.AppearFade, _s.AppearFade);
    }

    /// <summary>中断・終了。予兆の揺らぎと形成中の影をすぐ消す。出現後に抜けていく揺らぎと、崩れている途中の影は自然に終わらせる。</summary>
    public void Interrupt()
    {
        if (_overlay != Overlay.AppearFade) StopOverlay();
        HideShadow(_formShadow);
    }

    private void LateUpdate()
    {
        if (_s == null) return;
        float now = FeedbackVisualTime.Now;
        if (_overlay != Overlay.Off && _overlayMaterial != null)
        {
            float u = Mathf.Clamp01((now - _overlayStart) / _overlayLength);
            float strength = _overlay == Overlay.Warning ? u * u * (3f - 2f * u) : 1f - u;
            var tint = _s.Color; tint.a = Mathf.Clamp01(_s.WarningStrength * strength) * 1.6f;
            _overlayMaterial.SetColor(TintId, tint);
            _overlayMaterial.SetFloat(GhostTimeId, now);
            _overlayMaterial.SetFloat(GhostSwayId, _s.WarningSway * (.4f + .6f * strength));
            if (_overlay == Overlay.AppearFade && u >= 1f) StopOverlay();
        }
        TickShadow(_vanishShadow, now);
        TickShadow(_formShadow, now);
    }

    // ---------------------------------------------------------------- overlay on the live body
    private void StartOverlay(Overlay mode, float seconds)
    {
        _overlay = mode; _overlayStart = FeedbackVisualTime.Now; _overlayLength = Mathf.Max(.01f, seconds);
        for (int i = 0; i < _overlays.Length; i++)
            if (_overlays[i] != null) _overlays[i].enabled = _sources[i] != null && _sources[i].enabled && _sources[i].gameObject.activeInHierarchy;
    }

    private void StopOverlay()
    {
        _overlay = Overlay.Off;
        foreach (var overlay in _overlays) if (overlay != null) overlay.enabled = false;
    }

    // ---------------------------------------------------------------- baked shadows
    private Shadow CreateShadow(Shader shader, string name)
    {
        var shadow = new Shadow { Root = new GameObject(name), Material = new Material(shader) { name = name } };
        shadow.Root.SetActive(false);
        shadow.Parts = new MeshRenderer[_sources.Length];
        for (int i = 0; i < _sources.Length; i++)
        {
            var part = new GameObject(_sources[i].name, typeof(MeshFilter), typeof(MeshRenderer));
            part.layer = _sources[i].gameObject.layer;
            part.transform.SetParent(shadow.Root.transform, false);
            part.GetComponent<MeshFilter>().sharedMesh = _baked[i];
            var renderer = part.GetComponent<MeshRenderer>();
            var mats = new Material[_sources[i].sharedMesh.subMeshCount];
            for (int m = 0; m < mats.Length; m++) mats[m] = shadow.Material;
            renderer.sharedMaterials = mats;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            shadow.Parts[i] = renderer;
        }
        return shadow;
    }

    private void Bake()
    {
        var rootInverse = transform.worldToLocalMatrix;
        for (int i = 0; i < _sources.Length; i++)
        {
            var source = _sources[i];
            bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy;
            foreach (var shadow in new[] { _vanishShadow, _formShadow })
                if (shadow != null) shadow.Parts[i].enabled = visible;
            if (!visible) continue;
            source.BakeMesh(_baked[i], false);   // unscaled; the part transform carries the real scale
            // Keep each part's pose relative to the enemy root so the shadow can be placed anywhere.
            var local = rootInverse * source.transform.localToWorldMatrix;
            foreach (var shadow in new[] { _vanishShadow, _formShadow })
            {
                if (shadow == null) continue;
                var t = shadow.Parts[i].transform;
                t.localPosition = local.GetColumn(3);
                t.localRotation = local.rotation;
                t.localScale = source.transform.lossyScale;
            }
        }
    }

    private void PlaceShadow(Shadow shadow, Vector3 feet, Quaternion facing)
    {
        shadow.Root.transform.SetPositionAndRotation(feet, facing);
        shadow.Material.SetColor(ColorId, _s.Color);
        shadow.Material.SetColor(EdgeColorId, _s.EdgeColor);
        shadow.Material.SetFloat(BottomId, feet.y);
        shadow.Material.SetFloat(TopId, feet.y + _figureHeight);
        shadow.Material.SetFloat(DriftId, _s.Drift);
    }

    private void StartShadow(Shadow shadow, bool forming, float seconds)
    {
        shadow.Forming = forming; shadow.Start = FeedbackVisualTime.Now; shadow.Length = Mathf.Max(.01f, seconds);
        shadow.Material.SetFloat(InvertId, forming ? 1f : 0f);
        shadow.Material.SetFloat(ProgressId, forming ? 1f : 0f);
        shadow.Root.SetActive(true);
    }

    private void TickShadow(Shadow shadow, float now)
    {
        if (shadow == null || shadow.Start < 0f) return;
        float u = Mathf.Clamp01((now - shadow.Start) / shadow.Length);
        // Vanish erodes 0 -> 1; forming goes 1 -> 0 and then waits fully formed until the body appears.
        shadow.Material.SetFloat(ProgressId, shadow.Forming ? 1f - u : u);
        shadow.Material.SetFloat(FlowId, now);
        if (!shadow.Forming && u >= 1f) HideShadow(shadow);
    }

    private static void HideShadow(Shadow shadow)
    {
        if (shadow == null) return;
        shadow.Start = -1f;
        shadow.Root.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var overlay in _overlays) if (overlay != null) Destroy(overlay.gameObject);
        foreach (var shadow in new[] { _vanishShadow, _formShadow })
        {
            if (shadow == null) continue;
            if (shadow.Material != null) Destroy(shadow.Material);
            if (shadow.Root != null) Destroy(shadow.Root);
        }
        foreach (var mesh in _baked) if (mesh != null) Destroy(mesh);
        if (_overlayMaterial != null) Destroy(_overlayMaterial);
    }
}

