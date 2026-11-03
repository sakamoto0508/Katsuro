using UnityEngine;

/// <summary>
/// ジャスト回避追撃の「一閃」を鬼武者のように見せる追加演出：画面の白飛び、体の周りを走る巨大な弧、
/// 命中点から放射状に飛ぶ光の筋、白いしぶき、余韻の青い火花。既存の一閃（直線と光の面）・血・HitStop とは独立して重ねる。
/// 時間は FeedbackVisualTime（Pause で停止）、粒子は既存の追撃演出と同じく unscaled 時間。
/// </summary>
[DisallowMultipleComponent]
public sealed class OnimushaIssenVFX : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

    private VFXConfig.IssenFinisher _s;
    private MeshRenderer _arc, _flash;
    private Material _arcMaterial, _flashMaterial, _additiveParticle, _alphaParticle, _sparkParticle;
    private ParticleSystem _streaks, _mist, _sparks;
    private float _arcStart = -1f, _flashStart = -1f, _flashPeak;

    public void Initialize(VFXConfig config)
    {
        _s = config.IssenFinisherStyle;
        int layer = gameObject.layer;
        if (config.IssenArcMaterial != null)
        {
            _arcMaterial = new Material(config.IssenArcMaterial) { name = "Issen Arc" };
            _arc = CreateRenderer("Issen Arc", BuildArcMesh(_s.ArcAngle), _arcMaterial, layer);
        }
        var additive = Shader.Find("Katsuro/VFXAdditive");
        if (additive != null)
        {
            _flashMaterial = new Material(additive) { name = "Issen Screen Flash" };   // default white texture = flat wash
            var quad = new Mesh { name = "Issen Flash Quad" };
            quad.SetVertices(new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) });
            quad.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            quad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            _flash = CreateRenderer("Issen Screen Flash", quad, _flashMaterial, layer);
        }
        if (config.IssenParticleMaterial != null)
        {
            _alphaParticle = new Material(config.IssenParticleMaterial) { name = "Issen Mist" };
            _additiveParticle = new Material(config.IssenParticleMaterial) { name = "Issen Light Particles" };
            _additiveParticle.SetFloat(SrcBlendId, (float)UnityEngine.Rendering.BlendMode.One);
            _additiveParticle.SetFloat(DstBlendId, (float)UnityEngine.Rendering.BlendMode.One);
            _sparkParticle = new Material(_additiveParticle) { name = "Issen Sparks" };   // own material: its own intensity
            _streaks = CreateParticles("Issen Streaks", _additiveParticle, layer, ParticleSystemRenderMode.Stretch);
            _mist = CreateParticles("Issen Mist", _alphaParticle, layer, ParticleSystemRenderMode.Billboard);
            _sparks = CreateParticles("Issen Sparks", _sparkParticle, layer, ParticleSystemRenderMode.Billboard);
        }
    }

    /// <summary>追撃が命中した瞬間に呼ぶ。attacker はプレイヤー（弧の中心）。</summary>
    public void Play(Vector3 hit, Vector3 slashDirection, Vector3 attacker, bool first)
    {
        if (_s == null || !_s.Enabled) return;
        var view = Camera.main;
        float now = FeedbackVisualTime.Now;

        // 1) Screen white-out.
        _flashPeak = first ? _s.FirstFlash : _s.SecondFlash;
        if (_flash != null && _flashPeak > 0f && view != null) { _flashStart = now; _flash.enabled = true; PlaceFlash(view); }

        // 2) Huge arc sweeping around the attacker, its rim passing through the hit point.
        if (_arc != null && (first ? _s.FirstArc : _s.SecondArc) && view != null)
        {
            Vector3 center = attacker + Vector3.up * Mathf.Clamp(hit.y - attacker.y, .6f, 1.4f);
            float radius = Mathf.Clamp(Vector3.Distance(center, hit) + _s.ArcRadiusPadding, 1.5f, 4.5f);
            var rotation = KatsuroVFXOrientation.FacingCamera(center, slashDirection, view);
            _arc.transform.SetPositionAndRotation(center, rotation);
            _arc.transform.localScale = Vector3.one * radius;
            _arcStart = now;
            _arc.enabled = true;
        }

        // 3) Radial streaks, white spray and lingering blue sparks from the contact point.
        Burst(_streaks, hit, first ? _s.StreakCount / 2 : _s.StreakCount, _s.StreakSpeed * .6f, _s.StreakSpeed, .07f, .15f, .04f, .08f, _s.StreakColor, 2f);
        Burst(_mist, hit, _s.MistCount, 2f, 5f, .3f, .55f, .45f, 1.1f, _s.MistColor, 1f);
        Burst(_sparks, hit, first ? _s.SparkCount / 2 : _s.SparkCount, 1.5f, 6f, .35f, .8f, .02f, .05f, _s.SparkColor, _s.SparkIntensity);
    }

    private void LateUpdate()
    {
        if (_s == null) return;
        float now = FeedbackVisualTime.Now;
        if (_flashStart >= 0f)
        {
            float u = Mathf.Clamp01((now - _flashStart) / Mathf.Max(.01f, _s.FlashDuration));
            var view = Camera.main;
            if (u >= 1f || view == null) { _flashStart = -1f; _flash.enabled = false; }
            else
            {
                PlaceFlash(view);
                float envelope = u < .15f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (u - .15f) / .85f);   // hit white, then fall away
                _flashMaterial.SetColor(ColorId, _s.FlashColor);
                _flashMaterial.SetFloat(IntensityId, _flashPeak * envelope);
            }
        }
        if (_arcStart >= 0f)
        {
            float t = now - _arcStart;
            float total = _s.ArcSweep + _s.ArcFade;
            if (t >= total) { _arcStart = -1f; _arc.enabled = false; }
            else
            {
                float sweep = Mathf.Clamp01(t / Mathf.Max(.01f, _s.ArcSweep));
                float fade = Mathf.Clamp01((t - _s.ArcSweep) / Mathf.Max(.01f, _s.ArcFade));
                _arcMaterial.SetColor(ColorId, _s.ArcColor);
                _arcMaterial.SetFloat(ProgressId, Mathf.Lerp(0f, 1.2f, 1f - (1f - sweep) * (1f - sweep)));
                _arcMaterial.SetFloat(IntensityId, _s.ArcIntensity * (1f - Mathf.SmoothStep(0f, 1f, fade)));
            }
        }
    }

    // ---------------------------------------------------------------- building blocks
    /// <summary>扇形（環の一部）。u=尾(0)→先端(1)、v=外縁の明るい縁(0)→内側(1)。ローカルX=斬撃方向、面は+Y側。</summary>
    private static Mesh BuildArcMesh(float angleDeg)
    {
        const int segments = 64, rows = 8; const float inner = .3f;
        var verts = new Vector3[(segments + 1) * (rows + 1)];
        var uvs = new Vector2[verts.Length];
        var tris = new int[segments * rows * 6];
        float half = angleDeg * .5f * Mathf.Deg2Rad;
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            float a = Mathf.PI * .5f + half - 2f * half * u;   // sweeps toward +X (the slash direction)
            for (int j = 0; j <= rows; j++)
            {
                float v = j / (float)rows;
                float r = Mathf.Lerp(1f, inner, v);
                int k = i * (rows + 1) + j;
                verts[k] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                uvs[k] = new Vector2(u, v);
            }
        }
        int t = 0;
        for (int i = 0; i < segments; i++)
            for (int j = 0; j < rows; j++)
            {
                int a0 = i * (rows + 1) + j, a1 = a0 + rows + 1;
                tris[t++] = a0; tris[t++] = a1; tris[t++] = a0 + 1;
                tris[t++] = a0 + 1; tris[t++] = a1; tris[t++] = a1 + 1;
            }
        var mesh = new Mesh { name = "Issen Arc" };
        mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris; mesh.RecalculateBounds();
        return mesh;
    }

    private MeshRenderer CreateRenderer(string name, Mesh mesh, Material material, int layer)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = layer };
        go.hideFlags = HideFlags.DontSave;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.enabled = false;
        return renderer;
    }

    private void PlaceFlash(Camera view)
    {
        float d = view.nearClipPlane + .05f;
        float h = 2f * d * Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad) * 1.1f;
        _flash.transform.SetPositionAndRotation(view.transform.position + view.transform.forward * d, view.transform.rotation);
        _flash.transform.localScale = new Vector3(h * view.aspect, h, 1f);
    }

    private static ParticleSystem CreateParticles(string name, Material material, int layer, ParticleSystemRenderMode mode)
    {
        var go = new GameObject(name) { layer = layer, hideFlags = HideFlags.DontSave };
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false; main.duration = .1f; main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
        var emission = ps.emission; emission.rateOverTime = 0f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .06f;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(.8f, .35f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 1000f; limit.drag = 2.5f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = mode;
        if (mode == ParticleSystemRenderMode.Stretch) { renderer.lengthScale = 3f; renderer.velocityScale = .05f; }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private static void Burst(ParticleSystem ps, Vector3 at, int count, float speedMin, float speedMax, float lifeMin, float lifeMax,
        float sizeMin, float sizeMax, Color color, float intensity)
    {
        if (ps == null || count <= 0) return;
        ps.transform.position = at;
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = color;
        main.gravityModifier = ps.name == "Issen Sparks" ? .35f : 0f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial.SetFloat(IntensityId, intensity);
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        ps.Play();
    }

    private void OnDestroy()
    {
        foreach (var r in new Renderer[] { _arc, _flash }) if (r != null) Destroy(r.gameObject);
        foreach (var p in new[] { _streaks, _mist, _sparks }) if (p != null) Destroy(p.gameObject);
        foreach (var m in new[] { _arcMaterial, _flashMaterial, _additiveParticle, _alphaParticle, _sparkParticle }) if (m != null) Destroy(m);
    }
}
