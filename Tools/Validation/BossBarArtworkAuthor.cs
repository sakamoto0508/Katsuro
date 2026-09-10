using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BossBarArtworkAuthor
{
    static Sprite Slice(string path,string name,Rect rect)
    {
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
#pragma warning disable 618
        importer.spritesheet=name=="BossFrame" ? new[]{new SpriteMetaData{name=name,rect=rect,pivot=new Vector2(.5f,.5f)},new SpriteMetaData{name="EmptyTrack",rect=new Rect(135,328,1510,60),pivot=new Vector2(.5f,.5f)}} : new[]{new SpriteMetaData{name=name,rect=rect,pivot=new Vector2(.5f,.5f)}};
#pragma warning restore 618
        importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>s.name==name);
    }
    public static void Run()
    {
        try {
            var frame=Slice("Assets/Mock/UI/EnemyBarEmpty.png","BossFrame",new Rect(78,285,2017,199));
            var red=Slice("Assets/Mock/UI/EnemyBarReference.png","BossRed",new Rect(135,328,1510,60));
            var root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/BattleHUD.prefab");
            var enemy=root.transform.Find("EnemyStatus");
            var frameImage=enemy.Find("EnemyHealthFrame").GetComponent<Image>();
            frameImage.sprite=frame;frameImage.type=Image.Type.Simple;frameImage.color=Color.white;
            frameImage.rectTransform.sizeDelta=new Vector2(500,49.33f);frameImage.rectTransform.anchoredPosition=new Vector2(0,-12);
            frameImage.transform.SetSiblingIndex(1); if(frameImage.GetComponent<BossBarSilhouette>()==null)frameImage.gameObject.AddComponent<BossBarSilhouette>();
            var track=enemy.Find("EnemyHealthTrack").GetComponent<Image>(); track.gameObject.SetActive(true);track.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Mock/UI/EnemyBarEmpty.png").OfType<Sprite>().First(s=>s.name=="EmptyTrack");track.color=Color.white;track.rectTransform.sizeDelta=new Vector2(479,15);track.rectTransform.anchoredPosition=new Vector2(0,-18.5f);track.transform.SetAsLastSibling();
            foreach(string name in new[]{"EnemyDamageTrail","EnemyHealthFill"})
            {
                var im=enemy.Find(name).GetComponent<Image>();im.sprite=red;
                im.color=name=="EnemyHealthFill"?Color.white:new Color(.025f,.025f,.025f,1);
                im.rectTransform.sizeDelta=new Vector2(479,15);im.rectTransform.anchoredPosition=new Vector2(0,-18.5f);
                im.transform.SetAsLastSibling();
            }
            ((RectTransform)enemy.Find("EnemyName")).anchoredPosition=new Vector2(0,33);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/BattleHUD.prefab");PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();Debug.Log("BOSS_ART_RESULT: success");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
