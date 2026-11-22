using UnityEngine;

/// <summary>追撃各段の接触光・切断線・方向血。段ごとのPrefabを固定プールから再生する。</summary>
[DisallowMultipleComponent]
public sealed class JustAvoidCounterCutVFX : MonoBehaviour
{
    private static readonly int AgeId = Shader.PropertyToID("_Age");
    [SerializeField] private MeshRenderer _cutRenderer;
    [SerializeField, Tooltip("刀軌跡Slashを主役にし、このPrefabの旧Cut Quadを非表示にする。Bloodは維持し、既存ContactPulseで局所光を再生します。")]
    private bool _useLocalContactFlash;
    public bool UsesLocalContactFlash => _useLocalContactFlash;
    public void UseExternalSlash()
    {
        _useLocalContactFlash = true;
        if (_cutRenderer != null)
            _cutRenderer.enabled = false;
    }
    [SerializeField] private ParticleSystem[] _blood;
    [SerializeField, Min(.01f), Tooltip("プールを返却するまでの秒数。Bloodの寿命も含むため、Cutだけの減衰は専用Materialで調整します。")]
    private float _duration = .38f;
    [Header("Cut footprint (each strike prefab)")]
    [SerializeField, Tooltip("X: Cutの最大長さ(m)。Y: 発光を収める面の高さ(m)。芯の太さはMaterialのCore widthで調整します。")]
    private Vector2 _cutSize = new Vector2(1.45f, .44f);
    [SerializeField, Range(0f, 1f), Tooltip("奥行き方向の斬りでもCutの投影長が潰れない下限。血とSwordTrailの軌道は変えない。")]
    private float _minimumProjectedLength = .7f;
    [SerializeField, Range(0f, .06f), Tooltip("HitPointから視線側へのCut専用の微小オフセット(m)。深度LEqualで面の干渉を緩和します。大きいと浮くため最小限にします。Bloodとダメージ座標は動かしません。")]
    private float _surfaceOffset = .025f;
    [SerializeField, Range(0f, .5f), Tooltip("血の進行軸だけを攻撃者から離れる側へ少量傾けます。斬撃方向に直交する成分を使用。HitPoint・旧Cut表示方向は変更しません。")]
    private float _bloodOutwardBias;
    [SerializeField, Tooltip("Bloodだけの表示位置。X=斬撃の横成分に沿う接線、Y=上、Z=攻撃者から離れる水平軸(m)。合計最大0.10m。0で実HitPointを維持。Cut/判定は移動しません。")]
    private Vector3 _bloodSpawnOffset;
    private MaterialPropertyBlock _block;
    private float _started;
    private bool _playing;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private JustAvoidCounterConfig _debugConfig;
    private JustAvoidCounterStrike _debugStrike;
    private int _debugSample;
#endif

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void ConfigureDebug(JustAvoidCounterConfig config, JustAvoidCounterStrike strike)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _debugConfig = config;
        _debugStrike = strike;
#endif
    }

    // 最初の命中を処理する前に、プールのインスタンス生成時に確保する。
    private void Awake()
    {
        if (_block == null)
            _block = new MaterialPropertyBlock();
    }

    public void Play(Vector3 point, Vector3 slash, Vector3 normal, Camera viewingCamera = null)
    {
        LogBloodSnapshot("プールから取得");
        Stop();
        if (_block == null)
            _block = new MaterialPropertyBlock();
        if (slash.sqrMagnitude < .0001f)
            slash = normal.sqrMagnitude > .0001f ? normal : Vector3.forward;
        slash.Normalize();
        Vector3 bloodDirection = ResolveBloodDirection(slash, normal, _bloodOutwardBias);
        Vector3 bloodUp = Mathf.Abs(Vector3.Dot(bloodDirection, Vector3.up)) > .98f ? Vector3.forward : Vector3.up;
        Vector3 bloodPoint = ResolveBloodSpawnPoint(point, slash, normal, _bloodSpawnOffset);
        transform.SetPositionAndRotation(bloodPoint, Quaternion.LookRotation(bloodDirection, bloodUp));
        // 血しぶきは実際の刀身の向きに沿わせ、細い光はカメラへ向ける。
        var camera = viewingCamera != null ? viewingCamera : Camera.main;
        Vector3 facing = camera != null ? (camera.transform.position - point).normalized : -normal.normalized;
        if (facing.sqrMagnitude < .0001f)
            facing = Vector3.back;
        // 命中を強調する斬線は、刀身の軌道を画面に投影した向きに合わせる。
        // 奥行き方向の斬撃にも画面上で判別できる長さを付け、伸びすぎないよう制限する。
        Vector3 projected = Vector3.ProjectOnPlane(slash, facing);
        float projection = projected.magnitude;
        if (projection < .001f)
            projected = Vector3.ProjectOnPlane(normal, facing);
        if (projected.sqrMagnitude < .000001f)
            projected = camera != null ? camera.transform.right : Vector3.Cross(facing, Vector3.up);
        if (projected.sqrMagnitude < .000001f)
            projected = Vector3.right;
        Vector3 right = projected.normalized;
        _cutRenderer.transform.SetPositionAndRotation(point + facing * _surfaceOffset, Quaternion.LookRotation(facing, Vector3.Cross(facing, right)));
        _cutRenderer.transform.localScale = new Vector3(
            _cutSize.x * Mathf.Max(_minimumProjectedLength, projection),
            _cutSize.y,
            1f);
        _started = FeedbackVisualTime.Now;
        _playing = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _debugSample = 0;
        _cutRenderer.forceRenderingOff = _debugConfig != null && _debugConfig.IsBloodOnlyDebug;
#endif
        gameObject.SetActive(true);
        LogBloodSnapshot("ルートを有効化");
        // 専用プレハブはどちらも、3 個の血しぶきオブジェクトを非アクティブで保存している。
        // ルート直下の子なので、ルートの有効化に加えて各オブジェクトの有効化も必要。
        // セット済みのワールド姿勢と playOnAwake=false を維持し、下の処理で再生を開始する。
        ActivateBloodObjects();
        LogBloodSnapshot("血しぶきを有効化");
        _cutRenderer.enabled = !_useLocalContactFlash;
        foreach (var ps in _blood)
            ps.Play(false);
        LogBloodSnapshot("再生");
        Tick();
    }

    private void ActivateBloodObjects()
    {
        foreach (var ps in _blood)
            ps.gameObject.SetActive(true);
    }

    public void Tick()
    {
        if (!_playing) return;
        float age = FeedbackVisualTime.Now - _started;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool bloodOnly = _debugConfig != null && _debugConfig.IsBloodOnlyDebug;
        _cutRenderer.forceRenderingOff = bloodOnly;
        while (bloodOnly && _debugSample < 3 && age >= (_debugSample == 0 ? .05f : _debugSample == 1 ? .1f : .2f))
        {
            _debugSample++;
            LogBloodSnapshot(_debugSample == 1 ? "50 ms 後" : _debugSample == 2 ? "100 ms 後" : "200 ms 後");
        }
#endif
        if (age >= _duration)
        {
            Stop();
            return;
        }
        _block.SetFloat(AgeId, age);
        _cutRenderer.SetPropertyBlock(_block);
        foreach (var ps in _blood)
        {
            if (FeedbackVisualTime.IsPaused && ps.isPlaying)
                ps.Pause(false);
            else if (!FeedbackVisualTime.IsPaused && ps.isPaused)
                ps.Play(false);
        }
    }

    public void Stop([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        _playing = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_cutRenderer != null)
            _cutRenderer.forceRenderingOff = false;
        _debugSample = 3;
#endif
        if (_blood != null)
            foreach (var ps in _blood)
                if (ps != null)
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        LogBloodSnapshot("停止・消去・無効化", caller);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogBloodSnapshot(string phase, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_debugConfig == null || !_debugConfig.IsBloodOnlyDebug) return;
        // メモリ確保と Renderer の取得は、診断対象のタイミングに限る。
        var text = new System.Text.StringBuilder(1024);
        var camera = Camera.main;
        text.Append($"[CounterBlood] {phase} 呼び出し元={caller} 段={_debugStrike} インスタンス={name} ID={GetEntityId()} 経過={FeedbackVisualTime.Now - _started:F3}s ルート有効={gameObject.activeSelf} ルート階層内で有効={gameObject.activeInHierarchy}" +
            $" 位置={transform.position:F3} 方向={transform.forward:F3}");
        if (camera != null)
            text.Append($" カメラ={camera.name}" +
                $" ビューポート={camera.WorldToViewportPoint(transform.position):F3} カメラ距離={Vector3.Distance(camera.transform.position, transform.position):F2}");
        else
            text.Append(" カメラ未設定");
        if (_blood == null)
            text.Append("\n血しぶき配列が未設定");
        else
            foreach (var ps in _blood)
            {
                if (ps == null)
                {
                    text.Append("\nParticleSystem が未設定");
                    continue;
                }
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                var material = renderer != null ? renderer.sharedMaterial : null;
                var shader = material != null ? material.shader : null;
                text.Append($"\n{ps.name}: 血しぶき有効={ps.gameObject.activeSelf} 血しぶき階層内で有効={ps.gameObject.activeInHierarchy}" +
                    $" 再生中={ps.isPlaying} 放出中={ps.isEmitting} 一時停止中={ps.isPaused}" +
                    $" 粒子数={ps.particleCount} 放出モジュール有効={ps.emission.enabled}");
                text.Append($" レンダラー={(renderer != null ? renderer.enabled.ToString() : "NULL")} 強制非表示={(renderer != null && renderer.forceRenderingOff)}" +
                    $" マテリアル={(material != null ? material.name : "NULL")} シェーダー={(shader != null ? shader.name : "NULL")} 対応={(shader != null && shader.isSupported)}");
                if (camera != null)
                    text.Append($" カメラ描画対象={((camera.cullingMask & (1 << ps.gameObject.layer)) != 0)}");
            }
        Debug.Log(text.ToString(), this);
#endif
    }

    // 成立したプレイヤーのカウンターの HitNormal は、攻撃者から命中点へ向く。
    // 刀身の軌跡と直交する成分だけを加え、斬線の向きと始点を保つ。
    public static Vector3 ResolveBloodDirection(Vector3 slash, Vector3 awayFromAttacker, float bias)
    {
        if (slash.sqrMagnitude < .0001f)
            return awayFromAttacker.sqrMagnitude > .0001f ? awayFromAttacker.normalized : Vector3.forward;
        slash.Normalize();
        Vector3 outward = Vector3.ProjectOnPlane(awayFromAttacker.normalized, slash);
        return (slash + outward * Mathf.Clamp(bias, 0f, .5f)).normalized;
    }

    public static Vector3 ResolveBloodSpawnPoint(Vector3 point, Vector3 slash, Vector3 awayFromAttacker, Vector3 offset)
    {
        Vector3 forward = Vector3.ProjectOnPlane(awayFromAttacker, Vector3.up);
        if (forward.sqrMagnitude < .0001f)
            forward = Vector3.ProjectOnPlane(slash, Vector3.up);
        if (forward.sqrMagnitude < .0001f) return point; // No measured horizontal basis: do not invent an offset axis.
        forward.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, forward);
        if (Vector3.Dot(tangent, slash) < 0f)
            tangent = -tangent;
        offset = Vector3.ClampMagnitude(offset, .1f);
        return point + tangent * offset.x + Vector3.up * offset.y + forward * offset.z;
    }
}
