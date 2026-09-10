using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mock.UI;

public static class InkUIRunner
{
    static int count;
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name); count++;Debug.Log("PASS: "+name); }
    static void Preview(GameObject root,string file)
    {
        var camera=new GameObject("Preview Camera",typeof(Camera)).GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10); camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.075f,.09f,.105f);
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target;
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true)) if(t.gameObject.activeInHierarchy)t.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases(); camera.Render();
        var prior=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(Path.GetFullPath("../"+file),image.EncodeToPNG());
        RenderTexture.active=prior;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
    public static void Run()
    {
        try {
            var contents=PrefabUtility.LoadPrefabContents("Assets/Mock/UI/BattleHUD.prefab");
            ((RectTransform)contents.transform.Find("EnemyStatus")).anchoredPosition=new Vector2(0,-25);
            PrefabUtility.SaveAsPrefabAsset(contents,"Assets/Mock/UI/BattleHUD.prefab");PrefabUtility.UnloadPrefabContents(contents);
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/RunSetupCanvas.prefab"));
            var setup=root.GetComponent<RunSetupUI>();setup.Init();
            var title=new GameObject("Title").AddComponent<TitleManager>();
            setup.Open(title);
            Check(setup.IsOpen&&root.GetComponent<CanvasGroup>().alpha==1,"Ink preparation opens");
            var choices=Get<Toggle[]>(setup,"_attackOptions");choices[1].isOn=true;
            var defense=Get<Toggle[]>(setup,"_defenseOptions");defense[1].isOn=true;
            Check(choices[1].graphic.GetComponent<Image>().sprite!=null,"Selected option has authored vermilion frame");
            Check(Get<TMP_Text>(setup,"_attackDescription").text==RunSession.AttackDescription(1),"Restyled equipment still updates description");
            Canvas.ForceUpdateCanvases();
            foreach(var toggle in choices) Check(toggle.GetComponent<RectTransform>().rect.width==230,"Equipment card has readable width");
            Preview(root,"ink-preparation-preview.png");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(title.gameObject);
            root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/BattleHUD.prefab"));
            var hud=root.GetComponent<RunHUD>();
            var player=new GameObject("Player").AddComponent<PlayerController>();
            var enemy=new GameObject("Enemy").AddComponent<EnemyController>();
            Set(hud,"_player",player);Set(hud,"_enemy",enemy);
            RunSession.Begin("挑戦者",0,0);hud.Init();Call(hud,"LateUpdate");
            Check(Get<TMP_Text>(hud,"_challenger").text=="挑戦者","Player name is bound");
            Check(Get<TMP_Text>(hud,"_opponent").text=="名もなき守人","First boss has correct name");
            var fill=Get<Image>(hud,"_enemyHpFill");
            enemy.HpRatio=.23f;Call(hud,"LateUpdate");Check(Mathf.Approximately(fill.fillAmount,.23f),"Enemy damage immediately updates fill");
            enemy.HpRatio=0;Call(hud,"LateUpdate");Check(fill.fillAmount==0,"Empty enemy HP shows empty bar");
            enemy.HpRatio=.68f;Call(hud,"LateUpdate");
            var playerView=root.GetComponent<PlayerHUDView>();
            playerView.SetHpNormalized(.24f);playerView.SetSkillNormalized(.8f);
            Check(Mathf.Approximately(Get<Image>(playerView,"_hpFill").fillAmount,.24f),"Existing player HUD presenter API updates new HP bar");
            var name=Get<TMP_Text>(hud,"_challenger").rectTransform;
            Check(name.anchoredPosition.y>Get<Image>(playerView,"_hpFill").rectTransform.anchoredPosition.y,"Player name is above own HP");
            Check(((RectTransform)root.transform.Find("EnemyStatus")).anchoredPosition.x==0,"Enemy status is centered at top");
            Preview(root,"ink-battle-hud-preview.png");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(player.gameObject);UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            Debug.Log("INK_VALIDATION_RESULT: "+count+" passed");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
