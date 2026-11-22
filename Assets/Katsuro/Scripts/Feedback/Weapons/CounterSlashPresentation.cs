using UnityEngine;

/// <summary>既存SwordTrailのMeshを追撃専用表示へ切替え、命中中の履歴時刻と局所光を保持する。</summary>
internal sealed class CounterSlashPresentation
{
    private static readonly int ContactId = Shader.PropertyToID("_ContactPoint");
    private static readonly int ImpactId = Shader.PropertyToID("_Impact");
    private readonly MeshRenderer renderer;
    private readonly Material normalMaterial;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private float lastUpdate, holdStart, holdUntil;
    private bool contacted;
    private Vector3 contact;
    private CounterImpactStroke stroke;
    public JustAvoidCounterConfig.Strike Settings { get; private set; }
    public bool Enabled => Settings != null && Settings.SlashMaterial != null;

    public CounterSlashPresentation(MeshRenderer renderer, Material normalMaterial)
    {
        this.renderer = renderer;
        this.normalMaterial = normalMaterial;
    }

    public void Configure(JustAvoidCounterConfig.Strike settings)
    {
        Settings = settings;
        renderer.sharedMaterial = Enabled ? settings.SlashMaterial : normalMaterial;
        Reset(FeedbackVisualTime.Now);
    }

    public void Reset(float now)
    {
        SetDebugVisibility(false);
        lastUpdate = now;
        holdStart = holdUntil = 0f;
        contacted = false;
        if (renderer != null)
            renderer.SetPropertyBlock(null);
        stroke?.Hide();
    }

    public bool IsHolding(float now) => Enabled && contacted && now < holdUntil;

    public void Hit(Vector3 point, float now)
    {
        if (!Enabled) return;
        // 既存の軌跡と同じ時計を使い、ポーズ中は進行を止める。
        if (!IsHolding(now))
            holdStart = now;
        holdUntil = Mathf.Max(holdUntil, now + Mathf.Max(0f, Settings.HitStop));
        contacted = true;
        contact = point;
    }

    public void Advance(float now, float[] sampleTimes, int count, ref float previousTime)
    {
        if (Enabled && contacted)
        {
            float held = Mathf.Max(0f, Mathf.Min(now, holdUntil) - Mathf.Max(lastUpdate, holdStart));
            // 刀身が止まっている間はサンプルを追加せず、既存の履歴を保持する。
            for (int i = 0; i < count; i++)
                sampleTimes[i] += held;
            previousTime += held;
        }
        lastUpdate = now;
    }

    public float CoreAge(float sampleAge, float now)
    {
        // 命中時に生成済みの弧を更新し、ヒットストップ中はその形を保持する。
        return contacted ? Mathf.Min(sampleAge, .012f + Mathf.Max(0f, now - holdUntil)) : sampleAge;
    }

    public void Apply(float now)
    {
        stroke?.Tick(now);
        if (!Enabled) return;
        float impact = contacted ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, now - holdUntil) / .065f) : 0f;
        block.SetVector(ContactId, new Vector4(contact.x, contact.y, contact.z, 1f));
        block.SetFloat(ImpactId, impact);
        renderer.SetPropertyBlock(block);
    }
    public void PrepareStroke(int layer)
    {
        if (stroke == null)
            stroke = new CounterImpactStroke(layer);
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void SetDebugVisibility(bool hidden)
    {
        if (renderer != null)
            renderer.forceRenderingOff = hidden;
        stroke?.SetDebugVisibility(hidden);
    }
    public void ShowStroke(Vector3 point, Vector3 direction, Vector3 bladeAxis, Vector3 view, float now)
    {
        if (Enabled)
            stroke?.Show(Settings, point, direction, bladeAxis, view, now);
    }
    public void Dispose() => stroke?.Dispose();

    public static float Taper(float along)
    {
        return Mathf.Sqrt(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(along / .16f)) *
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - along) / .08f)));
    }

    public static Vector3 CurvedTip(Vector3 interpolatedBase, Vector3 bladeBefore, Vector3 bladeAfter, float t)
    {
        // 低 FPS では、同じ 2 点の刀身サンプルから途中の回転を補間する。
        // 刀身の向きが反対か退化しているときは、半回転の向きを決められないため直線で補間する。
        bool safeArc = bladeBefore.sqrMagnitude > .0001f && bladeAfter.sqrMagnitude > .0001f &&
            Vector3.Dot(bladeBefore.normalized, bladeAfter.normalized) > -.5f;
        return interpolatedBase + (safeArc ? Vector3.Slerp(
            bladeBefore,
            bladeAfter,
            t) : Vector3.Lerp(
            bladeBefore,
            bladeAfter,
            t));
    }
}
