using UnityEngine;

/// <summary>
/// 牙突風 RushAttack の演出を「構え → 突進 → 刺突」の順に出す。EnemyController から合図を受けるだけで、
/// Animator・Root Motion・攻撃判定には触れない。時間は FeedbackVisualTime（Pause で停止）基準。
/// </summary>
[DisallowMultipleComponent]
public sealed class GatotsuRushVFX : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int TintId = Shader.PropertyToID("_Tint");

    private GatotsuRushVFXConfig _config;
    private Animator _animator;
    private Transform _tip, _bladeBase;
    private KatsuroTimedVFX _glint, _thrust;
    private ParticleSystem _dust;
    private TrailRenderer[] _trails = System.Array.Empty<TrailRenderer>();
    private MaterialPropertyBlock _block;

    // short afterimages (baked poses)
    private SkinnedMeshRenderer[] _skins = System.Array.Empty<SkinnedMeshRenderer>();
    private Ghost[] _ghosts = System.Array.Empty<Ghost>();
    private sealed class Ghost { public GameObject Root; public Mesh[] Meshes; public MeshRenderer[] Renderers; public Material Material; public float Started = -1f; }
    private float[] _ghostAt = System.Array.Empty<float>();
    private int _nextGhost;

    private float _glintAt = -1f, _dustAt = -1f, _thrustAt = -1f, _trailsUntil = -1f;

    public void Initialize(GatotsuRushVFXConfig config, Animator animator, Transform tip, Transform bladeBase)
    {
        _config = config; _animator = animator; _tip = tip; _bladeBase = bladeBase;
        _block = new MaterialPropertyBlock();
        if (config.Glint.Prefab != null) _glint = Spawn(config.Glint.Prefab, "Rush Tip Glint");
        if (config.ThrustLine.Prefab != null) _thrust = Spawn(config.ThrustLine.Prefab, "Rush Thrust Line");
        if (config.GroundDust.Enabled && config.DustMaterial != null) _dust = CreateDust();
        if (config.WindTrail.Enabled && config.WindMaterial != null) _trails = CreateTrails();
        if (config.AfterImages.Enabled && config.AfterImages.Count > 0) CreateGhosts();
    }

    /// <summary>構え開始。突進開始の LeadTime 秒前から刀先へ収束光を出す（予兆を隠さない控えめな光）。</summary>
    public void BeginStance(float stanceSeconds)
    {
        ClearSchedule();
        if (_glint == null) return;
        float lead = Mathf.Min(_config.Glint.LeadTime, Mathf.Max(.05f, stanceSeconds));
        _glintAt = FeedbackVisualTime.Now + Mathf.Max(0f, stanceSeconds - lead);
    }

    /// <summary>突進開始（攻撃判定 ON）。砂埃・風の軌跡・残像を出し、刺突の瞬間に白銀の直線を出す。</summary>
    public void BeginDash(float dashSeconds, float thrustDelay)
    {
        float now = FeedbackVisualTime.Now;
        _glintAt = -1f;
        if (_dust != null) _dustAt = now + _config.GroundDust.Delay;
        if (_trails.Length > 0)
        {
            _trailsUntil = now + Mathf.Max(.05f, dashSeconds);
            foreach (var trail in _trails) { trail.Clear(); trail.emitting = true; }
        }
        int count = _ghosts.Length > 0 ? _config.AfterImages.Count : 0;
        _ghostAt = new float[count];
        for (int i = 0; i < count; i++) _ghostAt[i] = now + dashSeconds * (i + 1f) / (count + 1f);
        if (_thrust != null) _thrustAt = now + Mathf.Max(0f, thrustDelay);
    }

    /// <summary>攻撃の終了・中断。中断（被弾・追撃・死亡）では刀先の光と刺突線をすぐ消す。砂埃と残像は自然に消える。</summary>
    public void End(bool cancelled)
    {
        ClearSchedule();
        foreach (var trail in _trails) trail.emitting = false;
        if (cancelled)
        {
            if (_glint != null) _glint.Stop();
            if (_thrust != null) _thrust.Stop();
        }
    }

    /// <summary>刺突が命中した。既存 HitStop と同じ時間だけ刺突線を保持して明るくする。</summary>
    public void OnHit(float hitStopSeconds)
    {
        if (_thrust != null && _thrust.IsPlaying) _thrust.HoldFor(hitStopSeconds, _config.ThrustLine.HitHoldBoost);
    }

    private void ClearSchedule()
    {
        _glintAt = _dustAt = _thrustAt = _trailsUntil = -1f;
        _ghostAt = System.Array.Empty<float>();
    }

    private void LateUpdate()
    {
        if (_config == null) return;
        float now = FeedbackVisualTime.Now;
        if (_glintAt >= 0f && now >= _glintAt) { _glintAt = -1f; PlayGlint(); }
        if (_dustAt >= 0f && now >= _dustAt) { _dustAt = -1f; PlayDust(); }
        if (_thrustAt >= 0f && now >= _thrustAt) { _thrustAt = -1f; PlayThrust(); }
        if (_trailsUntil >= 0f && now >= _trailsUntil) { _trailsUntil = -1f; foreach (var trail in _trails) trail.emitting = false; }
        for (int i = 0; i < _ghostAt.Length; i++)
            if (_ghostAt[i] >= 0f && now >= _ghostAt[i]) { _ghostAt[i] = -1f; BakeGhost(now); }
        UpdateGhosts(now);
    }

    // ---------------------------------------------------------------- stance: tip glint
    private void PlayGlint()
    {
        if (_tip == null) return;
        var c = _config.Glint;
        _glint.SetTint(c.Color);
        var view = Camera.main;
        _glint.Play(_tip.position, view != null ? KatsuroVFXOrientation.FacingCamera(_tip.position, view) : Quaternion.identity,
            c.Size, c.Intensity, c.LeadTime + .06f);
        _glint.Follow(_tip, default, true);
    }

    // ---------------------------------------------------------------- dash: dust, wind, afterimages
    private ParticleSystem CreateDust()
    {
        var go = new GameObject("Rush Ground Dust");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false; main.duration = .2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -.05f;
        main.maxParticles = 64;
        var emission = ps.emission; emission.rateOverTime = 0f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 38f; shape.radius = .12f;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .55f, 1f, 1.5f));
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .12f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 100f; limit.drag = 3.5f;   // kicked up fast, settles quickly
        var rotation = ps.rotationOverLifetime; rotation.enabled = true; rotation.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _config.DustMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private void PlayDust()
    {
        var c = _config.GroundDust;
        var foot = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;   // V08: right foot is the back (push-off) foot
        Vector3 at = foot != null ? foot.position : transform.position;
        at.y = transform.position.y + .03f;
        Vector3 back = -transform.forward;
        _dust.transform.SetPositionAndRotation(at, Quaternion.LookRotation((back + Vector3.up * .55f).normalized, Vector3.up));
        var main = _dust.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(c.Lifetime * .7f, c.Lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(c.Speed * .4f, c.Speed);
        main.startSize = new ParticleSystem.MinMaxCurve(c.Size * .6f, c.Size);
        main.startColor = c.Color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var emission = _dust.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)c.Count) });
        _dust.Clear(); _dust.Play();
    }

    private TrailRenderer[] CreateTrails()
    {
        var c = _config.WindTrail;
        var list = new System.Collections.Generic.List<TrailRenderer>();
        void Add(Transform anchor, float widthScale)
        {
            if (anchor == null) return;
            var go = new GameObject("Rush Wind Trail");
            go.transform.SetParent(anchor, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.emitting = false;
            trail.time = c.Length;
            trail.minVertexDistance = .03f;
            trail.widthMultiplier = c.Width * widthScale;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(c.Color, 0f), new GradientColorKey(c.Color, 1f) },
                new[] { new GradientAlphaKey(c.Color.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.sharedMaterial = _config.WindMaterial;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.GetPropertyBlock(_block); _block.SetFloat(IntensityId, c.Intensity); trail.SetPropertyBlock(_block);
            list.Add(trail);
        }
        if (_animator != null && _animator.isHuman)
            foreach (var bone in c.Bones) Add(_animator.GetBoneTransform(bone), 1f);
        if (c.Tip) Add(_tip, .7f);
        return list.ToArray();
    }

    private void CreateGhosts()
    {
        var shader = Resources.Load<Shader>("CombatGlow");
        if (shader == null) return;
        _skins = (_animator != null ? _animator.gameObject : gameObject).GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int pool = Mathf.Max(1, _config.AfterImages.Count) * 2;
        _ghosts = new Ghost[pool];
        for (int g = 0; g < pool; g++)
        {
            var ghost = new Ghost { Root = new GameObject("Rush AfterImage"), Material = new Material(shader) { name = "Rush AfterImage" } };
            ghost.Root.SetActive(false);
            ghost.Meshes = new Mesh[_skins.Length]; ghost.Renderers = new MeshRenderer[_skins.Length];
            for (int i = 0; i < _skins.Length; i++)
            {
                if (_skins[i].sharedMesh == null) continue;
                var part = new GameObject(_skins[i].name, typeof(MeshFilter), typeof(MeshRenderer));
                part.layer = _skins[i].gameObject.layer;
                part.transform.SetParent(ghost.Root.transform, false);
                var mesh = new Mesh { name = "Rush Pose" }; mesh.MarkDynamic();
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.GetComponent<MeshRenderer>();
                var materials = new Material[_skins[i].sharedMesh.subMeshCount];
                for (int m = 0; m < materials.Length; m++) materials[m] = ghost.Material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                ghost.Meshes[i] = mesh; ghost.Renderers[i] = renderer;
            }
            _ghosts[g] = ghost;
        }
    }

    private void BakeGhost(float now)
    {
        if (_ghosts.Length == 0) return;
        var ghost = _ghosts[_nextGhost]; _nextGhost = (_nextGhost + 1) % _ghosts.Length;
        for (int i = 0; i < _skins.Length; i++)
        {
            var skin = _skins[i]; var renderer = ghost.Renderers[i];
            if (renderer == null) continue;
            bool visible = skin != null && skin.enabled && skin.gameObject.activeInHierarchy;
            renderer.enabled = visible;
            if (!visible) continue;
            skin.BakeMesh(ghost.Meshes[i], false);   // unscaled; the transform below applies the real scale
            renderer.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
            renderer.transform.localScale = skin.transform.lossyScale;
        }
        ghost.Started = now;
        ghost.Root.SetActive(true);
        UpdateGhosts(now);
    }

    private void UpdateGhosts(float now)
    {
        var c = _config.AfterImages;
        foreach (var ghost in _ghosts)
        {
            if (ghost == null || ghost.Started < 0f) continue;
            float age = (now - ghost.Started) / Mathf.Max(.01f, c.Duration);
            if (age >= 1f) { ghost.Started = -1f; ghost.Root.SetActive(false); continue; }
            var tint = c.Color; tint.a *= 1f - age * age;
            ghost.Material.SetColor(TintId, tint);
        }
    }

    // ---------------------------------------------------------------- thrust: white-silver line + blue-steel afterglow
    private void PlayThrust()
    {
        if (_tip == null) return;
        var c = _config.ThrustLine;
        Vector3 dir = _bladeBase != null ? (_tip.position - _bladeBase.position) : transform.forward;
        if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
        dir.Normalize();
        Vector3 center = _tip.position - dir * c.BackOffset;
        var view = Camera.main;
        var rotation = view != null ? KatsuroVFXOrientation.FacingCamera(center, dir, view) : Quaternion.LookRotation(Vector3.up, Vector3.Cross(Vector3.up, dir));
        _thrust.SetTint(c.Color);
        _thrust.Play(center, rotation, c.Length, c.Intensity, c.Duration);
    }

    // ---------------------------------------------------------------- lifetime
    private KatsuroTimedVFX Spawn(KatsuroTimedVFX prefab, string name)
    {
        var instance = Instantiate(prefab);
        instance.name = name;
        foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        instance.Stop();
        return instance;
    }

    private void OnDestroy()
    {
        if (_glint != null) Destroy(_glint.gameObject);
        if (_thrust != null) Destroy(_thrust.gameObject);
        if (_dust != null) Destroy(_dust.gameObject);
        foreach (var trail in _trails) if (trail != null) Destroy(trail.gameObject);
        foreach (var ghost in _ghosts)
        {
            if (ghost == null) continue;
            if (ghost.Meshes != null) foreach (var mesh in ghost.Meshes) if (mesh != null) Destroy(mesh);
            if (ghost.Material != null) Destroy(ghost.Material);
            if (ghost.Root != null) Destroy(ghost.Root);
        }
    }
}
