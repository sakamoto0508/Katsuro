using UnityEngine;
using UnityEngine.UI;

/// <summary>バー画像に描き込まれた市松模様を、設定した輪郭メッシュで描画対象から除く。</summary>
[RequireComponent(typeof(Image))]
public sealed class BossBarSilhouette : BaseMeshEffect
{
    [SerializeField] private Vector2[] _outline = {
        new Vector2(0,.15f),new Vector2(0,.60f),new Vector2(.018f,.73f),
        new Vector2(.455f,.73f),new Vector2(.469f,.78f),new Vector2(.479f,.90f),
        new Vector2(.490f,.98f),new Vector2(.500f,1),new Vector2(.510f,.98f),
        new Vector2(.521f,.90f),new Vector2(.531f,.78f),new Vector2(.545f,.73f),
        new Vector2(.982f,.73f),new Vector2(1,.60f),new Vector2(1,.15f),
        new Vector2(.981f,0),new Vector2(.019f,0)
    };
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount!=4 || _outline==null || _outline.Length<3) return;
        var bottom=UIVertex.simpleVert;var top=UIVertex.simpleVert;
        vh.PopulateUIVertex(ref bottom,0);vh.PopulateUIVertex(ref top,2);
        vh.Clear();
        Add(vh,bottom,top,new Vector2(.5f,.35f));
        foreach(var point in _outline) Add(vh,bottom,top,point);
        for(int i=0;i<_outline.Length;i++)vh.AddTriangle(0,i+1,(i+1)%_outline.Length+1);
    }
    private static void Add(VertexHelper vh,UIVertex bottom,UIVertex top,Vector2 point)
    {
        var vertex=bottom;
        vertex.position=new Vector3(Mathf.Lerp(bottom.position.x,top.position.x,point.x),Mathf.Lerp(bottom.position.y,top.position.y,point.y),bottom.position.z);
        vertex.uv0=new Vector4(Mathf.Lerp(bottom.uv0.x,top.uv0.x,point.x),Mathf.Lerp(bottom.uv0.y,top.uv0.y,point.y),0,0);
        vh.AddVert(vertex);
    }
}
