using UnityEngine;

/// <summary>刀の共有履歴から受けた方向で、命中時だけ固定表示する先細りの直線刃。リソースは初期化時に一度だけ確保。</summary>
internal sealed class CounterImpactStroke
{
    private const int Sections = 9;
    private readonly Vector3[] vertices = new Vector3[Sections * 3];
    private readonly Vector2[] uvs = new Vector2[Sections * 3];
    private readonly int[] indices = new int[(Sections - 1) * 12];
    private readonly Mesh mesh;
    private readonly GameObject surface;
    private readonly MeshRenderer renderer;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private static readonly int AgeId = Shader.PropertyToID("_StrokeAge");
    private static readonly int CoreId = Shader.PropertyToID("_CoreDuration");
    private static readonly int DurationId = Shader.PropertyToID("_Duration");
    private static readonly int IntensityId = Shader.PropertyToID("_StrokeIntensity");
    private float started, duration;
    private bool active;

    public CounterImpactStroke(int layer)
    {
        mesh = new Mesh { name = "Counter Impact Straight Blade" }; mesh.MarkDynamic();
        for (int i=0;i<Sections;i++)
        {
            float u=i/(float)(Sections-1);
            for (int row=0;row<3;row++) uvs[i*3+row]=new Vector2(u,row*.5f);
            if (i==Sections-1) continue;
            for (int row=0;row<2;row++)
            {
                int v=i*3+row, k=i*12+row*6;
                indices[k]=v; indices[k+1]=v+1; indices[k+2]=v+3;
                indices[k+3]=v+1; indices[k+4]=v+4; indices[k+5]=v+3;
            }
        }
        surface = new GameObject("Counter Impact Straight Blade",typeof(MeshFilter),typeof(MeshRenderer));
        surface.layer=layer; surface.GetComponent<MeshFilter>().sharedMesh=mesh;
        renderer=surface.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false; renderer.enabled=false;
    }

    public static Vector3 ResolveDirection(Vector3 sweep, Vector3 history)
    {
        // Use exactly the same accepted sweep as Blood. Averaging an older curved
        // history here can tilt the straight incision away from that contact direction.
        return sweep.sqrMagnitude>=.000001f ? sweep.normalized : history.normalized;
    }

    public static Vector3 BladeVertex(Vector3 point, Vector3 direction, Vector3 side, float length, float width, float u, float across)
    {
        float taper=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(Mathf.PI*u)),.8f)*Mathf.Lerp(.55f,1f,u);
        // Anchor the geometric center at the real contact. Direction comes from the reveal/light,
        // rather than displacing the entire stroke forward beside the enemy.
        return point+direction*((u-.5f)*length)+side*(across*width*.5f*taper);
    }

    public void Show(JustAvoidCounterConfig.Strike settings, Vector3 point, Vector3 direction, Vector3 bladeAxis, Vector3 view, float now)
    {
        if (settings.StrokeMaterial==null || direction.sqrMagnitude<.000001f) { Hide(); return; }
        direction.Normalize();
        Vector3 side=Vector3.Cross(view,direction);
        if (side.sqrMagnitude<.000001f) side=Vector3.ProjectOnPlane(bladeAxis,direction);
        if (side.sqrMagnitude<.000001f) side=Vector3.Cross(Mathf.Abs(direction.y)<.95f?Vector3.up:Vector3.right,direction);
        side.Normalize();
        for (int i=0;i<Sections;i++) for (int row=0;row<3;row++)
            vertices[i*3+row]=BladeVertex(point,direction,side,Mathf.Max(.2f,settings.StrokeLength),Mathf.Max(.01f,settings.StrokeWidth),i/(float)(Sections-1),row-1f);
        mesh.SetVertices(vertices); mesh.SetUVs(0,uvs); mesh.SetTriangles(indices,0); mesh.RecalculateBounds();
        renderer.sharedMaterial=settings.StrokeMaterial;
        block.SetFloat(CoreId,Mathf.Max(.033f,settings.StrokeCoreDuration));
        duration=Mathf.Max(settings.StrokeCoreDuration+.01f,settings.StrokeDuration);
        block.SetFloat(DurationId,duration); block.SetFloat(IntensityId,Mathf.Max(0f,settings.StrokeIntensity));
        started=now; active=true; renderer.enabled=true; Tick(now);
    }

    public void Tick(float now)
    {
        if (!active) return;
        float age=Mathf.Max(0f,now-started);
        if (age>=duration) { Hide(); return; }
        block.SetFloat(AgeId,age); renderer.SetPropertyBlock(block);
    }
    public void Hide() { active=false; renderer.enabled=false; }
    public void Dispose() { Object.Destroy(mesh); Object.Destroy(surface); }
}
