using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
public static class SetupCapture
{
    public static string Main(string output, bool redesign = false, string state = "")
    {
        if(redesign) SetupInkGold.Generate();
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject root = null, cameraObject = null;
        RenderTexture rt = null; Texture2D tex = null;
        var previous = RenderTexture.active;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Katsuro/Prefabs/UI/RunSetupCanvas.prefab");
            root = UnityEngine.Object.Instantiate(prefab);
            if(redesign) SetupInkGold.Style(root);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(true); root.transform.localScale=Vector3.one;
            foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))
                if(b is RunSetupUI || b is RunSetupSelectableEffect) b.enabled = false;
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            foreach(var g in root.GetComponentsInChildren<CanvasGroup>(true)) g.alpha=1;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="HoverLayer" || t.name=="FocusFrame" || t.name=="Flash")
                { var g=t.GetComponent<CanvasGroup>(); if(g!=null)g.alpha=0; }
            }
            var toggles=root.GetComponentsInChildren<Toggle>(true);
            foreach(var toggle in toggles)
            {
                bool selected=toggle.name.EndsWith("_0");
                toggle.SetIsOnWithoutNotify(selected);
                var t=toggle.transform.Find("Selected");
                if(t!=null)t.GetComponent<CanvasGroup>().alpha=selected?1:0;
            }
            if(state=="FocusExample")
            {
                var card=root.transform.Find("Content/Body/Blessings/AttackOptions/Attack_2");
                card.Find("FocusFrame").GetComponent<CanvasGroup>().alpha=1;
                card.Find("HoverLayer").GetComponent<CanvasGroup>().alpha=1;
            }
            if(state=="Selected"||state=="Pressed"||state=="Disabled")foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if(state=="Disabled")b.interactable=false;
                b.GetComponent<UnityEngine.UI.Image>().overrideSprite=state=="Selected"?b.spriteState.selectedSprite:state=="Pressed"?b.spriteState.pressedSprite:b.spriteState.disabledSprite;
                var focus=b.transform.Find("FocusFrame");
                if(focus!=null)focus.GetComponent<CanvasGroup>().alpha=state=="Selected"?1:0;
            }
            cameraObject=new GameObject("PreviewCamera",typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.055f,.055f,.06f,1);
            camera.cullingMask=1<<31; camera.orthographic=true; camera.orthographicSize=540;
            camera.transform.position=new Vector3(0,0,-1000);
            camera.nearClipPlane=.1f;camera.farClipPlane=2000;
            rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            camera.Render(); RenderTexture.active=rt;
            tex=new Texture2D(1920,1080,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,tex.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active=previous;
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
            if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);
            if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

// Run with Unity Pipeline run_script. Kept outside imported scripts; no runtime dependency.
public static class SetupInkGold
{
    const string Folder="Assets/Katsuro/Art/UI/Textures/Setup/InkGold";
    static Color Ink=C("171719"), Gold=C("B59A66"), Red=C("9E3535"), Text=C("E8E3DA");
    static Color C(string hex,float alpha=1){Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);c.a=alpha;return c;}
    static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+name+".png");
    static void Texture(string name,int w,int h,Func<int,int,Color> pixel,int border=16)
    {
        var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var p=new Color[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)p[y*w+x]=pixel(x,y);
        t.SetPixels(p);t.Apply();var path=Folder+"/"+name+".png";
        File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;
        i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;
        i.spriteBorder=new Vector4(border,border,border,border);i.alphaIsTransparency=true;i.mipmapEnabled=false;
        i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;
        var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);
        i.SaveAndReimport();
    }
    // Quiet fibers and ink pooling, no marks behind labels. 1-pixel keylines stay in protected slices.
    static Color Surface(int x,int y,int w,int h,Color basis,Color edge,bool selected=false,bool disabled=false)
    {
        int d=Math.Min(Math.Min(x,w-1-x),Math.Min(y,h-1-y));
        if(d<2)return Color.clear;
        if(d==2)return new Color(edge.r,edge.g,edge.b,edge.a*(disabled?.35f:.60f));
        float rim=Mathf.Exp(-d/13f);
        float n=(Mathf.PerlinNoise(x*.071f+3,y*.071f+8)-.5f)*.009f;
        float f=(Mathf.PerlinNoise(x*.42f,y*.025f)-.5f)*.02f*rim;
        float wash=(Mathf.PerlinNoise(x*.014f,y*.014f)-.5f)*.018f;
        var c=basis; c.r+=n+f+wash;c.g+=n+f+wash;c.b+=n+f+wash;
        c.a=1;
        if(selected && (x==6||x==7) && y>=8&&y<h-8)c=Red;
        if(disabled&&d==5&&((x+y)/5)%2==0)c=C("55514A",.6f);
        return c;
    }
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        Texture("Panel",256,256,(x,y)=>Surface(x,y,256,256,Ink,C("B59A66",.4f)),24);
        Texture("Card",256,256,(x,y)=>Surface(x,y,256,256,C("202022"),C("7E715B")),24);
        Texture("ButtonNormal",192,96,(x,y)=>Surface(x,y,192,96,Ink,Gold),16);
        Texture("ButtonSelected",192,96,(x,y)=>Surface(x,y,192,96,C("2B2021"),Red,true),16);
        Texture("ButtonPressed",192,96,(x,y)=>Surface(x,y,192,96,C("3A2425"),Red,true),16);
        Texture("ButtonDisabled",192,96,(x,y)=>Surface(x,y,192,96,C("1B1B1D"),C("55514A"),false,true),16);
        Texture("FineFrame",32,32,(x,y)=>{int d=Math.Min(Math.Min(x,31-x),Math.Min(y,31-y));return d==0?Color.white:Color.clear;},6);
        Texture("FocusCorners",64,64,(x,y)=>{int a=Math.Min(x,63-x),b=Math.Min(y,63-y);return (a<2&&b<15)||(b<2&&a<15)?Color.white:Color.clear;},20);
    }
    static string Path(Transform t,Transform root)
    { if(t==root)return "";return Path(t.parent,root)+"/"+t.name; }
    static void Set(UnityEngine.UI.Image i,string name,Color color)
    {i.sprite=S(name);i.type=UnityEngine.UI.Image.Type.Sliced;i.color=color;}
    public static void Style(GameObject root)
    {
        foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            // Preserve all copy, typography, materials, sizes, margins and layout settings.
            var c=t.color;
            if(c.r>.8f&&c.g>.75f)t.color=new Color(Text.r,Text.g,Text.b,c.a);
            else if(c.r-c.b>.2f&&c.g>.4f)t.color=new Color(Gold.r,Gold.g,Gold.b,c.a);
            else if(c.r>.45f&&c.g>.4f&&c.b>.3f)t.color=C("BDB7AC",c.a);
        }
        foreach(var i in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            string p=Path(i.transform,root.transform);string name=i.name;
            if(name=="Attack_0"||name=="Attack_1"||name=="Attack_2"||name=="Defense_0"||name=="Defense_1"||name=="Defense_2")Set(i,"Card",Color.white);
            else if(name=="LoadoutPanel"||name=="InfoPanel"||name=="NameInput")Set(i,"Panel",Color.white);
            else if(p.Contains("/Selected/"))
            {
                if(name=="Fill")i.color=C("5B292B",.22f);
                else if(name=="Glow")i.color=C("9E3535",.12f);
                else if(name=="Frame")Set(i,"FineFrame",C("B59A66",.85f));
                else if(name=="TopBar")i.color=Red;
                else if(name=="Badge")i.color=Red;
            }
            else if(name=="BaseFrame")Set(i,"FineFrame",C("B59A66",.16f));
            else if(name=="FocusFrame")Set(i,"FocusCorners",C("E8E3DA",.9f));
            else if(name=="Frame")
            {Set(i,"FineFrame",C("B59A66",p.Contains("Emblem")?.35f:.18f));}
            else if(name=="HoverLayer")i.color=C("E8E3DA",.035f);
            else if(name=="Flash")i.color=C("9E3535",.12f);
            else if(name=="Tag"||name=="Seal"||name=="Mark")i.color=Red;
            else if(name=="Shade")i.color=C("000000",.08f);
            else if(name=="Line"||name=="Rule"||name=="HeaderRule"||name=="FooterRule")i.color=C("B59A66",.26f);
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="Badge")
            {var text=t.GetComponentInChildren<TMPro.TMP_Text>(true);if(text!=null)text.color=Text;}
        }
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            Set(b.GetComponent<UnityEngine.UI.Image>(),"ButtonNormal",Color.white);
            b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
            b.spriteState=new SpriteState {highlightedSprite=S("ButtonSelected"),selectedSprite=S("ButtonSelected"),pressedSprite=S("ButtonPressed"),disabledSprite=S("ButtonDisabled")};
        }
        foreach(var effect in root.GetComponentsInChildren<RunSetupSelectableEffect>(true))
        {
            var so=new SerializedObject(effect);
            so.FindProperty("_focusScale").floatValue=1f;so.FindProperty("_hoverScale").floatValue=1f;
            so.FindProperty("_focusFrameStartScale").floatValue=1f;so.FindProperty("_selectedStartScale").floatValue=1f;
            so.FindProperty("_pressPunch").floatValue=-.008f;so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    public static string Apply()
    {
        var path="Assets/Katsuro/Prefabs/UI/RunSetupCanvas.prefab";
        var original=File.ReadAllText(path).Replace("\r\n","\n");
        var root=PrefabUtility.LoadPrefabContents(path);
        try{Style(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        // Unity reorders YAML and refreshes TMP input display text on load. Retain only
        // the intended visual properties, keeping every original layout/copy/reference byte.
        var styled=File.ReadAllText(path).Replace("\r\n","\n");
        var pattern=@"(?ms)^--- !u!\d+ &(-?\d+)\n.*?(?=^--- !u!|\z)";
        var docs=new System.Collections.Generic.Dictionary<string,string>();
        foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(styled,pattern))docs[m.Groups[1].Value]=m.Value;
        var allowed=@"m_Color|m_Sprite|m_Type|m_fontColor|rgba|_focusScale|_hoverScale|_focusFrameStartScale|_selectedStartScale|_pressPunch|m_Transition|m_HighlightedSprite|m_PressedSprite|m_SelectedSprite|m_DisabledSprite";
        var merged=System.Text.RegularExpressions.Regex.Replace(original,pattern,m=>{
            var block=m.Value;var saved=docs[m.Groups[1].Value];
            foreach(System.Text.RegularExpressions.Match line in System.Text.RegularExpressions.Regex.Matches(saved,@"(?m)^( +)("+allowed+@"):.*$"))
                block=System.Text.RegularExpressions.Regex.Replace(block,@"(?m)^"+line.Groups[1].Value+line.Groups[2].Value+@":.*$",unused=>line.Value);
            return block;
        });
        File.WriteAllText(path,merged.Replace("\n","\r\n"),new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        return "Styled existing RunSetupCanvas; no hierarchy, layout, copy or gameplay edits.";
    }
}
