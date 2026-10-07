using UnityEngine;

/// <summary> キャラクターの攻撃や被弾時のフィードバックを管理するコンポーネント。 </summary>
[DisallowMultipleComponent]
public sealed class CombatFeedback : MonoBehaviour
{
    [Header("Ghost shimmer")]
    /// <summary> ゴースト表示時の色。 </summary>
    [SerializeField] private Color _ghostTint = new Color(.66f, .78f, .8f, .35f);
    /// <summary>
    /// ゴースト表示時の揺れの大きさ。値が大きいほど揺れが大きくなる。
    /// </summary>
    [SerializeField, Range(0f, .03f)] private float _ghostSway = .009f;
    /// <summary>
    /// ゴースト表示時の流れる速度。値が大きいほど流れが速くなる。
    /// </summary>
    [SerializeField, Range(.1f, 4f)] private float _ghostFlowSpeed = 1.2f;
    [Header("ジャスト回避")]
    [SerializeField, Min(.01f)] private float _afterImageDuration = .3f;
    [SerializeField, Min(.01f)] private float _flashDuration = .15f;
    [SerializeField] private Color _afterImageTint = new Color(.75f, .9f, 1f, .3f);
    [SerializeField] private Color _flashTint = new Color(.85f, .95f, 1f, .65f);
    [Header("接触フィードバック")]
    [SerializeField, Min(.01f)] private float _hitFlashDuration = .09f;
    [SerializeField, Min(.01f)] private float _justAvoidFlashDuration = .09f;
    [SerializeField, Min(.01f)] private float _justAvoidFlashSize = .35f;
    [SerializeField, Range(0f, 20f)] private float _lightReactionAngle = 5f;
    [SerializeField, Range(0f, 25f)] private float _heavyReactionAngle = 10f;
    [SerializeField, Min(.01f)] private float _reactionDuration = .18f;
    [SerializeField, Range(0f, 1f)] private float _justAvoidBodyFlashStrength = .2f;
    private Vector3 _reactionEuler;
    private CameraManager _cameraFeedback;
    private JustAvoidScreenDistortion _screenDistortion;
    private HitStopManager _hitStop;
    private ContactPulse _contactPulse;
    private ParticleSystem[][] _hitParticles;
    private ParticleSystem.Burst[][][] _hitBursts;
    private float[][] _hitAngles;
    private ParticleSystem.MinMaxCurve[][] _hitSizes;
    private ParticleSystem.MinMaxCurve[][] _hitSpeeds;
    private GameObject[] _hitRoots;
    private float[] _hitStarted;
    private int _nextHit;
    private float[] _shockwaveSizes;
    private ParticleSystem.MinMaxGradient[] _shockwaveColors;
    private AudioManager _audio;
    private VFXConfig _vfxConfig;
    private GameObject _afterImageRoot, _shockwaveRoot;
    private Mesh[] _afterImageMeshes;
    private MeshRenderer[] _afterImageRenderers;
    private SkinnedMeshRenderer[] _flashRenderers;
    private ParticleSystem[] _shockwaveParticles;
    private Material _afterImageMaterial, _flashMaterial;
    private float _justAvoidStarted;
    private bool _justAvoidPlaying;
    private Renderer[] _renderers;
    private Material[][] _original, _glow;
    private Material _material;
    private Transform _bone;
    private Quaternion _applied = Quaternion.identity;
    private float _hitUntil;
    private bool _ghost, _showing;
    private bool _initialized;

    public void Init(AudioManager audio = null, VFXConfig vfxConfig = null, CameraManager camera = null, HitStopManager hitStop = null, bool enableJustAvoid = true)
    {
        if (_initialized) return;
        _initialized = true;
        var shader = Resources.Load<Shader>("CombatGlow");
        if (shader != null) _material = new Material(shader);
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        _original = new Material[_renderers.Length][];
        _glow = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _original[i] = _renderers[i].sharedMaterials;
            _glow[i] = new Material[_original[i].Length];
            for (int j = 0; j < _glow[i].Length; j++) _glow[i][j] = _material;
        }
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman) _bone = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (_bone == null && _renderers.Length > 0) _bone = ((SkinnedMeshRenderer)_renderers[0]).rootBone;
        _audio = audio;
        _vfxConfig = vfxConfig;
        _cameraFeedback = camera; _hitStop = hitStop;
        if (enableJustAvoid && camera != null) _screenDistortion = camera.GetComponent<JustAvoidScreenDistortion>();
        PrepareContactFeedback();
        // 敵の被弾用コンポーネントには残像用リソースを作らない。
        if (enableJustAvoid && (audio != null || vfxConfig != null)) PrepareJustAvoid(shader);
    }

    private void PrepareJustAvoid(Shader shader)
    {
        if (shader != null)
        {
            _afterImageMaterial = new Material(shader) { name = "JustAvoid AfterImage" };
            _flashMaterial = new Material(shader) { name = "JustAvoid Flash" };
            _afterImageRoot = new GameObject("JustAvoid AfterImage");
            _afterImageRoot.SetActive(false);
            _afterImageMeshes = new Mesh[_renderers.Length];
            _afterImageRenderers = new MeshRenderer[_renderers.Length];
            _flashRenderers = new SkinnedMeshRenderer[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var source = (SkinnedMeshRenderer)_renderers[i];
                if (source.sharedMesh == null) continue;
                var snapshot = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                snapshot.layer = source.gameObject.layer;
                snapshot.transform.SetParent(_afterImageRoot.transform, false);
                var mesh = new Mesh { name = "JustAvoid Pose" };
                mesh.MarkDynamic();
                _afterImageMeshes[i] = mesh;
                snapshot.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = snapshot.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = RepeatedMaterial(_afterImageMaterial, source.sharedMesh.subMeshCount);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _afterImageRenderers[i] = renderer;

                // 本体のMaterialには触れず、同じ骨で動く薄い発光レイヤーを重ねる。
                var flash = new GameObject("JustAvoid Flash", typeof(SkinnedMeshRenderer));
                flash.layer = source.gameObject.layer;
                flash.transform.SetParent(source.transform, false);
                var overlay = flash.GetComponent<SkinnedMeshRenderer>();
                overlay.sharedMesh = source.sharedMesh;
                overlay.bones = source.bones;
                overlay.rootBone = source.rootBone;
                overlay.localBounds = source.localBounds;
                overlay.sharedMaterials = RepeatedMaterial(_flashMaterial, source.sharedMesh.subMeshCount);
                overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlay.receiveShadows = false;
                overlay.enabled = false;
                _flashRenderers[i] = overlay;
            }
        }
        if (_vfxConfig != null && _vfxConfig.JustAvoidShockwavePrefab != null)
        {
            // 非アクティブな親の下で準備し、初期化時の自動再生を防ぐ。
            _shockwaveRoot = new GameObject("JustAvoid Shockwave");
            _shockwaveRoot.SetActive(false);
            var effect = Instantiate(_vfxConfig.JustAvoidShockwavePrefab, _shockwaveRoot.transform, false);
            effect.SetActive(true);
            _shockwaveParticles = effect.GetComponentsInChildren<ParticleSystem>(true);
            _shockwaveSizes = new float[_shockwaveParticles.Length];
            _shockwaveColors = new ParticleSystem.MinMaxGradient[_shockwaveParticles.Length];
            for (int i = 0; i < _shockwaveParticles.Length; i++)
            {
                var particle = _shockwaveParticles[i];
                var main = particle.main;
                _shockwaveSizes[i] = main.startSizeMultiplier;
                _shockwaveColors[i] = main.startColor;
                main.loop = false;
                main.playOnAwake = false;
                main.useUnscaledTime = true;
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }

    private Material[] RepeatedMaterial(Material material, int count)
    {
        var materials = new Material[count];
        for (int i = 0; i < count; i++) materials[i] = material;
        return materials;
    }

    /// <summary>成立通知から一度だけ呼ぶ。判定、ゲージ、スローには触れない。</summary>
    public void PlayJustAvoidFeedback() => PlayJustAvoidFeedback(new DamageInfo(0, transform.position + Vector3.up, transform.forward, null, null));
    public void PlayJustAvoidFeedback(DamageInfo info)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        StopJustAvoid();
        _justAvoidStarted = Time.unscaledTime;
        _justAvoidPlaying = true;
        if (_afterImageRoot != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var source = (SkinnedMeshRenderer)_renderers[i];
                var snapshot = _afterImageRenderers[i];
                if (snapshot == null) continue;
                bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy;
                snapshot.enabled = visible;
                if (!visible) continue;
                // 成功時の姿勢を一度だけ焼き付け、ワールド上の成功地点に固定する。
                source.BakeMesh(_afterImageMeshes[i], true);
                snapshot.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                snapshot.transform.localScale = source.transform.lossyScale;
            }
            _afterImageRoot.SetActive(true);
        }
        if (_shockwaveRoot != null && _vfxConfig != null && _vfxConfig.ShockwaveEnable)
        {
            _shockwaveRoot.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
            _shockwaveRoot.SetActive(true);
            for (int i = 0; i < _shockwaveParticles.Length; i++)
            {
                var particle = _shockwaveParticles[i];
                if (particle == null) continue;
                var main = particle.main;
                main.startSizeMultiplier = _shockwaveSizes[i] * _vfxConfig.ShockwaveSize;
                var baseColor = _shockwaveColors[i];
                baseColor.color = new Color(baseColor.color.r, baseColor.color.g, baseColor.color.b, baseColor.color.a * _vfxConfig.ShockwaveStrength);
                main.startColor = baseColor;
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(false);
            }
        }
        var sound = _audio != null && _audio.AudioConfig != null ? _audio.AudioConfig.JustAvoidSound : null;
        if (!string.IsNullOrEmpty(sound)) _audio.PlaySE(sound);
        _screenDistortion?.Play(info.HitPoint, transform.position + Vector3.up, _vfxConfig);
        _contactPulse?.Play(info.HitPoint, info.HitNormal, _justAvoidFlashSize, _justAvoidFlashDuration, .65f);
        _cameraFeedback?.PlayJustAvoidFeedback();
        UpdateJustAvoid();
    }

    private void UpdateJustAvoid()
    {
        if (!_justAvoidPlaying) return;
        float elapsed = Time.unscaledTime - _justAvoidStarted;
        if (_afterImageRoot != null)
        {
            var tint = _afterImageTint;
            tint.a *= 1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, _afterImageDuration));
            _afterImageMaterial.SetColor("_Tint", tint);
            _afterImageRoot.SetActive(elapsed < _afterImageDuration);
        }
        if (_flashMaterial != null)
        {
            var tint = _flashTint;
            tint.a *= _justAvoidBodyFlashStrength;
            tint.a *= 1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, _flashDuration));
            _flashMaterial.SetColor("_Tint", tint);
            for (int i = 0; i < _flashRenderers.Length; i++)
            {
                var overlay = _flashRenderers[i];
                if (overlay == null) continue;
                var source = (SkinnedMeshRenderer)_renderers[i];
                overlay.enabled = elapsed < _flashDuration && source != null && source.enabled;
                if (overlay.enabled)
                    for (int shape = 0; shape < source.sharedMesh.blendShapeCount; shape++)
                        overlay.SetBlendShapeWeight(shape, source.GetBlendShapeWeight(shape));
            }
        }
        float shockwaveDuration = _vfxConfig != null ? _vfxConfig.ShockwaveDuration : 0f;
        if (_shockwaveRoot != null && elapsed >= shockwaveDuration) StopShockwave();
        if (elapsed >= Mathf.Max(_afterImageDuration, Mathf.Max(_flashDuration, shockwaveDuration))) StopJustAvoid();
    }

    private void StopShockwave()
    {
        if (_shockwaveRoot == null || !_shockwaveRoot.activeSelf) return;
        foreach (var particle in _shockwaveParticles)
            if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        _shockwaveRoot.SetActive(false);
    }

    private void StopJustAvoid()
    {
        _justAvoidPlaying = false;
        if (_afterImageRoot != null) _afterImageRoot.SetActive(false);
        if (_flashRenderers != null)
            foreach (var overlay in _flashRenderers) if (overlay != null) overlay.enabled = false;
        StopShockwave();
    }

    /// <summary>
    /// ゴースト表示を有効または無効にします。
    /// </summary>
    /// <param name="active"></param>
    public void SetGhost(bool active) { _ghost = active; Refresh(); }

    /// <summary>
    /// 攻撃や被弾時のヒットエフェクトをトリガーします。
    /// </summary>
    public void Hit() { _reactionEuler = new Vector3(-_lightReactionAngle, 0, 0); _hitUntil = Time.unscaledTime + _reactionDuration; Refresh(); }

    /// <summary>受理された命中から同期して演出する。AI、移動、攻撃状態は変更しない。</summary>
    [SerializeField, Min(0f), Tooltip("Just Avoid追撃専用のHitStop。通常Light / Heavyの値は維持する。")]
    private float _justAvoidCounterHitStop = .1f;

    public void Hit(DamageInfo info, bool useBoneReaction = true)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        float angle = info.IsHeavy ? _heavyReactionAngle : _lightReactionAngle;
        bool strongFeedback = info.IsHeavy || info.IsJustAvoidCounter;
        Vector3 direction = transform.InverseTransformDirection(info.HitNormal.normalized);
        if (!useBoneReaction) RemoveOffset();
        _reactionEuler = useBoneReaction ? new Vector3(-direction.z * angle, 0, direction.x * angle) : Vector3.zero;
        _hitUntil = Time.unscaledTime + Mathf.Max(.01f, _reactionDuration);
        Refresh();
        _contactPulse?.Play(info.HitPoint, info.HitNormal, strongFeedback ? .16f : .12f, Mathf.Min(_hitFlashDuration, .05f), strongFeedback ? .65f : .45f, new Color(1f, .96f, .9f));
        PlayHitVFX(info);
        var config = _audio != null ? _audio.AudioConfig : null;
        _audio?.PlaySE(config != null ? (strongFeedback ? config.HeavyHitSound : config.LightHitSound) : "Damage");
        if (_hitStop != null)
        {
            float duration = info.IsJustAvoidCounter ? Mathf.Max(_justAvoidCounterHitStop, _hitStop.HeavyHitStop) : info.IsHeavy ? _hitStop.HeavyHitStop : _hitStop.LightHitStop;
            _hitStop.PlayHitStop(duration, gameObject, info.Instigator);
        }
        _cameraFeedback?.PlayHitFeedback(info.IsHeavy, info.HitPoint);
    }

    private void PrepareContactFeedback()
    {
        _contactPulse = new ContactPulse(gameObject.layer);
        if (_vfxConfig == null || _vfxConfig.HitVFX == null) return;
        _hitRoots = new GameObject[4]; _hitParticles = new ParticleSystem[4][]; _hitStarted = new float[4];
        _hitBursts = new ParticleSystem.Burst[4][][];
        _hitAngles = new float[4][]; _hitSizes = new ParticleSystem.MinMaxCurve[4][];
        _hitSpeeds = new ParticleSystem.MinMaxCurve[4][];
        for (int i = 0; i < 4; i++)
        {
            _hitRoots[i] = new GameObject("Contact VFX"); _hitRoots[i].SetActive(false);
            var effect = Instantiate(_vfxConfig.HitVFX, _hitRoots[i].transform, false); effect.SetActive(true);
            _hitParticles[i] = effect.GetComponentsInChildren<ParticleSystem>(true);
            _hitBursts[i] = new ParticleSystem.Burst[_hitParticles[i].Length][];
            _hitAngles[i] = new float[_hitParticles[i].Length];
            _hitSizes[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            _hitSpeeds[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            for (int j = 0; j < _hitParticles[i].Length; j++)
            {
                var particle = _hitParticles[i][j];
                var emission = particle.emission;
                _hitBursts[i][j] = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(_hitBursts[i][j]);
                _hitAngles[i][j] = particle.shape.angle;
                _hitSizes[i][j] = particle.main.startSize;
                _hitSpeeds[i][j] = particle.main.startSpeed;
                var main = particle.main;
                main.loop = false; main.playOnAwake = false; main.useUnscaledTime = true; main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }
    private void PlayHitVFX(DamageInfo info)
    {
        if (_hitRoots == null) return;
        bool strongFeedback = info.IsHeavy || info.IsJustAvoidCounter;
        int i = _nextHit; _nextHit = (_nextHit + 1) % _hitRoots.Length;
        Vector3 direction = info.SlashDirection.sqrMagnitude > .0001f ? info.SlashDirection : info.HitNormal;
        if (direction.sqrMagnitude < .0001f) direction = transform.forward;
        // 垂直の斬り上げでもLookRotationのforward/upが平行にならないようにする。
        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
        _hitRoots[i].transform.SetPositionAndRotation(info.HitPoint, Quaternion.LookRotation(direction, up));
        _hitRoots[i].transform.localScale = Vector3.one * (strongFeedback ? _vfxConfig.HeavyHitVFXScale : 1f);
        _hitRoots[i].SetActive(true); _hitStarted[i] = Time.unscaledTime;
        for (int j = 0; j < _hitParticles[i].Length; j++)
        {
            var particle = _hitParticles[i][j];
            if (particle == null) continue;
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            // 再利用時もPrefabの初期値から計算し、強攻撃の倍率が蓄積しないようにする。
            float amount = strongFeedback ? _vfxConfig.HeavyHitVFXAmount : 1f;
            var emission = particle.emission;
            for (int b = 0; b < _hitBursts[i][j].Length; b++)
            {
                var burst = _hitBursts[i][j][b];
                var count = ScaleHitCurve(burst.count, amount);
                // Burstは整数粒子数なので、端数の切り捨てで強攻撃が弱くならないよう丸める。
                if (count.mode == ParticleSystemCurveMode.Constant) count.constant = Mathf.RoundToInt(count.constant);
                else if (count.mode == ParticleSystemCurveMode.TwoConstants)
                { count.constantMin = Mathf.RoundToInt(count.constantMin); count.constantMax = Mathf.RoundToInt(count.constantMax); }
                burst.count = count;
                emission.SetBurst(b, burst);
            }
            var shape = particle.shape;
            shape.angle = _hitAngles[i][j] * (strongFeedback ? _vfxConfig.HeavyHitVFXSpread : 1f);
            var main = particle.main;
            main.startSize = ScaleHitCurve(_hitSizes[i][j], strongFeedback ? _vfxConfig.HeavyHitVFXSize : 1f);
            main.startSpeed = ScaleHitCurve(_hitSpeeds[i][j], strongFeedback ? _vfxConfig.HeavyHitVFXSpeed : 1f);
            particle.Play(false);
        }
    }
    private static ParticleSystem.MinMaxCurve ScaleHitCurve(ParticleSystem.MinMaxCurve value, float scale)
    {
        if (value.mode == ParticleSystemCurveMode.Constant) value.constant *= scale;
        else if (value.mode == ParticleSystemCurveMode.TwoConstants)
        { value.constantMin *= scale; value.constantMax *= scale; }
        else value.curveMultiplier *= scale;
        return value;
    }

    private void StopHitVFX(int i)
    {
        foreach (var particle in _hitParticles[i]) if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        _hitRoots[i].SetActive(false);
    }

    private void Update() 
    { 
        RemoveOffset(); 
        Refresh(); 
        UpdateJustAvoid();
        _contactPulse?.Update();
        if (_hitRoots != null)
            for (int i = 0; i < _hitRoots.Length; i++)
                if (_hitRoots[i].activeSelf && Time.unscaledTime - _hitStarted[i] >= _vfxConfig.HitVFXDuration) StopHitVFX(i);
    }

    private void LateUpdate()
    {
        if (_bone == null) return;
        // ヒットエフェクトの残り時間に応じてボーンを揺らす
        float remaining = Mathf.Clamp01((_hitUntil - Time.unscaledTime) / Mathf.Max(.01f, _reactionDuration));
        _applied = Quaternion.Euler(_reactionEuler * remaining * Mathf.Sin(remaining * Mathf.PI));
        _bone.localRotation *= _applied;
    }

    /// <summary>
    /// ボーンの回転オフセットをリセットします。
    /// </summary>
    private void RemoveOffset()
    {
        if (_bone != null) _bone.localRotation *= Quaternion.Inverse(_applied);
        _applied = Quaternion.identity;
    }

    /// <summary>
    /// レンダラーのマテリアルを更新して、ゴースト表示やヒットエフェクトの状態を反映します。
    /// </summary>
    private void Refresh()
    {
        bool hit = Time.unscaledTime < _hitUntil - Mathf.Max(0f, _reactionDuration - _hitFlashDuration);
        bool visible = _material != null && (hit || _ghost);
        if (_material != null && visible)
        {
            _material.SetFloat("_Ghost", _ghost && !hit ? 1f : 0f);
            _material.SetColor("_Tint", hit ? new Color(1f, .75f, .6f, .9f) : _ghostTint);
            if (_ghost && !hit)
            {
                _material.SetFloat("_GhostTime", Time.unscaledTime);
                _material.SetFloat("_GhostSway", _ghostSway);
                _material.SetFloat("_GhostFlowSpeed", _ghostFlowSpeed);
            }
        }
        if (visible == _showing) return;
        _showing = visible;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].sharedMaterials = visible ? _glow[i] : _original[i];
    }

    private void OnDisable()
    {
        StopJustAvoid();
        _contactPulse?.Stop();
        if (_hitRoots != null) for (int i = 0; i < _hitRoots.Length; i++) StopHitVFX(i);
        RemoveOffset();
        _ghost = false; _hitUntil = 0;
        Refresh();
    }

    private void OnDestroy() 
    { 
        _contactPulse?.Dispose();
        if (_hitRoots != null) foreach (var root in _hitRoots) if (root != null) Destroy(root);
        if (_afterImageMeshes != null)
            foreach (var mesh in _afterImageMeshes) if (mesh != null) Destroy(mesh);
        if (_flashRenderers != null)
            foreach (var overlay in _flashRenderers) if (overlay != null) Destroy(overlay.gameObject);
        if (_afterImageRoot != null) Destroy(_afterImageRoot);
        if (_shockwaveRoot != null) Destroy(_shockwaveRoot);
        if (_afterImageMaterial != null) Destroy(_afterImageMaterial);
        if (_flashMaterial != null) Destroy(_flashMaterial);
        if (_material != null) Destroy(_material); 
    }
}

/// <summary>接触位置の短い光を固定数で再利用する。MaterialとMeshは初期化時に一度だけ作る。</summary>
internal sealed class ContactPulse
{
    private readonly GameObject[] objects;
    private readonly MeshRenderer[] renderers;
    private readonly float[] started, durations, strengths;
    private readonly Color[] tints;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private readonly Material material;
    private readonly Mesh mesh;
    private int next;
    public ContactPulse(int layer)
    {
        var shader = Resources.Load<Shader>("ContactFlash");
        if (shader == null) return;
        material = new Material(shader);
        mesh = new Mesh { name = "Contact Flash Quad" };
        mesh.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateBounds();
        objects = new GameObject[4]; renderers = new MeshRenderer[4];
        started = new float[4]; durations = new float[4]; strengths = new float[4]; tints = new Color[4];
        for (int i = 0; i < 4; i++)
        {
            objects[i] = new GameObject("Contact Flash", typeof(MeshFilter), typeof(MeshRenderer));
            objects[i].layer = layer;
            objects[i].GetComponent<MeshFilter>().sharedMesh = mesh;
            renderers[i] = objects[i].GetComponent<MeshRenderer>(); renderers[i].sharedMaterial = material;
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderers[i].receiveShadows = false; objects[i].SetActive(false);
        }
    }
    public void Play(Vector3 position, Vector3 direction, float size, float duration, float strength, Color? tint = null)
    {
        if (objects == null) return;
        int i = next; next = (next + 1) % objects.Length;
        objects[i].transform.SetPositionAndRotation(position,
            direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : Quaternion.identity);
        objects[i].transform.localScale = Vector3.one * Mathf.Max(.01f, size);
        started[i] = Time.unscaledTime; durations[i] = Mathf.Max(.01f, duration); strengths[i] = strength;
        tints[i] = tint ?? new Color(.86f, .95f, 1f);
        objects[i].SetActive(true); Update();
    }
    public void Update()
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null || !objects[i].activeSelf) continue;
            float age = (Time.unscaledTime - started[i]) / durations[i];
            if (age >= 1) { objects[i].SetActive(false); continue; }
            var tint = tints[i]; tint.a *= strengths[i] * (1 - age);
            block.SetColor("_Tint", tint);
            renderers[i].SetPropertyBlock(block);
        }
    }
    public void Stop() { if (objects != null) foreach (var obj in objects) if (obj != null) obj.SetActive(false); }
    public void Dispose()
    {
        if (objects != null) foreach (var obj in objects) if (obj != null) Object.Destroy(obj);
        if (mesh != null) Object.Destroy(mesh);
        if (material != null) Object.Destroy(material);
    }
}
