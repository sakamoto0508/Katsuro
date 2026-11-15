using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// Authoring only: Pipeline run_script; Source~ is excluded from runtime compilation.
public static class SetupRedesignV2
{
    const string Prefab="Assets/Katsuro/Prefabs/UI/RunSetupCanvas.prefab";
    const string Art="Assets/Katsuro/Art/UI/Textures/Setup/Redesign/";
    static Color C(string hex,float alpha=1){Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);c.a=alpha;return c;}
    static Transform T(Transform r,string path){var t=r.Find(path);if(t==null)throw new Exception("Missing existing view: "+path);return t;}
    static void Off(Transform t){var l=t.GetComponent<LayoutGroup>();if(l!=null)l.enabled=false;}
    static void R(Transform t,float x,float y,float w,float h,float ax=0,float ay=1,float px=0,float py=1)
    {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(ax,ay);r.pivot=new Vector2(px,py);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static void Stretch(Transform t,float left=0,float bottom=0,float right=0,float top=0)
    {var r=(RectTransform)t;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);r.localScale=Vector3.one;}
    static UnityEngine.UI.Image I(Transform t)=>t.GetComponent<UnityEngine.UI.Image>();
    static void HideImage(Transform t){var i=I(t);if(i!=null)i.color=Color.clear;}
    static Sprite S(string n)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+n+".png");
    static void Font(Transform t,float size,string color="E8E3DA",bool wrap=false)
    {var text=t.GetComponent<TMP_Text>();text.fontSize=size;text.enableAutoSizing=false;text.color=C(color);text.textWrappingMode=wrap?TextWrappingModes.Normal:TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;text.characterSpacing=0;}
    static void Import(string n,Vector4 border)
    {
        string p=Art+n+".png";AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(p);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
        i.spriteBorder=border;i.spritePixelsPerUnit=100;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.sRGBTexture=true;
        i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;
        i.maxTextureSize=4096;var st=new TextureImporterSettings();i.ReadTextureSettings(st);st.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(st);i.SaveAndReimport();
    }
    public static string Materials()
    {
        Import("InkPaper",Vector4.zero);Import("FocusBrush",Vector4.zero);
        foreach(string state in new[]{"Normal","Selected","Pressed","Disabled"})
        {
            int w=256,h=96;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                var c=C("171719",state=="Normal"?.28f:state=="Disabled"?.12f:.45f);
                if(state=="Selected")c=C("45282A",.35f);
                if(state=="Pressed")c=C("663234",.50f);
                if(y==3&&x>8&&x<w-9)
                {
                    float fade=Mathf.Clamp01(Math.Min(x-8,w-9-x)/20f);
                    c=state=="Normal"?C("B59A66",.5f*fade):state=="Disabled"?C("77716A",((x/8)%2==0?.30f:.05f)*fade):C("9E3535",fade);
                }
                pixels[y*w+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(Art+"Action"+state+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            Import("Action"+state,new Vector4(32,8,32,8));
        }
        return "Imported paper, transparent dry brush, and four sliced action states.";
    }
    static void Heading(Transform t)
    {
        Off(t);R(T(t,"Mark"),0,-13,15,15);I(T(t,"Mark")).color=C("B59A66",.7f);
        R(T(t,"Label"),32,0,270,46);Font(T(t,"Label"),32);
        R(T(t,"Line"),310,-33,180,1);I(T(t,"Line")).color=C("B59A66",.22f);
        R(T(t,"Hint"),0,-12,220,30,1,1,1,1);Font(T(t,"Hint"),19,"BDB7AC");
    }
    public static void Layout(GameObject root)
    {
        var r=root.transform;var content=T(r,"Content");Off(content);Stretch(content,96,44,96,36);
        var backdrop=I(T(r,"Backdrop"));backdrop.sprite=S("InkPaper");backdrop.color=Color.white;backdrop.type=UnityEngine.UI.Image.Type.Simple;
        I(T(r,"Backdrop/Tint")).color=C("0D0D0F",.28f);
        var header=T(content,"Header");Off(header);R(header,0,0,1728,128);
        var title=T(header,"TitleBlock");Off(title);R(title,0,0,730,128);
        var eyebrow=T(title,"Eyebrow");Off(eyebrow);R(eyebrow,0,0,500,28);
        R(T(eyebrow,"Seal"),0,-7,14,14);R(T(eyebrow,"Text"),30,0,460,28);Font(T(eyebrow,"Text"),20,"B59A66");
        R(T(title,"Heading"),0,-31,730,94);Font(T(title,"Heading"),70);
        var nb=T(header,"NameBlock");Off(nb);R(nb,0,-28,770,70,1,1,1,1);
        R(T(nb,"NameLabel"),0,-15,146,40);Font(T(nb,"NameLabel"),23,"B59A66");
        var ni=T(nb,"NameInput");R(ni,156,0,614,64);I(ni).sprite=null;I(ni).type=UnityEngine.UI.Image.Type.Simple;I(ni).color=C("171719",.55f);
        HideImage(T(ni,"Frame"));I(T(ni,"Underline")).color=C("B59A66",.55f);
        Font(T(ni,"Viewport/Text"),26);Font(T(ni,"Viewport/Placeholder"),26,"BDB7AC");
        R(T(content,"HeaderRule"),0,-146,1728,1);I(T(content,"HeaderRule")).color=C("B59A66",.38f);
        var body=T(content,"Body");Off(body);R(body,0,-172,1728,692);
        var left=T(body,"Blessings");Off(left);R(left,0,0,790,692);
        R(T(left,"AttackHeading"),0,0,790,44);Heading(T(left,"AttackHeading"));
        R(T(left,"AttackOptions"),0,-56,790,252);Off(T(left,"AttackOptions"));
        R(T(left,"Gap"),0,-308,790,32);
        R(T(left,"DefenseHeading"),0,-340,790,44);Heading(T(left,"DefenseHeading"));
        R(T(left,"DefenseOptions"),0,-396,790,252);Off(T(left,"DefenseOptions"));
        foreach(string kind in new[]{"Attack","Defense"})for(int j=0;j<3;j++)
        {
            var card=T(left,kind+"Options/"+kind+"_"+j);R(card,0,-j*84,790,84);
            var image=I(card);image.sprite=null;image.type=UnityEngine.UI.Image.Type.Simple;image.color=C("171719",.12f);
            var baseFrame=T(card,"BaseFrame");R(baseFrame,44,-83,746,1);I(baseFrame).sprite=null;I(baseFrame).type=UnityEngine.UI.Image.Type.Simple;I(baseFrame).color=C("B59A66",.14f);
            var hover=T(card,"HoverLayer");R(hover,-26,108,842,300);I(hover).sprite=S("FocusBrush");I(hover).type=UnityEngine.UI.Image.Type.Simple;I(hover).color=new Color(1,1,1,.42f);
            var selected=T(card,"Selected");Stretch(selected);
            HideImage(T(selected,"Fill"));HideImage(T(selected,"Glow"));HideImage(T(selected,"TopBar"));
            var line=T(selected,"Frame");R(line,44,-82,746,1);I(line).sprite=null;I(line).type=UnityEngine.UI.Image.Type.Simple;I(line).color=C("B59A66",.85f);
            var badge=T(selected,"Badge");R(badge,-4,-24,82,30,1,1,1,1);HideImage(badge);Font(T(badge,"Text"),18,"B59A66");
            var inner=T(card,"Content");Off(inner);Stretch(inner);
            var top=T(inner,"TopRow");Off(top);R(top,0,0,238,84);
            var emblem=T(top,"Emblem");R(emblem,44,-10,58,58);I(emblem).color=C("101012",.65f);I(T(emblem,"Frame")).color=C("B59A66",.65f);Font(T(emblem,"Text"),34);
            R(T(top,"Label"),126,-8,112,64);Font(T(top,"Label"),34);
            var effect=T(inner,"Effect");R(effect,260,-9,426,66);Font(effect,22,"D1C9BC",true);effect.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
            var focus=T(card,"FocusFrame");Stretch(focus,26,7,-6,7);I(focus).color=C("E8E3DA",.9f);
            I(T(card,"Flash")).color=C("9E3535",.10f);
        }
        var right=T(body,"Details");Off(right);R(right,0,0,850,692,1,1,1,1);
        var load=T(right,"LoadoutPanel");Off(load);R(load,0,0,850,372);HideImage(load);
        var divider=T(load,"Frame");R(divider,-38,0,1,692);I(divider).sprite=null;I(divider).type=UnityEngine.UI.Image.Type.Simple;I(divider).color=C("B59A66",.20f);
        R(T(load,"Title"),0,0,850,48);Font(T(load,"Title"),34);R(T(load,"Rule"),0,-56,850,1);I(T(load,"Rule")).color=C("B59A66",.32f);
        int row=0;foreach(string kind in new[]{"Attack","Defense"})
        {
            var entry=T(load,kind+"Row");Off(entry);R(entry,0,-82-row*146,850,132);
            var tag=T(entry,"Tag");R(tag,0,-2,56,56);I(tag).color=kind=="Attack"?C("9E3535",.65f):C("B59A66",.18f);Font(T(tag,"Text"),30);
            var text=T(entry,"Text");Off(text);Stretch(text,88,0,0,0);var head=T(text,"Head");Off(head);R(head,0,0,762,56);
            R(T(head,"Name"),0,0,126,56);Font(T(head,"Name"),38);
            R(T(head,"Description"),146,2,616,56);Font(T(head,"Description"),23,"B59A66",true);T(head,"Description").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
            R(T(text,"Detail"),0,-62,762,70);Font(T(text,"Detail"),23,"D1C9BC",true);
            row++;
        }
        var info=T(right,"InfoPanel");Off(info);R(info,0,-408,850,202);HideImage(info);HideImage(T(info,"Frame"));
        R(T(info,"Title"),0,0,850,48);Font(T(info,"Title"),34);
        // The brush font's U+639F has a nearly empty glyph; local body font keeps this heading readable.
        T(info,"Title").GetComponent<TMP_Text>().font=T(info,"Rules").GetComponent<TMP_Text>().font;
        R(T(info,"Rule"),0,-56,850,1);I(T(info,"Rule")).color=C("B59A66",.32f);
        R(T(info,"Opponent"),0,-82,850,64);Font(T(info,"Opponent"),25,"E8E3DA",true);
        R(T(info,"Rules"),0,-151,850,64);Font(T(info,"Rules"),22,"D1C9BC",true);
        var result=T(right,"ResultPanel");Off(result);R(result,0,-634,850,78);I(result).color=C("4C2428",.3f);Stretch(T(result,"Text"),20,8,12,8);Font(T(result,"Text"),21,"E1B5AA",true);
        R(T(content,"FooterRule"),0,-884,1728,1);I(T(content,"FooterRule")).color=C("B59A66",.38f);
        var footer=T(content,"Footer");Off(footer);R(footer,0,0,1728,96,0,0,0,0);
        R(T(footer,"InputHint"),0,-13,760,80);Font(T(footer,"InputHint"),20,"BDB7AC",true);
        var tutorial=T(footer,"TutorialButton");R(tutorial,-444,0,340,96,1,1,1,1);
        var start=T(footer,"StartButton");R(start,0,0,400,96,1,1,1,1);
        foreach(var btn in new[]{tutorial,start})
        {
            var image=I(btn);image.sprite=S("ActionNormal");image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Sliced;
            var b=btn.GetComponent<UnityEngine.UI.Button>();b.transition=Selectable.Transition.SpriteSwap;b.spriteState=new SpriteState{highlightedSprite=S("ActionSelected"),selectedSprite=S("ActionSelected"),pressedSprite=S("ActionPressed"),disabledSprite=S("ActionDisabled")};
            HideImage(T(btn,"Frame"));if(btn.Find("Shade")!=null)HideImage(T(btn,"Shade"));
            var hover=T(btn,"HoverLayer");Stretch(hover);I(hover).color=C("9E3535",.035f);
            Font(T(btn,"Content/Label"),btn==start?40:36);Font(T(btn,"Content/Caption"),18,"BDB7AC");
        }
        // Explicit navigation follows the six vertical rows, retaining the same handlers.
        var choices=new List<Selectable>();foreach(string kind in new[]{"Attack","Defense"})for(int j=0;j<3;j++)choices.Add(T(left,kind+"Options/"+kind+"_"+j).GetComponent<Selectable>());
        var input=ni.GetComponent<Selectable>();var tb=tutorial.GetComponent<Selectable>();var sb=start.GetComponent<Selectable>();
        for(int j=0;j<choices.Count;j++){var nav=choices[j].navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=j==0?input:choices[j-1];nav.selectOnDown=j==5?sb:choices[j+1];nav.selectOnLeft=null;nav.selectOnRight=sb;choices[j].navigation=nav;}
        var n=input.navigation;n.selectOnDown=choices[0];input.navigation=n;
        n=tb.navigation;n.selectOnUp=choices[4];n.selectOnRight=sb;tb.navigation=n;
        n=sb.navigation;n.selectOnUp=choices[5];n.selectOnLeft=tb;sb.navigation=n;
    }
    public static string Apply()
    {
        string original=File.ReadAllText(Prefab).Replace("\r\n","\n");var root=PrefabUtility.LoadPrefabContents(Prefab);
        try{Layout(root);PrefabUtility.SaveAsPrefabAsset(root,Prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
        // Preserve original string encoding, IDs, component order and all functional settings.
        string styled=File.ReadAllText(Prefab).Replace("\r\n","\n");
        string pat=@"(?ms)^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\z)";
        var docs=Regex.Matches(styled,pat).Cast<Match>().ToDictionary(m=>m.Groups[2].Value,m=>m.Value);
        string viewKeys=@"m_Enabled|m_Color|m_Sprite|m_Type|m_fontColor|rgba|m_fontAsset|m_sharedMaterial|m_fontSize|m_fontSizeBase|m_enableAutoSizing|m_characterSpacing|m_TextWrappingMode|m_overflowMode|m_HorizontalAlignment|m_VerticalAlignment|m_AnchorMin|m_AnchorMax|m_Pivot|m_AnchoredPosition|m_SizeDelta|m_LocalScale|m_Transition|m_HighlightedSprite|m_PressedSprite|m_SelectedSprite|m_DisabledSprite|m_SelectOnUp|m_SelectOnDown|m_SelectOnLeft|m_SelectOnRight";
        var final=Regex.Replace(original,pat,m=>{
            string block=m.Value;string next=docs[m.Groups[2].Value];
            bool visual=m.Groups[1].Value=="224"||Regex.IsMatch(block,@"m_EditorClassIdentifier: .*?(Image|TextMeshProUGUI|LayoutGroup|Button|Toggle|TMP_InputField)\s*\n");
            if(!visual)return block;
            foreach(Match line in Regex.Matches(next,@"(?m)^( +)("+viewKeys+@"):.*$"))block=Regex.Replace(block,@"(?m)^"+line.Groups[1].Value+line.Groups[2].Value+@":.*$",unused=>line.Value);
            return block;
        });
        Directory.CreateDirectory("Temp/SetupRedesign");
        File.WriteAllText("Temp/SetupRedesign/normalized.prefab.txt",final.Replace("\n","\r\n"),new System.Text.UTF8Encoding(false));
        return "Updated existing views, six-row navigation and sprites; original hierarchy, copy, runtime logic and event bindings preserved.";
    }
}
