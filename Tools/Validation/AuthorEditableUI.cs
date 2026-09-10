using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class AuthorEditableUI
{
    private static TMP_FontAsset font;
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float pointSize = 22)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = pointSize; label.text = text;
        label.color = new Color(.92f,.94f,.96f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.richText = false; label.raycastTarget = false;
        return label;
    }
    private static Image Background(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    private static GameObject Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        return go;
    }
    private static void Ref(SerializedObject data, string field, UnityEngine.Object value) => data.FindProperty(field).objectReferenceValue = value;
    private static void Refs(SerializedObject data, string field, UnityEngine.Object[] values)
    {
        var property = data.FindProperty(field); property.arraySize = values.Length;
        for(int i=0;i<values.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static Toggle Option(string name, Transform parent, string title, Vector2 position, ToggleGroup group)
    {
        var rect = Rect(name, parent, position, new Vector2(275,44));
        var image = Background(rect, new Color(.13f,.18f,.23f,1));
        var toggle = rect.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = image; toggle.group = group;
        var mark = Rect("Selected", rect, new Vector2(-116,0), new Vector2(12,12));
        toggle.graphic = Background(mark, new Color(.35f,.9f,.95f));
        Label("Label", rect, title, new Vector2(10,0), new Vector2(230,38),23);
        return toggle;
    }
    public static void Run()
    {
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Japanese.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 8, GlyphRenderMode.SDFAA, 1024,1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Katsuro UI Font";
            AssetDatabase.CreateAsset(font, "Assets/Mock/UI/KatsuroUIFont.asset");
            foreach(var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            var setup = Canvas("RunSetupCanvas", 200);
            var visibility = setup.AddComponent<CanvasGroup>();
            var shade = Rect("Backdrop", setup.transform, Vector2.zero, Vector2.zero);
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
            Background(shade, new Color(.025f,.035f,.05f,1));
            var panel = Rect("PreparationPanel", setup.transform, Vector2.zero, new Vector2(980,680));
            Background(panel, new Color(.065f,.08f,.105f,1));
            Label("Heading", panel, "挑戦の準備", new Vector2(0,286), new Vector2(880,58),36);
            Label("NameLabel", panel,"名前（16文字まで）",new Vector2(0,237),new Vector2(880,30),20);
            var inputRect=Rect("NameInput",panel,new Vector2(0,194),new Vector2(880,46));
            var inputImage=Background(inputRect,new Color(.14f,.18f,.22f,1));
            var viewport=Rect("Viewport",inputRect,Vector2.zero,new Vector2(846,42));
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText=Label("Text",viewport,"挑戦者",Vector2.zero,new Vector2(846,42),24);
            var placeholder=Label("Placeholder",viewport,"名前を入力",Vector2.zero,new Vector2(846,42),24);
            placeholder.color=new Color(.55f,.6f,.65f);
            var input=inputRect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic=inputImage; input.textViewport=viewport; input.textComponent=(TextMeshProUGUI)inputText;
            input.placeholder=placeholder; input.fontAsset=font; input.characterLimit=16; input.text="挑戦者";
            Label("AttackHeading",panel,"攻撃の加護",new Vector2(0,143),new Vector2(880,30),22);
            var ag=panel.gameObject.AddComponent<ToggleGroup>();
            var defenseRoot=Rect("DefenseOptions",panel,Vector2.zero,new Vector2(980,680));
            var dg=defenseRoot.gameObject.AddComponent<ToggleGroup>();
            var attacks=new Toggle[3]; var defenses=new Toggle[3];
            for(int i=0;i<3;i++)
            {
                attacks[i]=Option("Attack_"+i,panel,RunSession.AttackNames[i],new Vector2(-300+i*300,99),ag);
                defenses[i]=Option("Defense_"+i,defenseRoot,RunSession.DefenseNames[i],new Vector2(-300+i*300,-34),dg);
            }
            attacks[0].isOn=true; defenses[0].isOn=true;
            var attackDescription=Label("AttackDescription",panel,RunSession.AttackDescription(0),new Vector2(0,57),new Vector2(880,35),20);
            Label("DefenseHeading",panel,"防御の加護",new Vector2(0,10),new Vector2(880,30),22);
            var defenseDescription=Label("DefenseDescription",panel,RunSession.DefenseDescription(0),new Vector2(0,-76),new Vector2(880,35),20);
            var opponent=Label("Opponent",panel,"最初の相手：名もなき守人",new Vector2(0,-127),new Vector2(880,46),20);
            var rules=Label("Rules",panel,"命は2つ。勝利すると痕跡を更新。",new Vector2(0,-170),new Vector2(880,38),18);
            var result=Label("Result",panel,"",new Vector2(0,-210),new Vector2(880,44),18);
            var startRect=Rect("StartButton",panel,new Vector2(0,-266),new Vector2(880,52));
            var startImage=Background(startRect,new Color(.12f,.37f,.44f));
            var start=startRect.gameObject.AddComponent<Button>(); start.targetGraphic=startImage;
            var startLabel=Label("Label",startRect,"この装備で挑む",Vector2.zero,new Vector2(860,46),25); startLabel.alignment=TextAlignmentOptions.Center;
            Label("InputHint",panel,"マウス / キーボード・ゲームパッドで選択",new Vector2(0,-312),new Vector2(880,28),16);
            var view=setup.AddComponent<RunSetupUI>();
            var data=new SerializedObject(view);
            Ref(data,"_panel",visibility); Ref(data,"_nameInput",input); Refs(data,"_attackOptions",attacks); Refs(data,"_defenseOptions",defenses);
            Ref(data,"_attackDescription",attackDescription); Ref(data,"_defenseDescription",defenseDescription);
            Ref(data,"_opponent",opponent); Ref(data,"_rules",rules); Ref(data,"_result",result); Ref(data,"_startButton",start);
            data.ApplyModifiedPropertiesWithoutUndo();
            // Predictable keyboard and gamepad navigation.
            Navigation Link(Selectable up,Selectable down,Selectable left,Selectable right) => new Navigation { mode=Navigation.Mode.Explicit,selectOnUp=up,selectOnDown=down,selectOnLeft=left,selectOnRight=right };
            input.navigation=Link(start,attacks[0],null,null);
            for(int i=0;i<3;i++)
            {
                attacks[i].navigation=Link(input,defenses[i],attacks[(i+2)%3],attacks[(i+1)%3]);
                defenses[i].navigation=Link(attacks[i],start,defenses[(i+2)%3],defenses[(i+1)%3]);
            }
            start.navigation=Link(defenses[0],input,null,null);
            PrefabUtility.SaveAsPrefabAsset(setup,"Assets/Mock/UI/RunSetupCanvas.prefab");
            UnityEngine.Object.DestroyImmediate(setup);

            var damage=Canvas("DamageNumbersCanvas",100);
            UnityEngine.Object.DestroyImmediate(damage.GetComponent<GraphicRaycaster>());
            var container=Rect("Numbers",damage.transform,Vector2.zero,Vector2.zero);
            container.anchorMin=Vector2.zero; container.anchorMax=Vector2.one;
            var normal=Label("NormalTemplate",container,"123",Vector2.zero,new Vector2(180,70),30);
            normal.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            normal.alignment=TextAlignmentOptions.Center; normal.color=Color.white;
            var critical=Label("CriticalTemplate",container,"456",Vector2.zero,new Vector2(180,70),38);
            critical.font=normal.font; critical.alignment=TextAlignmentOptions.Center; critical.color=new Color(1,.75f,.15f);
            // Scene-authored previews. Runtime hides the templates and only shows pooled copies.
            var numbers=damage.AddComponent<DamageNumbers>(); data=new SerializedObject(numbers);
            Ref(data,"_container",container); Ref(data,"_normalTemplate",normal); Ref(data,"_criticalTemplate",critical);
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(damage,"Assets/Mock/UI/DamageNumbersCanvas.prefab");
            UnityEngine.Object.DestroyImmediate(damage);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Debug.Log("AUTHOR_UI_RESULT: both uGUI prefabs and padded Japanese font saved");
            EditorApplication.Exit(0);
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
