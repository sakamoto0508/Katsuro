using UnityEngine;

/// <summary>刀身端点の履歴を共有し、通常の細い軌跡と追撃専用の白銀Ribbonを描く。</summary>
[DisallowMultipleComponent]
public sealed class SwordTrail : MonoBehaviour
{
    /// <summary>通常・強攻撃・追撃で刀の軌跡の持続時間と表示量を選び分ける。</summary>
    public enum AttackStyle { Light, Heavy, JustAvoidCounter }
    [SerializeField] private JustAvoidCounterConfig _counterPresentation;
    private JustAvoidCounterStrike counterStrike;
    /// <summary>通常攻撃の刀軌跡を残す時間（秒）。</summary>
    [UnityEngine.Tooltip("通常攻撃の刀軌跡を残す時間（秒）。")]
    [Header("刀身の軌跡")]
    [SerializeField, Min(.01f)] private float _duration = .11f;
    /// <summary>刀身の根元を示すTransform。刃先との間を軌跡の幅として使う。</summary>
    [UnityEngine.Tooltip("刀身の根元を示すTransform。刃先との間を軌跡の幅として使う。")]
    [SerializeField] private Transform _bladeBase;
    /// <summary>刀の刃先を示すTransform。刀身の軌跡の外側を生成する。</summary>
    [UnityEngine.Tooltip("刀の刃先を示すTransform。刀身の軌跡の外側を生成する。")]
    [SerializeField] private Transform _tip;
    /// <summary>実Blade BaseからTipへ寄せる割合。0.48なら刀身の外側52%だけを使用する。</summary>
    [Tooltip("実Blade BaseからTipへ寄せる割合。0.48なら刀身の外側52%だけを使用する。")]
    [SerializeField, Range(0f, 1f)] private float _bladeTrailStart = .48f;
    /// <summary>同じ刀の複数Colliderは、代表となるSwordTrailを共有する。</summary>
    [Tooltip("同じ刀の複数Colliderは、代表となるSwordTrailを共有する。")]
    [SerializeField] private SwordTrail _sharedTrail;
    /// <summary>刀軌跡を描画するMaterial参照。未設定なら対応ShaderからMaterialを生成する。</summary>
    [UnityEngine.Tooltip("刀軌跡を描画するMaterial参照。未設定なら対応ShaderからMaterialを生成する。")]
    [SerializeField] private Material _trailMaterial;
    /// <summary>通常攻撃の軌跡の明るさ倍率。</summary>
    [UnityEngine.Tooltip("通常攻撃の軌跡の明るさ倍率。")]
    [SerializeField, Range(0f, 2f)] private float _brightness = .8f;
    /// <summary>時間経過で軌跡を消すFade曲線の指数。</summary>
    [UnityEngine.Tooltip("時間経過で軌跡を消すFade曲線の指数。")]
    [SerializeField, Min(.1f)] private float _fade = 1.5f;
    /// <summary>刀軌跡の基準色と透明度。</summary>
    [UnityEngine.Tooltip("刀軌跡の基準色と透明度。")]
    [SerializeField] private Color _color = new Color(.78f, .9f, 1f, .32f);
    /// <summary>軌跡を出し始める刀の最低速度（Unity単位/秒）。低速の三角形を抑える。</summary>
    [UnityEngine.Tooltip("軌跡を出し始める刀の最低速度（Unity単位/秒）。低速の三角形を抑える。")]
    [Header("振りの速度 / 攻撃差")]
    [SerializeField, Min(0f)] private float _minimumSpeed = 3.5f;
    /// <summary>軌跡の表示強度が最大になる刀の速度（Unity単位/秒）。</summary>
    [UnityEngine.Tooltip("軌跡の表示強度が最大になる刀の速度（Unity単位/秒）。")]
    [SerializeField, Min(.1f)] private float _fullSpeed = 16f;
    /// <summary>Heavy攻撃の刀軌跡を残す時間（秒）。</summary>
    [UnityEngine.Tooltip("Heavy攻撃の刀軌跡を残す時間（秒）。")]
    [SerializeField, Min(.01f)] private float _heavyDuration = .13f;
    /// <summary>Just Avoid追撃の刀軌跡を残す時間（秒）。</summary>
    [UnityEngine.Tooltip("Just Avoid追撃の刀軌跡を残す時間（秒）。")]
    [SerializeField, Min(.01f)] private float _counterDuration = .15f;
    /// <summary>通常の明るさへ掛けるHeavy攻撃の追加倍率。</summary>
    [UnityEngine.Tooltip("通常の明るさへ掛けるHeavy攻撃の追加倍率。")]
    [SerializeField, Range(1f, 2f)] private float _heavyBrightness = 1.18f;
    /// <summary>通常の明るさへ掛けるJust Avoid追撃の追加倍率。</summary>
    [UnityEngine.Tooltip("通常の明るさへ掛けるJust Avoid追撃の追加倍率。")]
    [SerializeField, Range(1f, 2f)] private float _counterBrightness = 1.35f;
    /// <summary>追撃の振りのピークだけに掛ける細いコアの強度。残光や幅には掛けない。</summary>
    [SerializeField, Range(0f, 4f), Tooltip("追撃の高速区間だけ強まる細いコア。軌跡全体は明るくしません。")]
    private float _counterCoreIntensity = 2.6f;
    /// <summary>追撃の明るいコアが消える実時間。残光の寿命とは独立する。</summary>
    [SerializeField, Range(.015f, .06f), Tooltip("追撃コアの寿命（秒）。HitStop中も短く減衰します。")]
    private float _counterCoreDuration = .026f;
    /// <summary>追撃の淡い外側と残光の表示倍率。高輝度コアから分離して抑える。</summary>
    [SerializeField, Range(0f, 1f), Tooltip("追撃の外側・残光の倍率。細いコアの強さとは独立します。")]
    private float _counterAfterglow = .3f;
    // 旧Prefabの幅設定を保持する。端点未設定の場合だけ使う。
    /// <summary>刃先Transform未設定時だけ使う旧互換の軌跡端点距離（Unity単位）。通常Inspectorでは非表示。</summary>
    [UnityEngine.Tooltip("刃先Transform未設定時だけ使う旧互換の軌跡端点距離（Unity単位）。通常Inspectorでは非表示。")]
    [SerializeField, HideInInspector] private float _width = .045f;
    private const int Capacity = 40;
    private readonly Vector3[] bases = new Vector3[Capacity], tips = new Vector3[Capacity];
    private readonly float[] times = new float[Capacity];
    private readonly float[] lifetimes = new float[Capacity], strengths = new float[Capacity], sampleWidths = new float[Capacity];
    private readonly Vector3[] accents = new Vector3[Capacity];
    // Velocity metadata for the same fixed history; no independent trajectory sampler.
    private readonly Vector3[] counterTipVelocities = new Vector3[Capacity];
    private const int CurveSteps = 3;
    private readonly Vector3[] vertices = new Vector3[Capacity * CurveSteps * 2];
    private readonly Vector2[] uvs = new Vector2[Capacity * CurveSteps * 2];
    private readonly Vector2[] dynamics = new Vector2[Capacity * CurveSteps * 2];
    private readonly Vector4[] highlights = new Vector4[Capacity * CurveSteps * 2];
    private readonly Color[] colors = new Color[Capacity * CurveSteps * 2];
    private readonly int[] triangles = new int[(Capacity * CurveSteps - 1) * 6];
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
    private CounterSlashPresentation counterSlash;
    private JustAvoidCounterAnimation counterAnimation;
    private bool counterWindow, counterSuppressed, captureCounterHit;

    /// <summary>共有先を含む刀の軌跡を攻撃種別の持続時間・明るさへ切り替える。</summary>
    public void SetStyle(AttackStyle value, JustAvoidCounterStrike strike = JustAvoidCounterStrike.None)
    {
        if (_sharedTrail != null && _sharedTrail != this) { _sharedTrail.SetStyle(value, strike); return; }
        if (counterSlash != null && (style != value || counterStrike != strike))
        {
            if (style == AttackStyle.JustAvoidCounter || value == AttackStyle.JustAvoidCounter)
            { count = 0; mesh.Clear(); hasPrevious = false; speedStrength = 0f; }
            counterSlash.Configure(value == AttackStyle.JustAvoidCounter && _counterPresentation != null
                ? _counterPresentation.GetStrike(strike) : null);
        }
        style = value;
        counterStrike = strike;
        if (value != AttackStyle.JustAvoidCounter) counterWindow = false;
        counterSlash?.SetDebugVisibility(value == AttackStyle.JustAvoidCounter && _counterPresentation != null && _counterPresentation.IsBloodOnlyDebug);
    }
    /// <summary>刀の振り区間のサンプリングを切り替える。新しい振りの開始時には前回履歴を消去する。</summary>
    public void SetActive(bool active)
    {
        if (_sharedTrail != null && _sharedTrail != this) { _sharedTrail.SetActive(active); return; }
        // Counter visuals follow the existing counter State, without extending the hitbox.
        if (!active && counterWindow) return;
        if (!initialized || emitting == active) return;
        emitting = active;
        if (active) { count = 0; mesh.Clear(); hasPrevious = false; speedStrength = 0f; counterSlash?.Reset(FeedbackVisualTime.Now); Sample(); }
    }
    /// <summary>受理済み命中の表示通知のみ。判定、ダメージ、Animator、HitStop設定は操作しない。</summary>
    public Vector3 NotifyCounterHit(JustAvoidCounterStrike strike, Vector3 point, Vector3 sweepDirection = default, bool showImpactStroke = true)
    {
        if (_sharedTrail != null && _sharedTrail != this) return _sharedTrail.NotifyCounterHit(strike, point, sweepDirection, showImpactStroke);
        if (!initialized || style != AttackStyle.JustAvoidCounter || counterStrike != strike || !counterSlash.Enabled) return Vector3.zero;
        counterSlash.Hit(point, FeedbackVisualTime.Now);
        float now=FeedbackVisualTime.Now;
        Vector3 recent=Vector3.zero;
        if(count>0 && now-times[count-1]<=.08f)
        {
            recent=counterTipVelocities[count-1];
            if(count>1 && now-times[count-2]<=.08f && recent.sqrMagnitude>.01f &&
                counterTipVelocities[count-2].sqrMagnitude>.01f && Vector3.Dot(recent.normalized,counterTipVelocities[count-2].normalized)>.9f)
                recent=(recent.normalized*2f+counterTipVelocities[count-2].normalized).normalized*recent.magnitude;
        }
        float delta=now-previousTime;
        Vector3 ownerDelta=owner!=null?owner.position-previousOwnerPosition:Vector3.zero;
        Vector3 instantaneous=_tip!=null && hasPrevious && delta>.0001f && delta<=.08f
            ? ((_tip.position-previousTip)-ownerDelta)/delta : Vector3.zero;
        Vector3 direction=CounterImpactStroke.SelectMotionDirection(instantaneous,recent,sweepDirection,_minimumSpeed);
        Vector3 physicalDirection = direction;
        Vector3 bladeAxis = _tip != null && _bladeBase != null ? _tip.position - _bladeBase.position : transform.up;
        var camera = Camera.main;
        Vector3 view = camera != null ? camera.transform.position - point : -transform.forward;
        var settings=counterSlash.Settings;
        float reliableSpeed=Mathf.Max(.1f,_minimumSpeed);
        direction=CounterImpactStroke.ProjectDirection(direction,recent.sqrMagnitude>=reliableSpeed*reliableSpeed?recent:sweepDirection,view,
            settings.StrokeMinProjection,settings.StrokeMaxProjectionAngle);
        if (showImpactStroke) counterSlash.ShowStroke(point, direction, bladeAxis, view, FeedbackVisualTime.Now);
        captureCounterHit = true;
        // Blood reuses the measured blade motion before the existing stroke-only camera correction.
        return physicalDirection;
    }
    /// <summary>共有軌跡または専用Meshを準備し、旧TrailRendererによる二重描画を停止する。</summary>
    /// <returns>軌跡の描画リソースを使用可能になった場合はtrue。</returns>
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
        owner = GetComponentInParent<Animator>(true)?.transform;
        surface = new GameObject("Blade Trail Surface", typeof(MeshFilter), typeof(MeshRenderer));
        // Runtime-only helper: never written into the scene, even if Init runs in Edit Mode (tests, tools).
        surface.hideFlags = HideFlags.DontSave;
        surface.layer = gameObject.layer;
        // Keep sampled vertices in world space; do not transform the history with the blade.
        surface.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        counterSlash = new CounterSlashPresentation(renderer, material);
        if (_counterPresentation != null) counterSlash.PrepareStroke(gameObject.layer);
        counterAnimation = owner != null ? owner.GetComponent<JustAvoidCounterAnimation>() : null;
        GameManager.OnGameStateChanged += OnGameStateChanged;
        initialized = true;
        return true;
    }
    /// <summary>切先側の端点をワールド座標で記録し、所有者の移動を除いた刀の振り速度から表示強度を求める。</summary>
    private void Sample()
    {
        Vector3 realBase = _bladeBase != null ? _bladeBase.position : transform.position;
        Vector3 b = _tip != null ? _tip.position : transform.position + transform.up * _width;
        bool silverSlash = counterSlash != null && counterSlash.Enabled;
        Vector3 a = Vector3.Lerp(realBase, b, silverSlash ? counterSlash.Settings.BladeStart : _bladeTrailStart);
        Vector3 physicalTip = b;
        if (silverSlash) b += (b - realBase) * counterSlash.Settings.TipExtension;
        float now = FeedbackVisualTime.Now;
        Vector3 ownerPosition = owner != null ? owner.position : Vector3.zero;
        float delta = now - previousTime;
        // 低FPSの短い間隔は補間し、長い停止・移動ジャンプは別の軌跡として始める。
        if (hasPrevious && (delta > .12f || (ownerPosition - previousOwnerPosition).sqrMagnitude > 4f))
        { count = 0; hasPrevious = false; speedStrength = 0f; }
        float speed = hasPrevious && delta > .0001f ? ((physicalTip - previousTip) - (ownerPosition - previousOwnerPosition)).magnitude / delta : 0f;
        Vector3 counterVelocity=silverSlash && hasPrevious && delta>.0001f
            ? ((physicalTip-previousTip)-(ownerPosition-previousOwnerPosition))/delta : Vector3.zero;
        previousTip = physicalTip; previousOwnerPosition = ownerPosition; previousTime = now; hasPrevious = true;
        float target = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_minimumSpeed, Mathf.Max(_minimumSpeed + .1f, _fullSpeed), speed));
        speedStrength = Mathf.MoveTowards(speedStrength, target, Mathf.Max(0f, delta) * (target > speedStrength ? 30f : 60f));
        if (count > 0 && (a - bases[count - 1]).sqrMagnitude + (b - tips[count - 1]).sqrMagnitude < .000004f) return;
        if (speedStrength < .025f)
        {
            // Keep one invisible seed at the current blade, rather than bridging
            // a long pause to the next fast sample.
            if (count == 0 || (silverSlash && count == 1 && strengths[0] <= 0f))
            { bases[0] = a; tips[0] = b; times[0] = now; lifetimes[0] = silverSlash ? .12f : .01f; strengths[0] = 0f; sampleWidths[0] = 1f; accents[0] = Vector3.zero; counterTipVelocities[0]=Vector3.zero; count = 1; }
            return;
        }
        if (count == Capacity) RemoveOldest();
        float duration = style == AttackStyle.JustAvoidCounter ? _counterDuration : style == AttackStyle.Heavy ? _heavyDuration : _duration;
        float power = style == AttackStyle.JustAvoidCounter ? _counterBrightness : style == AttackStyle.Heavy ? _heavyBrightness : 1f;
        var counterSettings = style == AttackStyle.JustAvoidCounter && _counterPresentation != null ? _counterPresentation.GetStrike(counterStrike) : null;
        if (counterSettings != null) { duration = counterSettings.TrailDuration; power = counterSettings.TrailBrightness; }
        bases[count] = a; tips[count] = b; times[count] = now;
        counterTipVelocities[count]=counterVelocity;
        lifetimes[count] = duration * Mathf.Lerp(.6f, 1f, speedStrength);
        strengths[count] = speedStrength * power;
        sampleWidths[count] = style == AttackStyle.Heavy ? 1.02f : 1f;
        if (counterSettings != null) sampleWidths[count] = counterSettings.TrailWidth;
        bool counter = style == AttackStyle.JustAvoidCounter;
        accents[count] = counter
            ? new Vector3(.08f + _counterCoreIntensity * Mathf.Pow(speedStrength, 4f), _counterAfterglow, _counterCoreDuration)
            : new Vector3(style == AttackStyle.Heavy ? .9f : .6f, 1f, duration * .45f);
        if (counterSettings != null) accents[count] = new Vector3(.15f + counterSettings.CoreIntensity * speedStrength * speedStrength, counterSettings.Afterglow, counterSettings.CoreDuration);
        count++;
    }
    /// <summary>履歴が上限に達したときに最古のサンプルを捨て、残りを前へ詰める。</summary>
    private void RemoveOldest()
    {
        for (int i = 1; i < count; i++) CopySample(i, i - 1);
        count--;
    }
    /// <summary>軌跡サンプルの端点・寿命・表示量を指定スロットへ複製する。</summary>
    private void CopySample(int source, int destination)
    {
        bases[destination] = bases[source]; tips[destination] = tips[source]; times[destination] = times[source];
        lifetimes[destination] = lifetimes[source]; strengths[destination] = strengths[source]; sampleWidths[destination] = sampleWidths[source];
        accents[destination] = accents[source];
        counterTipVelocities[destination]=counterTipVelocities[source];
    }
    /// <summary>寿命内のサンプルだけを残して刀身の細いMeshを更新し、残像の位置をワールド上に保持する。</summary>
    private void LateUpdate()
    {
        if (!initialized || mesh == null) return;
        counterSlash.SetDebugVisibility(style == AttackStyle.JustAvoidCounter && _counterPresentation != null && _counterPresentation.IsBloodOnlyDebug);
        if (FeedbackVisualTime.IsPaused) return;
        UpdateCounterWindow();
        float now = FeedbackVisualTime.Now;
        counterSlash.Advance(now, times, count, ref previousTime);
        int live = 0;
        for (int i = 0; i < count; i++)
            if (now - times[i] < lifetimes[i]) { CopySample(i, live); live++; }
        // 15〜20fpsで寿命よりフレーム間隔が長くても、直前位置から短い線を作れるよう種だけ残す。
        if (live == 0 && count > 0 && emitting && now - times[count - 1] < .12f)
        {
            CopySample(count - 1, 0); strengths[0] = 0f; counterTipVelocities[0]=Vector3.zero; times[0] = now; lifetimes[0] = .01f; live = 1;
        }
        count = live;
        if (emitting && (!counterSlash.IsHolding(now) || captureCounterHit)) Sample();
        captureCounterHit = false;
        counterSlash.Apply(now);
        mesh.Clear();
        if (count < 2) return;
        int points = (count - 1) * CurveSteps + 1;
        for (int p = 0; p < points; p++)
        {
            int i = Mathf.Min(p / CurveSteps, count - 2), j = i + 1;
            float t = (p - i * CurveSteps) / (float)CurveSteps;
            int v = p * 2;
            vertices[v] = InterpolateBlade(bases, i, t); vertices[v + 1] = InterpolateBlade(tips, i, t);
            float along = p / (float)(points - 1);
            if (counterSlash.Enabled)
            {
                vertices[v + 1] = CounterSlashPresentation.CurvedTip(vertices[v], tips[i] - bases[i], tips[j] - bases[j], t);
                // Anchor the sheet at the swept tip. Taper geometry at both temporal ends.
                Vector3 blade = vertices[v + 1] - vertices[v];
                vertices[v] = vertices[v + 1] - blade * Mathf.Lerp(sampleWidths[i], sampleWidths[j], t) * CounterSlashPresentation.Taper(along);
            }
            float strength = Mathf.Lerp(strengths[i], strengths[j], t);
            float age = Mathf.Clamp01((now - Mathf.Lerp(times[i], times[j], t)) / Mathf.Max(.01f, Mathf.Lerp(lifetimes[i], lifetimes[j], t)));
            uvs[v] = new Vector2(age, 0); uvs[v + 1] = new Vector2(age, 1);
            dynamics[v] = dynamics[v + 1] = new Vector2(Mathf.Clamp01(strength), counterSlash.Enabled ? along : Mathf.Lerp(sampleWidths[i], sampleWidths[j], t));
            Vector3 accent = Vector3.Lerp(accents[i], accents[j], t);
            float coreAge = Mathf.Max(0f, now - Mathf.Lerp(times[i], times[j], t)) / Mathf.Max(.01f, accent.z);
            if (counterSlash.Enabled) coreAge = counterSlash.CoreAge(Mathf.Max(0f, now - Mathf.Lerp(times[i], times[j], t)), now) / Mathf.Max(.033f, accent.z);
            highlights[v] = highlights[v + 1] = new Vector4(accent.x, accent.y, coreAge, accent.y);
            Color color = _color; color.r *= _brightness * strength; color.g *= _brightness * strength; color.b *= _brightness * strength;
            color.a *= Mathf.Clamp01(strength) * Mathf.Pow(1f - age, _fade);
            // Counter shader owns its single temporal fade. Do not multiply the old tiny body alpha again.
            if (counterSlash.Enabled) color = new Color(_brightness * strength, _brightness * strength, _brightness * strength, 1f);
            colors[v] = colors[v + 1] = color;
            if (p == points - 1) continue;
            int index = p * 6;
            triangles[index] = v; triangles[index + 1] = v + 1; triangles[index + 2] = v + 2;
            triangles[index + 3] = v + 1; triangles[index + 4] = v + 3; triangles[index + 5] = v + 2;
        }
        mesh.SetVertices(vertices, 0, points * 2);
        mesh.SetUVs(0, uvs, 0, points * 2);
        mesh.SetUVs(1, dynamics, 0, points * 2);
        mesh.SetUVs(2, highlights, 0, points * 2);
        mesh.SetColors(colors, 0, points * 2);
        mesh.SetTriangles(triangles, 0, (points - 1) * 6, 0);
        mesh.RecalculateBounds();
    }
    private void UpdateCounterWindow()
    {
        var strike = counterAnimation != null ? counterAnimation.Strike : JustAvoidCounterStrike.None;
        bool requested = !counterSuppressed && strike != JustAvoidCounterStrike.None && _counterPresentation != null &&
            _counterPresentation.GetStrike(strike).SlashMaterial != null;
        if (requested)
        {
            if (!counterWindow || counterStrike != strike)
            {
                SetStyle(AttackStyle.JustAvoidCounter, strike);
                // A hitbox event may have already started this same trail earlier in the frame.
                if (!emitting) SetActive(true);
                counterWindow = true;
            }
        }
        else if (counterWindow) { counterWindow = false; emitting = false; }
    }
    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Pause) return;
        counterSuppressed = state != GameManager.GameState.InGame;
        if (counterSuppressed && counterSlash.Enabled)
        { counterWindow = false; emitting = false; count = 0; hasPrevious = false; mesh.Clear(); counterSlash.Reset(FeedbackVisualTime.Now); }
    }
    /// <summary>記録済みの刀身端点を補間し、低FPSでも軌跡の折れ角を抑える。長い移動は直線補間に限定する。</summary>
    private Vector3 InterpolateBlade(Vector3[] samples, int i, float t)
    {
        Vector3 b = samples[i], c = samples[i + 1];
        if ((c - b).sqrMagnitude > 1f) return Vector3.Lerp(b, c, t);
        Vector3 a = samples[Mathf.Max(0, i - 1)], d = samples[Mathf.Min(count - 1, i + 2)];
        return .5f * ((2f * b) + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (-a + 3f * b - 3f * c + d) * t * t * t);
    }
    /// <summary>サンプリングを停止して履歴とMesh表示を消去する。</summary>
    private void OnDisable() { emitting = false; counterWindow = captureCounterHit = false; count = 0; if (mesh != null) mesh.Clear(); counterSlash?.Reset(FeedbackVisualTime.Now); }
    /// <summary>所有する軌跡Mesh・表示オブジェクトと、生成した場合だけMaterialを解放する。</summary>
    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged; counterSlash?.Dispose();
        // Destroy is not allowed in Edit Mode; DontSave objects must be removed explicitly there.
        if (Application.isPlaying) { if (mesh != null) Destroy(mesh); if (surface != null) Destroy(surface); if (ownsMaterial) Destroy(material); }
        else { if (mesh != null) DestroyImmediate(mesh); if (surface != null) DestroyImmediate(surface); if (ownsMaterial && material != null) DestroyImmediate(material); }
    }
}
