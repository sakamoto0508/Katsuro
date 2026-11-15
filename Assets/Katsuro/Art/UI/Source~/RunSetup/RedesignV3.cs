using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
public static class SetupRedesignV3
{
    const string Path="Assets/Katsuro/Prefabs/UI/RunSetupCanvas.prefab";
    const string Art="Assets/Katsuro/Art/UI/Textures/Setup/Redesign/";
    static Color C(string h,float a=1){Color c;ColorUtility.TryParseHtmlString("#"+h,out c);c.a=a;return c;}
    static Transform T(Transform r,string p){var t=r.Find(p);if(t==null)throw new Exception(p);return t;}
    static void R(Transform t,float x,float y,float w,float h,float ax=0,float ay=1,float px=0,float py=1)
    {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(ax,ay);r.pivot=new Vector2(px,py);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static void F(Transform t,float size,bool wrap=false,string color="E8E3DA")
    {var f=t.GetComponent<TMP_Text>();f.fontSize=size;f.enableAutoSizing=false;f.color=C(color);f.textWrappingMode=wrap?TextWrappingModes.Normal:TextWrappingModes.NoWrap;f.alignment=TextAlignmentOptions.MidlineLeft;f.overflowMode=TextOverflowModes.Overflow;}
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+n+".png");
    static void Import(string n,Vector4 border)
    {
        string path=Art+n+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spriteBorder=border;i.spritePixelsPerUnit=100;
        i.alphaIsTransparency=true;i.mipmapEnabled=false;i.sRGBTexture=true;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;
        var st=new TextureImporterSettings();i.ReadTextureSettings(st);st.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(st);i.SaveAndReimport();
    }
    public static string Materials()
    {
        Import("PreviewEnso",Vector4.zero);
        foreach(string role in new[]{"Tutorial","Boss"})foreach(string state in new[]{"Normal","Selected","Pressed","Disabled"})
        {
            int w=256,h=96;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var p=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int d=Math.Min(Math.Min(x,w-x-1),Math.Min(y,h-y-1));var c=Color.clear;
                if(d>=2)
                {
                    c=role=="Boss"?C("492428",.93f):C("252326",.93f);
                    if(state=="Selected")c=C("71373A",.95f);if(state=="Pressed")c=C("301B1D");if(state=="Disabled")c=C("202023",.7f);
                    if(d==2)c=state=="Disabled"?C("6E685E",.35f):state=="Pressed"?C("9E3535"):role=="Boss"?C("B77A66",.85f):C("B59A66",.8f);
                    if(y>=3&&y<=5&&x>=12&&x<w-12&&state!="Disabled")c=role=="Boss"?C("9E3535"):C("B59A66",.8f);
                }
                p[y*w+x]=c;
            }
            t.SetPixels(p);t.Apply();string n="Strong"+role+state;File.WriteAllBytes(Art+n+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);Import(n,new Vector4(16,12,16,12));
        }
        return "Prepared transparent enso and eight strong action button sprites.";
    }
    static Transform Node(Transform parent,string name)
    {var t=parent.Find(name);if(t!=null)return t;var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return g.transform;}
    static TMP_Text Text(Transform parent,string name,TMP_Text source)
    {var t=parent.Find(name);if(t==null){var g=UnityEngine.Object.Instantiate(source.gameObject,parent);g.name=name;t=g.transform;}var text=t.GetComponent<TMP_Text>();text.raycastTarget=false;return text;}
    static UnityEngine.UI.Image Image(Transform parent,string name)
    {var t=Node(parent,name);var i=t.GetComponent<UnityEngine.UI.Image>();if(i==null)i=t.gameObject.AddComponent<UnityEngine.UI.Image>();i.raycastTarget=false;return i;}
    public static void Layout(GameObject root)
    {
        var body=T(root.transform,"Content/Body");var left=T(body,"Blessings");R(left,0,0,560,692);
        foreach(string kind in new[]{"Attack","Defense"})
        {
            var heading=T(left,kind+"Heading");R(heading,0,kind=="Attack"?0:-340,560,44);
            R(T(heading,"Label"),30,0,260,44);F(T(heading,"Label"),30);R(T(heading,"Line"),294,-33,36,1);
            R(T(heading,"Hint"),0,-12,224,30,1,1,1,1);F(T(heading,"Hint"),17,false,"BDB7AC");
            var group=T(left,kind+"Options");R(group,0,kind=="Attack"?-56:-396,560,252);
            for(int j=0;j<3;j++)
            {
                var row=T(group,kind+"_"+j);R(row,0,-84*j,560,84);
                R(T(row,"BaseFrame"),24,-83,536,1);R(T(row,"Selected/Frame"),24,-82,536,1);R(T(row,"Selected/Badge"),-5,-9,82,28,1,1,1,1);
                R(T(row,"HoverLayer"),-8,108,580,300);
                // Red brush belongs to the equipped layer; focus is represented by corner marks.
                var hover=T(row,"HoverLayer").GetComponent<UnityEngine.UI.Image>();hover.sprite=null;hover.color=Color.clear;
                var equipped=T(row,"Selected/Fill").GetComponent<UnityEngine.UI.Image>();
                R(equipped.transform,-8,108,580,300);equipped.sprite=S("FocusBrush");equipped.type=UnityEngine.UI.Image.Type.Simple;equipped.color=new Color(1,1,1,.42f);
                var f=(RectTransform)T(row,"FocusFrame");f.anchorMin=Vector2.zero;f.anchorMax=Vector2.one;f.offsetMin=new Vector2(10,6);f.offsetMax=new Vector2(5,-6);
                R(T(row,"Content/TopRow"),0,0,350,84);R(T(row,"Content/TopRow/Emblem"),24,-10,58,58);
                R(T(row,"Content/TopRow/Label"),104,-2,220,42);F(T(row,"Content/TopRow/Label"),32);
                R(T(row,"Content/Effect"),104,-44,440,32);F(T(row,"Content/Effect"),18,false,"D1C9BC");
            }
        }
        var right=T(body,"Details");R(right,0,0,576,692,1,1,1,1);var load=T(right,"LoadoutPanel");R(load,0,0,576,372);
        R(T(load,"Frame"),-24,0,1,692);R(T(load,"Title"),0,0,576,48);F(T(load,"Title"),30);R(T(load,"Rule"),0,-56,576,1);
        int index=0;foreach(string kind in new[]{"Attack","Defense"})
        {
            var row=T(load,kind+"Row");R(row,0,-82-index*146,576,132);R(T(row,"Tag"),0,-2,48,48);F(T(row,"Tag/Text"),28);
            var text=(RectTransform)T(row,"Text");text.offsetMin=new Vector2(66,0);text.offsetMax=Vector2.zero;
            R(T(text,"Head"),0,0,510,56);R(T(text,"Head/Name"),0,0,108,56);F(T(text,"Head/Name"),32);
            R(T(text,"Head/Description"),116,2,394,56);F(T(text,"Head/Description"),20,true,"B59A66");
            R(T(text,"Detail"),0,-62,510,70);F(T(text,"Detail"),20,true,"D1C9BC");index++;
        }
        var info=T(right,"InfoPanel");R(info,0,-408,576,202);R(T(info,"Title"),0,0,576,48);F(T(info,"Title"),30);
        R(T(info,"Rule"),0,-56,576,1);R(T(info,"Opponent"),0,-82,576,64);F(T(info,"Opponent"),23,true);
        R(T(info,"Rules"),0,-151,576,64);F(T(info,"Rules"),20,true,"D1C9BC");R(T(right,"ResultPanel"),0,-634,576,78);F(T(right,"ResultPanel/Text"),19,true,"E1B5AA");
        var preview=Node(body,"FocusPreview");R(preview,592,0,520,692);
        var enso=Image(preview,"Enso");R(enso.transform,10,-48,500,500);enso.sprite=S("PreviewEnso");enso.color=new Color(1,1,1,.24f);enso.preserveAspect=true;
        var first=T(left,"AttackOptions/Attack_0");
        var symbol=Text(preview,"Symbol",T(first,"Content/TopRow/Emblem/Text").GetComponent<TMP_Text>());R(symbol.transform,60,-100,400,330);F(symbol.transform,190);symbol.alignment=TextAlignmentOptions.Center;
        var name=Text(preview,"Name",T(first,"Content/TopRow/Label").GetComponent<TMP_Text>());R(name.transform,0,-430,520,80);F(name.transform,56);name.alignment=TextAlignmentOptions.Center;
        var line=Image(preview,"Rule");R(line.transform,75,-526,370,2);line.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Katsuro/Art/UI/Textures/Setup/SetupFadeLine.png");line.color=C("B59A66",.65f);
        var effect=Text(preview,"Effect",T(first,"Content/Effect").GetComponent<TMP_Text>());R(effect.transform,30,-548,460,104);F(effect.transform,28,true,"D1C9BC");effect.alignment=TextAlignmentOptions.Top;
        var presenter=preview.GetComponent<RunSetupBlessingPreview>();if(presenter==null)presenter=preview.gameObject.AddComponent<RunSetupBlessingPreview>();
        var so=new SerializedObject(presenter);so.FindProperty("_symbol").objectReferenceValue=symbol;so.FindProperty("_name").objectReferenceValue=name;so.FindProperty("_effect").objectReferenceValue=effect;var entries=so.FindProperty("_entries");entries.arraySize=6;
        int k=0;foreach(string kind in new[]{"Attack","Defense"})for(int j=0;j<3;j++)
        {
            var row=T(left,kind+"Options/"+kind+"_"+j);var e=entries.GetArrayElementAtIndex(k++);
            e.FindPropertyRelative("Option").objectReferenceValue=row.GetComponent<Selectable>();e.FindPropertyRelative("Symbol").objectReferenceValue=T(row,"Content/TopRow/Emblem/Text").GetComponent<TMP_Text>();e.FindPropertyRelative("Name").objectReferenceValue=T(row,"Content/TopRow/Label").GetComponent<TMP_Text>();e.FindPropertyRelative("Effect").objectReferenceValue=T(row,"Content/Effect").GetComponent<TMP_Text>();
            var visual=new SerializedObject(row.GetComponent<RunSetupSelectableEffect>());visual.FindProperty("_blessingPreview").objectReferenceValue=presenter;visual.ApplyModifiedPropertiesWithoutUndo();
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var footer=T(root.transform,"Content/Footer");R(footer,0,0,1728,104,0,0,0,0);
        var guide=T(footer,"InputHint").GetComponent<TMP_Text>();guide.text=guide.text.Replace("金枠＝装備中","赤帯＝装備中");
        foreach(string role in new[]{"Tutorial","Boss"})
        {
            var button=T(footer,role=="Boss"?"StartButton":"TutorialButton");R(button,role=="Boss"?0:-480,0,role=="Boss"?440:400,104,1,1,1,1);
            var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=S("Strong"+role+"Normal");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;
            button.GetComponent<Button>().spriteState=new SpriteState{highlightedSprite=S("Strong"+role+"Selected"),selectedSprite=S("Strong"+role+"Selected"),pressedSprite=S("Strong"+role+"Pressed"),disabledSprite=S("Strong"+role+"Disabled")};
            F(T(button,"Content/Label"),role=="Boss"?44:40);T(button,"Content/Label").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            F(T(button,"Content/Caption"),18,false,"D1C9BC");T(button,"Content/Caption").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        }
    }
    public static string Apply()
    {
        var original=File.ReadAllText(Path).Replace("\r\n","\n");
        var guide=AssetDatabase.LoadAssetAtPath<GameObject>(Path).transform.Find("Content/Footer/InputHint").GetComponent<TMP_Text>();
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(guide,out string guideGuid,out long guideId);
        var root=PrefabUtility.LoadPrefabContents(Path);
        try{Layout(root);PrefabUtility.SaveAsPrefabAsset(root,Path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var saved=File.ReadAllText(Path).Replace("\r\n","\n");string pat=@"(?ms)^--- !u!\d+ &(-?\d+)\n.*?(?=^--- !u!|\z)";
        var fresh=Regex.Matches(saved,pat).Cast<Match>().ToDictionary(m=>m.Groups[1].Value,m=>m.Value);var originalMatches=Regex.Matches(original,pat).Cast<Match>().ToArray();
        var result=original.Substring(0,originalMatches[0].Index);var ids=originalMatches.Select(m=>m.Groups[1].Value).ToHashSet();
        foreach(var old in originalMatches)
        {
            var next=fresh[old.Groups[1].Value];
            if(old.Value.Contains("Assembly-CSharp::RunSetupUI"))next=old.Value;
            foreach(Match originalName in Regex.Matches(old.Value,@"(?m)^  m_Name:.*$"))next=Regex.Replace(next,@"(?m)^  m_Name:.*$",m=>originalName.Value);
            if(old.Groups[1].Value!=guideId.ToString())foreach(Match text in Regex.Matches(old.Value,@"(?m)^  m_text:.*$"))next=Regex.Replace(next,@"(?m)^  m_text:.*$",m=>text.Value);
            result+=next;
        }
        foreach(var pair in fresh)if(!ids.Contains(pair.Key))result+=Regex.Replace(pair.Value,@"(?m)[ \t]+$","");
        Directory.CreateDirectory("Temp/SetupV3");File.WriteAllText("Temp/SetupV3/normalized.prefab.txt",result.Replace("\n","\r\n"),new System.Text.UTF8Encoding(false));
        return "Added read-only central focus preview and emphasized both existing actions; gameplay bindings preserved.";
    }
}
