using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mock.UI;

public static class EnemyGaugeAuthor
{
    static void Ref(object target,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject((UnityEngine.Object)target);
        so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Run()
    {
        try {
            var root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/BattleHUD.prefab");
            var enemy=root.transform.Find("EnemyStatus");
            var fill=enemy.Find("EnemyHealthFill").GetComponent<Image>();
            var trail=UnityEngine.Object.Instantiate(fill.gameObject,enemy).GetComponent<Image>();
            trail.name="EnemyDamageTrail";trail.transform.SetSiblingIndex(fill.transform.GetSiblingIndex());
            trail.color=new Color(.035f,.028f,.025f,1);
            var track=enemy.Find("EnemyHealthTrack").GetComponent<Image>();
            track.color=new Color(.32f,.28f,.24f,1);
            foreach(var im in new[]{fill,trail}) {im.type=Image.Type.Filled;im.fillMethod=Image.FillMethod.Horizontal;im.fillOrigin=0;im.fillAmount=1;}
            var gauge=enemy.gameObject.AddComponent<DamageTrailGauge>();
            Ref(gauge,"_currentFill",fill);Ref(gauge,"_trailFill",trail);Ref(root.GetComponent<RunHUD>(),"_enemyGauge",gauge);

            var player=root.transform.Find("PlayerStatus");
            foreach(var name in new[]{"SkillFrame","SkillTrack","SkillFill"})
                if(player.Find(name)!=null)UnityEngine.Object.DestroyImmediate(player.Find(name).gameObject);
            var skill=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/Skill.prefab"),player);
            skill.name="SkillGauge";
            var rect=(RectTransform)skill.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(0,-28);rect.sizeDelta=new Vector2(620,80);rect.localScale=Vector3.one*(350f/620);
            // Restore the original Canvas's skill artwork, proportions and tint.
            var frame=AssetDatabase.LoadAllAssetsAtPath("Assets/Mock/UI/CHatGPT Image 2026年2月1日.png").OfType<Sprite>()
                .First(s=> {long id;string guid;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out guid,out id);return id==-167807101;});
            skill.GetComponent<Image>().sprite=frame;
            var body=(RectTransform)skill.transform.Find("HealthGuage");body.sizeDelta=new Vector2(504,33.33334f);body.anchoredPosition=new Vector2(340.5f,-39.4f);
            body.Find("burn").GetComponent<Image>().color=Color.black;
            var skillFill=body.Find("Health").GetComponent<Image>();skillFill.name="SkillFill";skillFill.color=new Color(.6981132f,.22640648f,.10932711f,1);skillFill.fillAmount=1;
            foreach(var im in skill.GetComponentsInChildren<Image>())im.raycastTarget=false;
            Ref(root.GetComponent<PlayerHUDView>(),"_skillFill",skillFill);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/BattleHUD.prefab");PrefabUtility.UnloadPrefabContents(root);

            root=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/RunSetupCanvas.prefab");
            foreach(var toggle in root.GetComponentsInChildren<Toggle>(true))
                if(toggle.GetComponent<EquipmentOptionSelection>()==null)toggle.gameObject.AddComponent<EquipmentOptionSelection>();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/UI/RunSetupCanvas.prefab");PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();Debug.Log("ENEMY_AUTHOR_RESULT: success");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
