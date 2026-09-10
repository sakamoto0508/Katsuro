using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mock.UI;

public static class LegacyHealthAuthor
{
    static void Ref(SerializedObject so,string field,UnityEngine.Object obj)=>so.FindProperty(field).objectReferenceValue=obj;
    static Image Image(string name,Transform parent,Vector2 point,Vector2 size,Sprite sprite)
    {
        var existing=parent.Find(name);
        var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform));
        go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchoredPosition=point;r.sizeDelta=size;
        var im=go.GetComponent<Image>()??go.AddComponent<Image>();im.sprite=sprite;im.color=Color.white;im.raycastTarget=false;return im;
    }
    static void Remove(Transform root,string path)
    {
        var child=root.Find(path);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);
    }
    static Sprite Selection()
    {
        const int w=512,h=176;
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float edge=Mathf.Min(Mathf.Min(x,w-1-x),Mathf.Min(y,h-1-y));
            bool corner=(x<8||x>w-9)&&(y<8||y>h-9);
            float a=corner?0:Mathf.Clamp01(1-Mathf.Abs(edge-2)/1.4f);
            Color color=new Color(.62f,.46f,.28f,a*.9f);
            if(y>=2&&y<=5&&x>14&&x<w-15)color=new Color(.73f,.18f,.12f,.95f);
            texture.SetPixel(x,y,color);
        }
        texture.Apply();
        string path="Assets/Mock/UI/Ink/SelectionOutline.png";
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void Run()
    {
        try
        {
            string atlas="Assets/Mock/UI/LegacyHealthAtlas.png";
            AssetDatabase.ImportAsset(atlas);
            var importer=(TextureImporter)AssetImporter.GetAtPath(atlas);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
#pragma warning disable 618
            importer.spritesheet=new[]{
                new SpriteMetaData {name="Green",rect=new Rect(452,860,765,50),pivot=new Vector2(.5f,.5f)},
                new SpriteMetaData {name="Red",rect=new Rect(451,619,765,50),pivot=new Vector2(.5f,.5f)},
                new SpriteMetaData {name="Empty",rect=new Rect(451,401,765,50),pivot=new Vector2(.5f,.5f)},
                new SpriteMetaData {name="Frame",rect=new Rect(320,160,915,110),pivot=new Vector2(.5f,.5f)},
                new SpriteMetaData {name="LifeOrb",rect=new Rect(1007,130,30,30),pivot=new Vector2(.5f,.5f)}
            };
#pragma warning restore 618
            importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(atlas).OfType<Sprite>().ToDictionary(s=>s.name);
            if(sprites.Count!=5)throw new Exception("Legacy atlas slice import failed");
            var root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/BattleHUD.prefab");
            var player=root.transform.Find("PlayerStatus");
            ((RectTransform)player).anchoredPosition=new Vector2(24,-26);
            ((RectTransform)player).sizeDelta=new Vector2(350,134);
            var ink=player.Find("InkBacking").GetComponent<Image>();ink.color=new Color(0,0,0,.25f);
            ink.rectTransform.sizeDelta=new Vector2(378,185);ink.rectTransform.anchoredPosition=new Vector2(0,-7);
            var name=player.Find("PlayerName").GetComponent<TMP_Text>();
            name.rectTransform.anchoredPosition=new Vector2(-65,59);
            var hpText=player.Find("HP").GetComponent<TMP_Text>();hpText.rectTransform.anchoredPosition=new Vector2(125,59);
            Remove(player,"HealthTrack");Remove(player,"LifeSeal1");Remove(player,"LifeSeal2");
            float scale=350f/915f;
            var frame=Image("HealthFrame",player,new Vector2(0,25),new Vector2(350,110*scale),sprites["Frame"]);
            frame.transform.SetSiblingIndex(1);
            var point=new Vector2(56*scale,25-12*scale);
            var size=new Vector2(765*scale,68*scale);
            var empty=Image("HealthEmpty",player,point,size,sprites["Empty"]);empty.transform.SetSiblingIndex(2);
            var red=Image("DamageTrail",player,point,size,sprites["Red"]);red.transform.SetSiblingIndex(3);
            var green=Image("HealthFill",player,point,size,sprites["Green"]);green.transform.SetSiblingIndex(4);
            foreach(var im in new[]{green,red}) {
                im.type=UnityEngine.UI.Image.Type.Filled;im.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;im.fillOrigin=0;
            }
            green.fillAmount=.62f;red.fillAmount=.86f;
            var orbs=new Image[3];
            for(int i=0;i<3;i++)
            {
                orbs[i]=Image("LifeOrb"+(i+1),player,new Vector2((244.5f+i*46)*scale,25-70*scale),new Vector2(30,30)*scale,sprites["LifeOrb"]);
                orbs[i].color=i<2?Color.white:new Color(.16f,.16f,.16f,.8f);
            }
            foreach(string path in new[]{"SkillFrame","SkillTrack","SkillFill"})
                ((RectTransform)player.Find(path)).anchoredPosition=new Vector2(24,-28);
            ((RectTransform)player.Find("Equipment")).anchoredPosition=new Vector2(0,-55);
            ((RectTransform)player.Find("GhostStatus")).anchoredPosition=new Vector2(0,-79);
            var view=root.GetComponent<PlayerHUDView>();var data=new SerializedObject(view);
            Ref(data,"_hpFill",green);Ref(data,"_damageFill",red);
            data.FindProperty("_damageDelay").floatValue=.25f;data.FindProperty("_damageCatchupPerSecond").floatValue=.65f;
            data.ApplyModifiedPropertiesWithoutUndo();
            data=new SerializedObject(root.GetComponent<RunHUD>());
            var refs=data.FindProperty("_lifeOrbs");refs.arraySize=3;
            for(int i=0;i<3;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=orbs[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/BattleHUD.prefab");PrefabUtility.UnloadPrefabContents(root);
            var selected=Selection();
            root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/RunSetupCanvas.prefab");
            var setup=new SerializedObject(root.GetComponent<RunSetupUI>());
            foreach(string field in new[]{"_attackOptions","_defenseOptions"}) {
                var options=setup.FindProperty(field);
                for(int i=0;i<options.arraySize;i++){
                    var toggle=(Toggle)options.GetArrayElementAtIndex(i).objectReferenceValue;
                    var im=(Image)toggle.graphic;im.sprite=selected;im.color=Color.white;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/RunSetupCanvas.prefab");PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log("LEGACY_HEALTH_AUTHOR: saved original-image bar, damage trail, life orbs and revised selection");
            EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
