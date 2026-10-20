using UnityEngine;

/// <summary> キャラクターの攻撃や被弾時のフィードバックを管理するコンポーネント。 </summary>
[DisallowMultipleComponent]
public sealed class CombatFeedback : MonoBehaviour
{
    [Header("Ghost shimmer")]
    /// <summary> ゴースト表示時の色。 </summary>
    [Tooltip("幽体化中の体色と透明度。")]
    [SerializeField] private Color _ghostTint = new Color(.66f, .78f, .8f, .35f);
    /// <summary>
    /// ゴースト表示時の揺れの大きさ。値が大きいほど揺れが大きくなる。
    /// </summary>
    [Tooltip("幽体化中の輪郭の揺れ幅。大きいほど揺れが目立つ。")]
    [SerializeField, Range(0f, .03f)] private float _ghostSway = .009f;
    /// <summary>
    /// ゴースト表示時の流れる速度。値が大きいほど流れが速くなる。
    /// </summary>
    [Tooltip("幽体化の輪郭を流す速度倍率。")]
    [SerializeField, Range(.1f, 4f)] private float _ghostFlowSpeed = 1.2f;
    /// <summary>Just Avoid成功時の固定残像を表示する時間（秒）。</summary>
    [Tooltip("Just Avoid成功時の固定残像を表示する時間（秒）。")]
    [Header("ジャスト回避")]
    [SerializeField, Min(.01f)] private float _afterImageDuration = .3f;
    /// <summary>Just Avoid成功時の残像Flashの継続時間（秒）。</summary>
    [Tooltip("Just Avoid成功時の残像Flashの継続時間（秒）。")]
    [SerializeField, Min(.01f)] private float _flashDuration = .15f;
    /// <summary>Just Avoid成功時の固定残像の色と初期透明度。</summary>
    [Tooltip("Just Avoid成功時の固定残像の色と初期透明度。")]
    [SerializeField] private Color _afterImageTint = new Color(.75f, .9f, 1f, .3f);
    /// <summary>Just Avoid成功時の残像Flashの色と初期透明度。</summary>
    [Tooltip("Just Avoid成功時の残像Flashの色と初期透明度。")]
    [SerializeField] private Color _flashTint = new Color(.85f, .95f, 1f, .65f);
    /// <summary>命中時に体へ加えるFlashの継続時間（秒）。</summary>
    [Tooltip("命中時に体へ加えるFlashの継続時間（秒）。")]
    [Header("接触フィードバック")]
    [SerializeField, Min(.01f)] private float _hitFlashDuration = .09f;
    /// <summary>Just Avoid成功時の小さなFlashの継続時間（秒）。</summary>
    [Tooltip("Just Avoid成功時の小さなFlashの継続時間（秒）。")]
    [SerializeField, Min(.01f)] private float _justAvoidFlashDuration = .09f;
    /// <summary>Just Avoid成功時の小さなFlashのサイズ（Unity単位）。</summary>
    [Tooltip("Just Avoid成功時の小さなFlashのサイズ（Unity単位）。")]
    [SerializeField, Min(.01f)] private float _justAvoidFlashSize = .35f;
    /// <summary>通常Light命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。</summary>
    [Tooltip("通常Light命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。")]
    [SerializeField, Range(0f, 20f)] private float _lightReactionAngle = 5f;
    /// <summary>通常Heavy命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。</summary>
    [Tooltip("通常Heavy命中で体を局所的に揺らす角度（度）。大きなAnimator Reactionとは別。")]
    [SerializeField, Range(0f, 25f)] private float _heavyReactionAngle = 10f;
    /// <summary>通常命中の局所的な体の反応時間（秒）。</summary>
    [Tooltip("通常命中の局所的な体の反応時間（秒）。")]
    [SerializeField, Min(.01f)] private float _reactionDuration = .18f;
    /// <summary>Enemyが通常Heavyを受けた際の局所反応時間（秒）。</summary>
    [Tooltip("Enemyが通常Heavyを受けた際の局所反応時間（秒）。")]
    [Header("Enemy normal hit presentation only")]
    [SerializeField, Range(.18f, .25f)] private float _enemyHeavyReactionDuration = .22f;
    /// <summary>Enemy攻撃中の局所反応の強さに掛ける倍率。攻撃Poseの崩れを抑える。</summary>
    [Tooltip("Enemy攻撃中の局所反応の強さに掛ける倍率。攻撃Poseの崩れを抑える。")]
    [SerializeField, Range(.3f, 1f)] private float _enemyAttackReactionScale = .55f;
    /// <summary>通常被弾の全反応時間のうち、衝撃が最大になるまでの割合。LightとHeavyの既存継続時間に連動する。</summary>
    [SerializeField, Range(.08f, .3f), Tooltip("通常被弾のImpact割合。Light/Heavyそれぞれの反応時間に掛けます。")]
    private float _enemyReactionImpactFraction = .16f;
    /// <summary>衝撃のピーク後に短い余韻を残す時間の割合。残り時間で元の姿勢へ滑らかに戻る。</summary>
    [SerializeField, Range(0f, .25f), Tooltip("通常被弾の余韻の割合。残り時間が自然な姿勢復帰に使われます。")]
    private float _enemyReactionFollowThroughFraction = .14f;
    /// <summary>EnemyへLightが命中した際の体Flashの強さ。</summary>
    [Tooltip("EnemyへLightが命中した際の体Flashの強さ。")]
    [SerializeField, Range(0f, .5f)] private float _enemyLightFlashStrength = .18f;
    /// <summary>EnemyへHeavyが命中した際の体Flashの強さ。</summary>
    [Tooltip("EnemyへHeavyが命中した際の体Flashの強さ。")]
    [SerializeField, Range(0f, .5f)] private float _enemyHeavyFlashStrength = .28f;
    /// <summary>Just Avoid成功時に体へ加えるFlashの強さ。</summary>
    [Tooltip("Just Avoid成功時に体へ加えるFlashの強さ。")]
    [SerializeField, Range(0f, 1f)] private float _justAvoidBodyFlashStrength = .2f;
    /// <summary>受理された命中から同期して演出する。AI、移動、攻撃状態は変更しない。</summary>
    [SerializeField, Range(.12f, .16f), Tooltip("Just Avoid追撃専用のHitStop。通常Light / Heavyの値は維持する。")]
    private float _justAvoidCounterHitStop = .14f;
    private EnemyController _enemyOwner;
    private EnemyAnimationController _enemyAnimation;
    private Animator _reactionAnimator;
    private EnemyBoneHitReaction _enemyBoneReaction;
    private SkinnedMeshRenderer[] _normalFlashRenderers;
    private Material _normalFlashMaterial;
    private float _normalFlashUntil, _normalFlashLength;
    private bool _normalFlashHeavy;
    private Vector3 _reactionEuler;
    private CameraManager _cameraFeedback;
    private JustAvoidScreenDistortion _screenDistortion;
    private HitStopManager _hitStop;
    private ContactPulse _contactPulse;
    private JustAvoidCounterCutVFX[] _counterCuts;
    private int _nextCounterCut;
    private JustAvoidCounterCutVFX[] _counterFirstCuts;
    private int _nextCounterFirstCut;
    private KatsuroTimedVFX[] _counterIssens;
    private int _nextCounterIssen;
    private ParticleSystem[][] _hitParticles;
    private ParticleSystem.Burst[][][] _hitBursts;
    private float[][] _hitAngles;
    private ParticleSystem.MinMaxCurve[][] _hitSizes;
    private ParticleSystem.MinMaxCurve[][] _hitSpeeds;
    private ParticleSystem.MinMaxCurve[][] _hitLifetimes;
    private ParticleSystem.MinMaxCurve[][] _hitDelays;
    private ParticleSystem.MinMaxGradient[][] _hitColors;
    private float[][] _hitLengths;
    private ParticleSystemRenderer[][] _hitSlashRenderers;
    private Vector3[][] _hitLocalPositions;
    private Quaternion[][] _hitLocalRotations;
    private byte[][] _hitBloodKinds;
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
    private JustAvoidContrast _justAvoidContrast;

    /// <summary>キャラクター表示と命中演出を準備し、Playerには回避演出、Enemyには通常被弾用の骨補正を接続する。</summary>
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
        _reactionAnimator = animator;
        _enemyOwner = GetComponent<EnemyController>();
        if (_enemyOwner != null)
        {
            _enemyAnimation = GetComponent<EnemyAnimationController>();
            _enemyBoneReaction = new EnemyBoneHitReaction(transform, animator);
            PrepareNormalHitFlash(shader);
        }
        _audio = audio;
        _vfxConfig = vfxConfig;
        _cameraFeedback = camera; _hitStop = hitStop;
        if (enableJustAvoid && camera != null) _screenDistortion = camera.GetComponent<JustAvoidScreenDistortion>();
        if (enableJustAvoid && camera != null) _justAvoidContrast = camera.GetComponent<JustAvoidContrast>();
        PrepareContactFeedback();
        // 敵の被弾用コンポーネントには残像用リソースを作らない。
        if (enableJustAvoid && (audio != null || vfxConfig != null)) PrepareJustAvoid(shader);
    }

    /// <summary>元のMeshと骨を共有するEnemy用の光オーバーレイを準備し、元Materialを差し替えず命中を表現する。</summary>
    private void PrepareNormalHitFlash(Shader shader)
    {
        if (shader == null) return;
        _normalFlashMaterial = new Material(shader) { name = "Enemy Normal Hit Overlay" };
        _normalFlashMaterial.SetFloat("_HitRim", 1f);
        _normalFlashRenderers = new SkinnedMeshRenderer[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var source = (SkinnedMeshRenderer)_renderers[i];
            if (source.sharedMesh == null) continue;
            var flash = new GameObject("Normal Hit Flash", typeof(SkinnedMeshRenderer));
            flash.layer = source.gameObject.layer;
            flash.transform.SetParent(source.transform, false);
            var overlay = flash.GetComponent<SkinnedMeshRenderer>();
            overlay.sharedMesh = source.sharedMesh; overlay.bones = source.bones;
            overlay.rootBone = source.rootBone; overlay.localBounds = source.localBounds;
            overlay.sharedMaterials = RepeatedMaterial(_normalFlashMaterial, source.sharedMesh.subMeshCount);
            overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlay.receiveShadows = false; overlay.enabled = false;
            _normalFlashRenderers[i] = overlay;
        }
    }

    /// <summary>攻撃種別と実時間の終了時刻を記録して通常被弾Flashを開始する。</summary>
    private void BeginNormalHitFlash(bool heavy)
    {
        _normalFlashHeavy = heavy;
        _normalFlashLength = _hitFlashDuration * (heavy ? 1f : .7f);
        _normalFlashUntil = FeedbackVisualTime.Now + _normalFlashLength;
    }

    /// <summary>元MeshのBlendShapeを追従し、攻撃種別に応じた光を減衰させて終了時に非表示にする。</summary>
    private void UpdateNormalHitFlash()
    {
        if (_normalFlashMaterial == null) return;
        float fade = Mathf.Clamp01((_normalFlashUntil - FeedbackVisualTime.Now) / Mathf.Max(.01f, _normalFlashLength));
        _normalFlashMaterial.SetColor("_Tint", new Color(1f, .75f, .6f, fade * (_normalFlashHeavy ? _enemyHeavyFlashStrength : _enemyLightFlashStrength)));
        for (int i = 0; i < _normalFlashRenderers.Length; i++)
        {
            var overlay = _normalFlashRenderers[i]; if (overlay == null) continue;
            var source = (SkinnedMeshRenderer)_renderers[i];
            overlay.enabled = fade > 0f && source != null && source.enabled && source.gameObject.activeInHierarchy;
            if (overlay.enabled) for (int shape = 0; shape < source.sharedMesh.blendShapeCount; shape++) overlay.SetBlendShapeWeight(shape, source.GetBlendShapeWeight(shape));
        }
    }

    /// <summary>通常被弾の骨補正と光を解除し、大被弾・死亡へ持ち越さない。</summary>
    public void CancelNormalHitReaction()
    {
        _enemyAnimation?.CancelNormalHitPose();
        _enemyBoneReaction?.Clear();
        _normalFlashUntil = 0f;
        UpdateNormalHitFlash();
    }

    /// <summary>成功時の固定残像・Flash・任意Shockwaveに使用するMeshとMaterialを準備する。</summary>
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

    /// <summary>Meshのサブメッシュ数に合わせて同一Materialの参照配列を作る。</summary>
    /// <returns>サブメッシュに割り当てるMaterial配列。</returns>
    private Material[] RepeatedMaterial(Material material, int count)
    {
        var materials = new Material[count];
        for (int i = 0; i < count; i++) materials[i] = material;
        return materials;
    }

    /// <summary>成立通知から一度だけ呼ぶ。判定、ゲージ、スローには触れない。</summary>
    public void PlayJustAvoidFeedback() => PlayJustAvoidFeedback(new DamageInfo(0, transform.position + Vector3.up, transform.forward, null, null));
    /// <summary>既定の接触位置で共通の成功演出を要求する。成功判定や追撃受付は変更しない。</summary>
    public void PlayJustAvoidFeedback(DamageInfo info)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        StopJustAvoid();
        _justAvoidStarted = Time.unscaledTime;
        _justAvoidPlaying = true;
        _justAvoidContrast?.Play(_audio);
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

    /// <summary>成功からの実経過時間に応じて残像・Flash・Shockwaveを減衰し、終了後に表示を解除する。</summary>
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

    /// <summary>Shockwaveの粒子を停止・消去して表示オブジェクトを非表示にする。</summary>
    private void StopShockwave()
    {
        if (_shockwaveRoot == null || !_shockwaveRoot.activeSelf) return;
        foreach (var particle in _shockwaveParticles)
            if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        _shockwaveRoot.SetActive(false);
    }

    /// <summary>回避用の残像とFlash、Shockwaveを停止する。新しい成功の色・音の時計は別の所有者が管理する。</summary>
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
    public void Hit()
    {
        if (_enemyOwner != null)
        {
            _enemyBoneReaction?.Begin(new DamageInfo(0f, transform.position, -transform.forward, null, null),
                _lightReactionAngle, _reactionDuration, _enemyOwner.IsAttackAnimationActive ? _enemyAttackReactionScale : 1f,
                _enemyReactionImpactFraction, _enemyReactionFollowThroughFraction);
            _hitUntil = 0f; BeginNormalHitFlash(false);
        }
        else { _reactionEuler = new Vector3(-_lightReactionAngle, 0, 0); _hitUntil = Time.unscaledTime + _reactionDuration; }
        Refresh();
    }

    /// <summary>攻撃情報または既定の被弾で、音・HitStop・接触VFXと必要な骨反応を開始する。</summary>
    public void Hit(DamageInfo info, bool useBoneReaction = true)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        // プレイヤー自身が被弾した場合も、追撃倍率を残さない。
        GetComponent<JustAvoidCounterAnimation>()?.ResetSpeed();
        float angle = info.IsHeavy ? _heavyReactionAngle : _lightReactionAngle;
        bool strongFeedback = info.IsHeavy || info.IsJustAvoidCounter;
        var counterPresentation = _vfxConfig != null ? _vfxConfig.CounterPresentation : null;
        bool bloodOnly = info.IsJustAvoidCounter && counterPresentation != null && counterPresentation.IsBloodOnlyDebug;
        Vector3 counterBloodDirection = info.SlashDirection;
        if (info.IsJustAvoidCounter && info.Instigator != null)
        {
            info.Instigator.GetComponent<JustAvoidCounterAnimation>()?.OnCounterHit();
            var trail = info.Instigator.GetComponentInChildren<SwordTrail>(true);
            Vector3 measured = trail != null ? trail.NotifyCounterHit(info.CounterStrike, info.HitPoint, info.SlashDirection,
                showImpactStroke: _vfxConfig == null || _vfxConfig.CounterIssenSlash == null) : Vector3.zero;
            if (measured.sqrMagnitude > .0001f) counterBloodDirection = measured;
        }
        if (_enemyOwner != null)
        {
            _enemyAnimation?.DiagnoseNormalHitRequest(info, useBoneReaction);
            bool normal = !info.IsJustAvoidCounter;
            if (normal && useBoneReaction)
            {
                float attackScale = _enemyOwner.IsAttackAnimationActive ? _enemyAttackReactionScale : 1f;
                if (info.IsHeavy && _enemyAnimation != null && _enemyAnimation.TryPlayNormalHeavyPose(attackScale))
                    angle *= _enemyAnimation.NormalHitProceduralScale;
                _enemyBoneReaction?.Begin(info, angle, info.IsHeavy ? _enemyHeavyReactionDuration : _reactionDuration,
                    attackScale,
                    _enemyReactionImpactFraction, _enemyReactionFollowThroughFraction);
                // 命中したフレームから接触姿勢を見せ、その後のHitStop中は同じ補正を保持する。
                _enemyBoneReaction?.Apply();
            }
            else CancelNormalHitReaction();
            if (normal)
            {
                _hitUntil = 0f;
                if (_enemyAnimation == null || !_enemyAnimation.IsReacting) BeginNormalHitFlash(info.IsHeavy);
            }
            else
            {
                // 追撃専用の被弾Poseを全身の旧Flashで覆わず、閃光の中心を接触点へ限定する。
                _hitUntil = 0f;
                if (!bloodOnly) BeginNormalHitFlash(!info.IsCounterFirst);
            }
        }
        else
        {
            Vector3 direction = transform.InverseTransformDirection(info.HitNormal.normalized);
            if (!useBoneReaction) RemoveOffset();
            _reactionEuler = useBoneReaction ? new Vector3(-direction.z * angle, 0, direction.x * angle) : Vector3.zero;
            _hitUntil = Time.unscaledTime + Mathf.Max(.01f, _reactionDuration);
        }
        Refresh();
        Vector3 contact = _vfxConfig != null ? _vfxConfig.GetContactFlashStyle(info.IsHeavy, info.IsJustAvoidCounter) : new Vector3(.075f, .025f, .3f);
        if (info.IsCounterFirst && counterPresentation != null) contact = counterPresentation.FirstContact;
        Color contactTint = new Color(.93f, .96f, 1f);
        if (info.IsJustAvoidCounter && !info.IsCounterFirst && _vfxConfig != null) contactTint *= _vfxConfig.CounterContactIntensity;
        contactTint.a = 1f;
        var cuts = info.IsCounterFirst ? _counterFirstCuts : _counterCuts;
        bool dedicatedCut = info.IsJustAvoidCounter && cuts != null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (bloodOnly)
        {
            var source = counterPresentation.GetCFX(info.CounterStrike);
            Debug.Log($"[CounterBlood] Request stage={info.CounterStrike} dedicatedPool={dedicatedCut} prefab={(source != null ? source.name : "NULL")} point={info.HitPoint:F3} direction={counterBloodDirection:F3}", this);
            if (!dedicatedCut) Debug.LogWarning("[CounterBlood] Dedicated Blood pool missing. Mixed fallback HitVFX is suppressed in BloodOnly; check Counter Config / CFX references.", this);
        }
#endif
        if (dedicatedCut)
        {
            int slot = info.IsCounterFirst ? _nextCounterFirstCut : _nextCounterCut;
            cuts[slot].ConfigureDebug(counterPresentation, info.CounterStrike);
            cuts[slot].Play(info.HitPoint, counterBloodDirection, info.HitNormal);
            if (!bloodOnly && cuts[slot].UsesLocalContactFlash)
                _contactPulse?.Play(info.HitPoint, info.HitNormal, contact.x, contact.y, contact.z, contactTint);
            if (info.IsCounterFirst) _nextCounterFirstCut = (slot + 1) % cuts.Length;
            else _nextCounterCut = (slot + 1) % cuts.Length;
        }
        else if (!bloodOnly)
        {
            _contactPulse?.Play(info.HitPoint, info.HitNormal, contact.x, contact.y, contact.z, contactTint);
            PlayHitVFX(info);
        }
        // 一閃はEnemyが受けたJust Avoid追撃（1撃目・2撃目）のみ。通常攻撃やPlayer被弾では出さない。
        if (_enemyOwner != null && info.IsJustAvoidCounter && !bloodOnly) PlayCounterIssen(info, counterBloodDirection);
        var config = _audio != null ? _audio.AudioConfig : null;
        AudioClip hitClip = config != null ? (info.IsCounterFirst ? config.GetCounterFirstClip() : config.GetHitClip(info.IsHeavy, info.IsJustAvoidCounter)) : null;
        // Final Blowが同じ衝撃Clipをこの命中処理の直後に鳴らす場合は、音を二重に重ねない。
        bool finalBlowOwnsSound = _enemyOwner != null && _enemyOwner.HpRatio <= 0f &&
            FinalBlowManager.Instance != null && FinalBlowManager.Instance.isActiveAndEnabled &&
            hitClip != null && config != null && hitClip == _audio.GetSEClip(config.EnemyDeadSound);
        if (!finalBlowOwnsSound)
        {
            if (hitClip != null) _audio.PlaySE(hitClip, info.IsCounterFirst ? config.CounterFirstHitVolume : config.GetHitVolume(info.IsHeavy, info.IsJustAvoidCounter), info.IsCounterFirst ? config.CounterFirstHitOffset : config.GetHitStartOffset(info.IsHeavy, info.IsJustAvoidCounter));
            else _audio?.PlaySE(config != null ? (strongFeedback ? config.HeavyHitSound : config.LightHitSound) : "Damage");
        }
        if (_hitStop != null)
        {
            float duration = info.IsJustAvoidCounter ? Mathf.Max(_justAvoidCounterHitStop, _hitStop.HeavyHitStop) : info.IsHeavy ? _hitStop.HeavyHitStop : _hitStop.LightHitStop;
            if (info.IsJustAvoidCounter && counterPresentation != null) duration = counterPresentation.GetStrike(info.CounterStrike).HitStop;
            _hitStop.PlayHitStop(duration, gameObject, info.Instigator);
            // 敵の攻撃（RushAttack の刺突線など）を同じ HitStop 時間だけ保持する。
            if (info.Instigator != null && !info.IsJustAvoidCounter)
                info.Instigator.GetComponentInParent<EnemyController>()?.NotifyAttackHitFeedback(duration);
        }
        _enemyAnimation?.DiagnoseNormalHitAfterHitStop(info);
        _cameraFeedback?.PlayHitFeedback(info.IsHeavy, info.HitPoint, info.IsJustAvoidCounter, info.IsCounterFirst);
    }

    /// <summary>VFX設定の命中Prefabと、再利用する接触Flashの表示リソースを準備する。</summary>
    private void PrepareContactFeedback()
    {
        _contactPulse = new ContactPulse(gameObject.layer);
        var counter = _vfxConfig != null ? _vfxConfig.CounterPresentation : null;
        _counterCuts = CreateCounterPool(counter != null ? counter.FinisherCFX : null);
        _counterFirstCuts = CreateCounterPool(counter != null ? counter.FirstCFX : null);
        PrepareCounterIssen();
        if (_vfxConfig == null || _vfxConfig.HitVFX == null) return;
        _hitRoots = new GameObject[4]; _hitParticles = new ParticleSystem[4][]; _hitStarted = new float[4];
        _hitBursts = new ParticleSystem.Burst[4][][];
        _hitAngles = new float[4][]; _hitSizes = new ParticleSystem.MinMaxCurve[4][];
        _hitSpeeds = new ParticleSystem.MinMaxCurve[4][];
        _hitLifetimes = new ParticleSystem.MinMaxCurve[4][];
        _hitDelays = new ParticleSystem.MinMaxCurve[4][];
        _hitColors = new ParticleSystem.MinMaxGradient[4][];
        _hitLengths = new float[4][];
        _hitSlashRenderers = new ParticleSystemRenderer[4][];
        _hitLocalPositions = new Vector3[4][];
        _hitLocalRotations = new Quaternion[4][];
        _hitBloodKinds = new byte[4][];
        for (int i = 0; i < 4; i++)
        {
            _hitRoots[i] = new GameObject("Contact VFX"); _hitRoots[i].SetActive(false);
            var effect = Instantiate(_vfxConfig.HitVFX, _hitRoots[i].transform, false); effect.SetActive(true);
            _hitParticles[i] = effect.GetComponentsInChildren<ParticleSystem>(true);
            _hitBursts[i] = new ParticleSystem.Burst[_hitParticles[i].Length][];
            _hitAngles[i] = new float[_hitParticles[i].Length];
            _hitSizes[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            _hitSpeeds[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            _hitLifetimes[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            _hitDelays[i] = new ParticleSystem.MinMaxCurve[_hitParticles[i].Length];
            _hitColors[i] = new ParticleSystem.MinMaxGradient[_hitParticles[i].Length];
            _hitLengths[i] = new float[_hitParticles[i].Length];
            _hitSlashRenderers[i] = new ParticleSystemRenderer[_hitParticles[i].Length];
            _hitLocalPositions[i] = new Vector3[_hitParticles[i].Length];
            _hitLocalRotations[i] = new Quaternion[_hitParticles[i].Length];
            _hitBloodKinds[i] = new byte[_hitParticles[i].Length];
            for (int j = 0; j < _hitParticles[i].Length; j++)
            {
                var particle = _hitParticles[i][j];
                _hitLocalPositions[i][j] = particle.transform.localPosition;
                _hitLocalRotations[i][j] = particle.transform.localRotation;
                string particleName = particle.name;
                _hitBloodKinds[i][j] = particleName == "BloodStreaks" ? (byte)1 : particleName == "BloodDroplets" ? (byte)2 : (byte)0;
                var emission = particle.emission;
                _hitBursts[i][j] = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(_hitBursts[i][j]);
                _hitAngles[i][j] = particle.shape.angle;
                _hitSizes[i][j] = particle.main.startSize;
                _hitSpeeds[i][j] = particle.main.startSpeed;
                _hitLifetimes[i][j] = particle.main.startLifetime;
                _hitDelays[i][j] = particle.main.startDelay;
                _hitColors[i][j] = particle.main.startColor;
                _hitLengths[i][j] = particle.GetComponent<ParticleSystemRenderer>().lengthScale;
                if (particle.name == "SlashLine") _hitSlashRenderers[i][j] = particle.GetComponent<ParticleSystemRenderer>();
                var main = particle.main;
                main.loop = false; main.playOnAwake = false; main.useUnscaledTime = true; main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }
    /// <summary>追撃の命中点へ、刀の振り方向に沿った一閃をカメラへ向けて出す。Poolは初回だけ作る。</summary>
    private void PlayCounterIssen(DamageInfo info, Vector3 slashDirection)
    {
        var prefab = _vfxConfig != null ? _vfxConfig.CounterIssenSlash : null;
        var view = Camera.main;
        if (prefab == null || view == null) return;
        PrepareCounterIssen();
        if (_counterIssens == null) return;
        var slash = _counterIssens[_nextCounterIssen];
        _nextCounterIssen = (_nextCounterIssen + 1) % _counterIssens.Length;
        Vector3 direction = slashDirection.sqrMagnitude > .0001f ? slashDirection : info.SlashDirection;
        Quaternion basis = KatsuroVFXOrientation.FacingCamera(info.HitPoint, direction, view);
        var style = _vfxConfig.GetCounterIssenStyle(info.IsCounterFirst);
        Vector3 point = info.HitPoint + (style != null ? basis * style.PositionOffset : Vector3.zero);
        slash.Play(point, basis * Quaternion.Euler(style != null ? style.RotationOffset : Vector3.zero),
            _vfxConfig.GetCounterIssenScale(info.IsCounterFirst), style != null ? style.EmissionIntensity : 1f, style != null ? style.Duration : 0f);
    }

    private void PrepareCounterIssen()
    {
        var prefab = _vfxConfig != null ? _vfxConfig.CounterIssenSlash : null;
        if (prefab != null && _counterIssens == null)
        {
            _counterIssens = new KatsuroTimedVFX[2];
            for (int i = 0; i < _counterIssens.Length; i++)
            {
                _counterIssens[i] = Instantiate(prefab);
                foreach (var child in _counterIssens[i].GetComponentsInChildren<Transform>(true)) child.gameObject.layer = gameObject.layer;
                _counterIssens[i].Stop();
            }
        }
    }

    /// <summary>段ごとの専用CFXを初期化時に固定数だけ作り、命中時の生成と破棄を避ける。</summary>
    private JustAvoidCounterCutVFX[] CreateCounterPool(JustAvoidCounterCutVFX prefab)
    {
        if (prefab == null) return null;
        var pool = new JustAvoidCounterCutVFX[4];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = Instantiate(prefab);
            foreach (var child in pool[i].GetComponentsInChildren<Transform>(true)) child.gameObject.layer = gameObject.layer;
            pool[i].Stop();
            if (_vfxConfig != null && _vfxConfig.CounterIssenSlash != null) pool[i].UseExternalSlash();
        }
        return pool;
    }
    /// <summary>再利用する命中Prefabを接触位置へ配置し、攻撃種別に応じた粒子量と速度で再生する。</summary>
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
        _hitRoots[i].SetActive(true); _hitStarted[i] = FeedbackVisualTime.Now;
        for (int j = 0; j < _hitParticles[i].Length; j++)
        {
            var particle = _hitParticles[i][j];
            if (particle == null) continue;
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            // 再利用時もPrefabの初期値から計算し、強攻撃の倍率が蓄積しないようにする。
            bool slash = _hitSlashRenderers[i][j] != null;
            byte bloodKind = _hitBloodKinds[i][j];
            bool normalBlood = !info.IsJustAvoidCounter && _enemyOwner != null && bloodKind != 0;
            var bloodStyle = normalBlood ? _vfxConfig.GetNormalBloodStyle(info.IsHeavy) : null;
            // Always restore cached poses before applying a Blood-only presentation offset.
            if (!slash)
            {
                particle.transform.localPosition = _hitLocalPositions[i][j];
                particle.transform.localRotation = _hitLocalRotations[i][j];
            }
            float amount = slash ? 1f : info.IsJustAvoidCounter ? _vfxConfig.CounterHitVFXAmount : strongFeedback ? _vfxConfig.HeavyHitVFXAmount : 1f;
            if (info.IsCounterFirst && !slash && _vfxConfig.CounterPresentation != null) amount *= _vfxConfig.CounterPresentation.FirstBloodAmount;
            // 煙状の血だけ控えめにし、被弾姿勢と接触位置を隠さない。
            if (particle.name == "BloodMist") amount *= .5f;
            if (info.IsJustAvoidCounter && particle.name == "BloodMist") amount *= .25f;
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
                if (normalBlood && b == 0)
                    burst.count = new ParticleSystem.MinMaxCurve(Mathf.Max(0, bloodKind == 1 ? bloodStyle.StreakCount : bloodStyle.DropletCount));
                emission.SetBurst(b, burst);
            }
            var shape = particle.shape;
            // 斬撃線はConeの乱数で曲げず、取得した刀の進行方向へ向ける。
            shape.angle = slash ? 0f : _hitAngles[i][j] * (info.IsJustAvoidCounter ? _vfxConfig.CounterHitVFXSpread : strongFeedback ? _vfxConfig.HeavyHitVFXSpread : 1f);
            var main = particle.main;
            main.startSize = ScaleHitCurve(_hitSizes[i][j], info.IsJustAvoidCounter ? _vfxConfig.CounterHitVFXSize : strongFeedback ? _vfxConfig.HeavyHitVFXSize : 1f);
            main.startSpeed = ScaleHitCurve(_hitSpeeds[i][j], info.IsJustAvoidCounter ? _vfxConfig.CounterHitVFXSpeed : strongFeedback ? _vfxConfig.HeavyHitVFXSpeed : 1f);
            main.startLifetime = ScaleHitCurve(_hitLifetimes[i][j], info.IsJustAvoidCounter ? _vfxConfig.CounterBloodLifetime : info.IsHeavy ? 1.05f : 1f);
            main.startColor = _hitColors[i][j];
            main.startDelay = _hitDelays[i][j];
            if (info.IsJustAvoidCounter && !slash && particle.name != "BloodMist") main.startColor = _vfxConfig.CounterBloodTint;
            if (info.IsJustAvoidCounter && particle.name == "BloodStreaks") main.startDelay = 0f;
            if (slash)
            {
                Vector3 style = _vfxConfig.GetSlashLineStyle(info.IsHeavy, info.IsJustAvoidCounter);
                if (info.IsCounterFirst) style = _vfxConfig.GetSlashLineStyle(false, false);
                float rootScale = _hitRoots[i].transform.localScale.x;
                main.startSize = style.y / Mathf.Max(.01f, rootScale);
                main.startLifetime = style.z;
                _hitSlashRenderers[i][j].lengthScale = style.x / style.y;
                main.startColor = info.IsHeavy && !info.IsJustAvoidCounter ? new Color(.97f, .9f, .75f, .65f) : new Color(.88f, .94f, 1f, info.IsJustAvoidCounter ? .85f : .6f);
            }
            else
            {
                // Pool再利用ごとに元の長さへ戻し、追撃の筋だけ方向に沿って伸ばす。
                var renderer = particle.GetComponent<ParticleSystemRenderer>();
                renderer.lengthScale = _hitLengths[i][j] * (info.IsJustAvoidCounter && particle.name == "BloodStreaks" ? _vfxConfig.CounterBloodStreakLength : 1f);
                if (normalBlood)
                {
                    main.startSize = ScaleHitCurve(_hitSizes[i][j], bloodStyle.Width);
                    main.startSpeed = ScaleHitCurve(_hitSpeeds[i][j], bloodStyle.Speed);
                    main.startLifetime = ScaleHitCurve(_hitLifetimes[i][j], bloodStyle.Lifetime);
                    renderer.lengthScale = _hitLengths[i][j] * bloodStyle.Length;
                    Vector3 bloodDirection = JustAvoidCounterCutVFX.ResolveBloodDirection(direction, info.HitNormal, bloodStyle.DirectionBias);
                    Vector3 bloodPoint = JustAvoidCounterCutVFX.ResolveBloodSpawnPoint(info.HitPoint, direction, info.HitNormal, bloodStyle.SpawnOffset);
                    Vector3 bloodUp = Mathf.Abs(Vector3.Dot(bloodDirection, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
                    particle.transform.SetPositionAndRotation(bloodPoint, Quaternion.LookRotation(bloodDirection, bloodUp));
                }
            }
            particle.Play(false);
        }
    }
    /// <summary>元のParticleカーブを保持しながら指定倍率を反映する。</summary>
    /// <returns>倍率を反映した粒子用カーブ。</returns>
    private static ParticleSystem.MinMaxCurve ScaleHitCurve(ParticleSystem.MinMaxCurve value, float scale)
    {
        if (value.mode == ParticleSystemCurveMode.Constant) value.constant *= scale;
        else if (value.mode == ParticleSystemCurveMode.TwoConstants)
        { value.constantMin *= scale; value.constantMax *= scale; }
        else value.curveMultiplier *= scale;
        return value;
    }

    /// <summary>指定スロットの粒子を停止・消去し、命中表示を非表示にする。</summary>
    private void StopHitVFX(int i)
    {
        foreach (var particle in _hitParticles[i]) if (particle != null) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_hitRoots[i] != null) _hitRoots[i].SetActive(false);
    }

    /// <summary>前フレームの骨補正を除去し、被弾・回避・接触演出の終了時刻と表示状態を更新する。</summary>
    private void Update() 
    { 
        RemoveOffset(); 
        _enemyBoneReaction?.RemoveOffsets();
        Refresh(); 
        UpdateJustAvoid();
        _contactPulse?.Update();
        UpdateHitVFX();
        if (_counterCuts != null) foreach (var cut in _counterCuts) if (cut != null) cut.Tick();
        if (_counterFirstCuts != null) foreach (var cut in _counterFirstCuts) if (cut != null) cut.Tick();
    }

    /// <summary>AnimatorだけのHitStopでは粒子を進め、ゲーム全体のPauseでは遅延を含め粒子と寿命時計を止める。</summary>
    private void UpdateHitVFX()
    {
        if (_hitRoots == null) return;
        float now = FeedbackVisualTime.Now;
        bool paused = FeedbackVisualTime.IsPaused;
        for (int i = 0; i < _hitRoots.Length; i++)
        {
            if (!_hitRoots[i].activeSelf) continue;
            foreach (var particle in _hitParticles[i])
            {
                if (paused && particle.isPlaying) particle.Pause(false);
                else if (!paused && particle.isPaused) particle.Play(false);
            }
            if (now - _hitStarted[i] >= _vfxConfig.HitVFXDuration) StopHitVFX(i);
        }
    }

    /// <summary>Animator更新後の姿勢へキャラクター別の被弾補正を重ねる。Enemyの通常反応は死亡・大被弾で解除する。</summary>
    private void LateUpdate()
    {
        if (_enemyOwner != null)
        {
            if (_enemyOwner.HpRatio <= 0f || (_enemyAnimation != null && _enemyAnimation.IsReacting)) _enemyBoneReaction?.Clear();
            _enemyBoneReaction?.Apply();
            _enemyBoneReaction?.Tick(Time.deltaTime, _reactionAnimator != null ? _reactionAnimator.speed : 1f);
            UpdateNormalHitFlash();
            return;
        }
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

    /// <summary>敗北・勝利・タイトル遷移時の通常被弾補正を解除する通知を購読する。</summary>
    private void OnEnable() => GameManager.OnGameStateChanged += OnGameStateChanged;

    /// <summary>戦闘終了時だけ通常の骨反応を消去する。Pauseでは解除せず、専用Reactionの状態制御には触れない。</summary>
    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (_enemyOwner != null && (state == GameManager.GameState.Defeat ||
            state == GameManager.GameState.Victory || state == GameManager.GameState.Title))
            CancelNormalHitReaction();
    }

    /// <summary>回避・命中・骨補正を停止してMaterialと表示状態を通常へ戻す。</summary>
    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        _justAvoidContrast?.Cancel();
        StopJustAvoid();
        _contactPulse?.Stop();
        if (_counterCuts != null) foreach (var cut in _counterCuts) if (cut != null) cut.Stop();
        if (_counterFirstCuts != null) foreach (var cut in _counterFirstCuts) if (cut != null) cut.Stop();
        if (_hitRoots != null) for (int i = 0; i < _hitRoots.Length; i++) StopHitVFX(i);
        RemoveOffset();
        CancelNormalHitReaction();
        _ghost = false; _hitUntil = 0;
        Refresh();
    }

    /// <summary>生成したFlash・残像・ParticleオブジェクトとMesh・Materialを解放する。</summary>
    private void OnDestroy() 
    { 
        _enemyBoneReaction?.Clear();
        if (_normalFlashRenderers != null) foreach (var overlay in _normalFlashRenderers) if (overlay != null) Destroy(overlay.gameObject);
        if (_normalFlashMaterial != null) Destroy(_normalFlashMaterial);
        _contactPulse?.Dispose();
        if (_counterCuts != null) foreach (var cut in _counterCuts) if (cut != null) Destroy(cut.gameObject);
        if (_counterFirstCuts != null) foreach (var cut in _counterFirstCuts) if (cut != null) Destroy(cut.gameObject);
        if (_counterIssens != null) foreach (var slash in _counterIssens) if (slash != null) Destroy(slash.gameObject);
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
    /// <summary>接触Flash用Mesh・Materialと固定数の表示スロットを準備する。</summary>
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
    /// <summary>次の再利用スロットを接触位置へ配置し、サイズ・色・実時間の寿命を設定する。</summary>
    public void Play(Vector3 position, Vector3 direction, float size, float duration, float strength, Color? tint = null)
    {
        if (objects == null) return;
        int i = next; next = (next + 1) % objects.Length;
        objects[i].transform.SetPositionAndRotation(position,
            direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : Quaternion.identity);
        objects[i].transform.localScale = Vector3.one * Mathf.Max(.01f, size);
        started[i] = FeedbackVisualTime.Now; durations[i] = Mathf.Max(.01f, duration); strengths[i] = strength;
        tints[i] = tint ?? new Color(.86f, .95f, 1f);
        objects[i].SetActive(true); Update();
    }
    /// <summary>有効な接触Flashの実時間に応じて透明度を下げ、寿命を過ぎたスロットを非表示にする。</summary>
    public void Update()
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null || !objects[i].activeSelf) continue;
            float age = (FeedbackVisualTime.Now - started[i]) / durations[i];
            if (age >= 1) { objects[i].SetActive(false); continue; }
            var tint = tints[i]; tint.a *= strengths[i] * (1 - age);
            block.SetColor("_Tint", tint);
            renderers[i].SetPropertyBlock(block);
        }
    }
    /// <summary>接触Flashの全スロットを即座に非表示にする。</summary>
    public void Stop() { if (objects != null) foreach (var obj in objects) if (obj != null) obj.SetActive(false); }
    /// <summary>接触Flashの表示オブジェクトと所有するMesh・Materialを解放する。</summary>
    public void Dispose()
    {
        if (objects != null) foreach (var obj in objects) if (obj != null) Object.Destroy(obj);
        if (mesh != null) Object.Destroy(mesh);
        if (material != null) Object.Destroy(material);
    }
}
