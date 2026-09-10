using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mock.UI;

public static class ThemeInkAuthor
{
    static readonly Color Ivory=new Color(.88f,.85f,.77f);
    static readonly Color Vermilion=new Color(.67f,.16f,.12f);
    static Sprite ink, frame, seal;
    static TMP_FontAsset font;
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var existing = parent != null ? parent.Find(name) : null;
        var go = existing != null ? existing.gameObject : new GameObject(name,typeof(RectTransform)); go.layer=5;
        var r=(RectTransform)go.transform; r.SetParent(parent,false);
        r.anchoredPosition=position; r.sizeDelta=size; return r;
    }
    static Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color,Sprite sprite=null)
    {
        var go=Rect(name,parent,position,size).gameObject;
        var im=go.GetComponent<Image>() ?? go.AddComponent<Image>();
        im.color=color; im.sprite=sprite; im.raycastTarget=false; return im;
    }
    static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float sizeText=22)
    {
        var go=Rect(name,parent,position,size).gameObject;
        var t=go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        t.font=font; t.text=value; t.fontSize=sizeText; t.color=Ivory;
        t.alignment=TextAlignmentOptions.MidlineLeft; t.raycastTarget=false; t.richText=false; return t;
    }
    static void Place(Transform root,string path,Vector2 point,Vector2 size)
    {
        var r=(RectTransform)root.Find(path); r.anchoredPosition=point; r.sizeDelta=size;
    }
    static Sprite Texture(string name,int width,int height,int kind)
    {
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
        {
            float nx=x/(float)(width-1), ny=y/(float)(height-1);
            float edge=Mathf.Min(Mathf.Min(x,width-1-x),Mathf.Min(y,height-1-y));
            float noise=Mathf.PerlinNoise(x*.073f+5,y*.093f+11);
            float alpha=Mathf.Clamp01((edge-1-noise*5)/3f)*(.88f+.12f*noise);
            if(kind==1)
            {
                float stroke=Mathf.Clamp01(1-Mathf.Abs(edge-4-noise*2)/2.5f);
                bool stamp=nx>.88f && nx<.98f && ny>.58f && ny<.92f;
                if(stamp) {
                    float sx=(nx-.88f)/.1f, sy=(ny-.58f)/.34f;
                    bool mark=sx<.09f||sx>.91f||sy<.08f||sy>.92f
                        || (sx>.3f&&sx<.39f&&sy>.2f&&sy<.75f)
                        || (sy>.45f&&sy<.54f&&sx>.22f&&sx<.77f)
                        || (sx>.65f&&sx<.73f&&sy>.3f&&sy<.82f);
                    stroke=Mathf.Max(stroke,mark ? .8f:0);
                }
                alpha=stroke*(.72f+.28f*noise);
            }
            if(kind==2) {
                float d=Mathf.Max(Mathf.Abs(nx-.5f),Mathf.Abs(ny-.5f));
                alpha=((d>.32f&&d<.43f)||(nx>.27f&&nx<.35f&&ny>.24f&&ny<.74f)
                    ||(ny>.45f&&ny<.54f&&nx>.24f&&nx<.73f)
                    ||(nx>.64f&&nx<.73f&&ny>.3f&&ny<.76f))?(.65f+.35f*noise):0;
            }
            texture.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        texture.Apply();
        string path="Assets/Mock/UI/Ink/"+name+".png";
        File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Ref(SerializedObject so,string key,UnityEngine.Object obj) => so.FindProperty(key).objectReferenceValue=obj;
    static Image Bar(Transform parent,string name,Vector2 point,Vector2 size,Color fillColor)
    {
        Image(name+"Frame",parent,point,size+new Vector2(4,4),new Color(.47f,.44f,.38f,.85f),ink);
        Image(name+"Track",parent,point,size,new Color(.04f,.04f,.04f,1),ink);
        var fill=Image(name+"Fill",parent,point,size,fillColor,ink);
        fill.type=UnityEngine.UI.Image.Type.Filled; fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin=0; fill.fillAmount=1; return fill;
    }
    public static void Run()
    {
        try {
            Directory.CreateDirectory("Assets/Mock/UI/Ink");
            ink=Texture("InkPlate",512,192,0); frame=Texture("SelectionFrame",512,176,1); seal=Texture("LifeSeal",64,64,2);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Mock/UI/KatsuroUIFont.asset");
            var root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/RunSetupCanvas.prefab");
            var panel=root.transform.Find("PreparationPanel");
            Place(root.transform,"PreparationPanel",Vector2.zero,new Vector2(1080,580));
            var background=panel.GetComponent<Image>(); background.sprite=ink; background.color=new Color(.025f,.026f,.029f,.97f);
            root.transform.Find("Backdrop").GetComponent<Image>().color=new Color(.012f,.015f,.02f,.93f);
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true)) label.color=Ivory;
            Place(panel,"Heading",new Vector2(-290,224),new Vector2(390,64));
            panel.Find("Heading").GetComponent<TMP_Text>().fontSize=42;
            Place(panel,"NameLabel",new Vector2(85,224),new Vector2(65,32));
            panel.Find("NameLabel").GetComponent<TMP_Text>().text="名前";
            Place(panel,"NameInput",new Vector2(295,224),new Vector2(330,42));
            Place(panel,"NameInput/Viewport",Vector2.zero,new Vector2(306,38));
            Place(panel,"NameInput/Viewport/Text",Vector2.zero,new Vector2(306,38));
            Place(panel,"NameInput/Viewport/Placeholder",Vector2.zero,new Vector2(306,38));
            panel.Find("NameInput").GetComponent<Image>().color=new Color(.1f,.1f,.105f,.9f);
            Image("HeaderRule",panel,new Vector2(0,176),new Vector2(940,1),new Color(.55f,.52f,.44f,.35f));
            Place(panel,"AttackHeading",new Vector2(-382,113),new Vector2(190,40));
            Place(panel,"DefenseHeading",new Vector2(-382,-14),new Vector2(190,40));
            string[] glyphs={"力","魂","刃","守","霊","息"};
            for(int i=0;i<3;i++)
            {
                foreach(bool attack in new[]{true,false})
                {
                    var option=panel.Find(attack?"Attack_"+i:"DefenseOptions/Defense_"+i);
                    var r=(RectTransform)option; r.anchoredPosition=new Vector2(-156+i*247,attack?113:-14); r.sizeDelta=new Vector2(230,72);
                    option.GetComponent<Image>().sprite=ink;
                    option.GetComponent<Image>().color=new Color(.17f,.175f,.18f,.8f);
                    var selected=option.Find("Selected").GetComponent<Image>();
                    selected.sprite=frame; selected.color=Vermilion;
                    selected.rectTransform.anchoredPosition=Vector2.zero; selected.rectTransform.sizeDelta=new Vector2(236,78);
                    var label=option.Find("Label").GetComponent<TMP_Text>();
                    label.rectTransform.anchoredPosition=new Vector2(30,0); label.rectTransform.sizeDelta=new Vector2(140,52); label.fontSize=26;
                    var glyph=Text("Emblem",option,glyphs[i+(attack?0:3)],new Vector2(-70,0),new Vector2(54,52),34); glyph.color=new Color(.67f,.66f,.61f);
                    var colors=option.GetComponent<Toggle>().colors;
                    colors.highlightedColor=new Color(1.35f,1.25f,1.1f);
                    colors.selectedColor=new Color(1.25f,1.16f,1.05f);
                    option.GetComponent<Toggle>().colors=colors;
                }
            }
            Place(panel,"AttackDescription",new Vector2(100,61),new Vector2(730,34));
            Place(panel,"DefenseDescription",new Vector2(100,-66),new Vector2(730,34));
            panel.Find("AttackDescription").GetComponent<TMP_Text>().fontSize=18;
            panel.Find("DefenseDescription").GetComponent<TMP_Text>().fontSize=18;
            Image("MiddleRule",panel,new Vector2(0,36),new Vector2(940,1),new Color(.55f,.52f,.44f,.25f));
            Image("LowerRule",panel,new Vector2(0,-94),new Vector2(940,1),new Color(.55f,.52f,.44f,.25f));
            Place(panel,"Opponent",new Vector2(0,-123),new Vector2(900,32));
            panel.Find("Opponent").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            Place(panel,"Rules",new Vector2(0,-156),new Vector2(900,26));
            panel.Find("Rules").GetComponent<TMP_Text>().fontSize=15; panel.Find("Rules").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            Place(panel,"Result",new Vector2(0,-182),new Vector2(940,32));
            panel.Find("Result").GetComponent<TMP_Text>().fontSize=14; panel.Find("Result").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            Place(panel,"StartButton",new Vector2(0,-228),new Vector2(440,56));
            panel.Find("StartButton").GetComponent<Image>().sprite=ink; panel.Find("StartButton").GetComponent<Image>().color=new Color(.47f,.095f,.065f,1);
            Place(panel,"StartButton/Label",Vector2.zero,new Vector2(410,50));
            Place(panel,"InputHint",new Vector2(0,-272),new Vector2(900,24));
            panel.Find("InputHint").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center; panel.Find("InputHint").GetComponent<TMP_Text>().fontSize=14;
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/RunSetupCanvas.prefab");
            PrefabUtility.UnloadPrefabContents(root);

            root=new GameObject("BattleHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
            root.layer=5;
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=10;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
            var player=Rect("PlayerStatus",root.transform,new Vector2(32,-26),new Vector2(330,134));
            player.anchorMin=player.anchorMax=new Vector2(0,1); player.pivot=new Vector2(0,1);
            // Child coordinates are relative to the status group's center.
            Image("InkBacking",player,new Vector2(-5,8),new Vector2(364,156),new Color(0,0,0,.54f),ink);
            var name=Text("PlayerName",player,"挑戦者",new Vector2(-50,53),new Vector2(220,32),24);
            var hpText=Text("HP",player,"HP 24",new Vector2(115,53),new Vector2(100,30),20); hpText.alignment=TextAlignmentOptions.MidlineRight;
            var hp=Bar(player,"Health",new Vector2(0,25),new Vector2(326,12),new Color(.67f,.18f,.13f)); hp.fillAmount=.24f;
            var sp=Bar(player,"Skill",new Vector2(24,-2),new Vector2(276,6),new Color(.48f,.78f,.8f)); sp.fillAmount=.8f;
            var life1=Image("LifeSeal1",player,new Vector2(-154,-2),new Vector2(25,25),Vermilion,seal);
            var life2=Image("LifeSeal2",player,new Vector2(-131,-2),new Vector2(25,25),Vermilion,seal);
            var gear=Text("Equipment",player,"剛力 / 堅守",new Vector2(0,-29),new Vector2(326,23),16); gear.color=new Color(.64f,.62f,.56f);
            var ghost=Text("GhostStatus",player,"",new Vector2(0,-54),new Vector2(326,23),16); ghost.color=new Color(.55f,.85f,.85f);
            var boss=Rect("EnemyStatus",root.transform,new Vector2(0,-25),new Vector2(480,62));
            boss.anchorMin=boss.anchorMax=new Vector2(.5f,1); boss.pivot=new Vector2(.5f,1);
            Image("InkBacking",boss,new Vector2(0,2),new Vector2(530,85),new Color(0,0,0,.42f),ink);
            var enemyName=Text("EnemyName",boss,"名もなき守人",new Vector2(0,14),new Vector2(480,32),23); enemyName.alignment=TextAlignmentOptions.Center;
            var enemyHp=Bar(boss,"EnemyHealth",new Vector2(0,-12),new Vector2(480,11),new Color(.63f,.15f,.12f)); enemyHp.fillAmount=.68f;
            var hud=root.AddComponent<RunHUD>(); var data=new SerializedObject(hud);
            Ref(data,"_visibility",root.GetComponent<CanvasGroup>()); Ref(data,"_challenger",name); Ref(data,"_opponent",enemyName);
            Ref(data,"_health",hpText); Ref(data,"_equipment",gear); Ref(data,"_ghostStatus",ghost); Ref(data,"_enemyHpFill",enemyHp);
            var seals=data.FindProperty("_lifeSeals"); seals.arraySize=2; seals.GetArrayElementAtIndex(0).objectReferenceValue=life1; seals.GetArrayElementAtIndex(1).objectReferenceValue=life2;
            data.ApplyModifiedPropertiesWithoutUndo();
            var playerView=root.AddComponent<PlayerHUDView>(); data=new SerializedObject(playerView);
            Ref(data,"_hpFill",hp); Ref(data,"_skillFill",sp); data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/BattleHUD.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("INK_UI_RESULT: setup theme and BattleHUD saved");
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
