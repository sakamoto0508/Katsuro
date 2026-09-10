using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mock.UI;
public static class HealthTrailRunner
{
    static int count;
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
    static void Call(object o,string m,params object[] args)=>o.GetType().GetMethod(m,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    static void Preview(GameObject root,string file)
    {
        var camera=new GameObject("Preview",typeof(Camera)).GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.105f);
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();
        foreach(var label in root.GetComponentsInChildren<TMP_Text>())label.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();camera.Render();
        var prior=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
        File.WriteAllBytes(Path.GetFullPath("../"+file),texture.EncodeToPNG());RenderTexture.active=prior;
        UnityEngine.Object.DestroyImmediate(texture);camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camera.gameObject);
    }
    public static void Run()
    {
        try {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/BattleHUD.prefab"));
            var view=root.GetComponent<PlayerHUDView>();var green=Get<Image>(view,"_hpFill");var red=Get<Image>(view,"_damageFill");
            Check(green.sprite.name=="Green"&&red.sprite.name=="Red","Original green/red image slices are used");
            view.SetHpNormalized(1);Check(green.fillAmount==1&&red.fillAmount==1,"Initial HP synchronizes both layers");
            view.SetHpNormalized(.6f);float until=Get<float>(view,"_catchupAt");
            Check(Mathf.Approximately(green.fillAmount,.6f)&&red.fillAmount==1,"Damage reduces green first");
            Call(view,"TickDamageTrail",until-.01f,.2f);Check(red.fillAmount==1,"Red waits for configured delay");
            Call(view,"TickDamageTrail",until+.2f,.2f);Check(red.fillAmount<1&&red.fillAmount>.6f,"Red catches up gradually");
            float trail=red.fillAmount;
            view.SetHpNormalized(.4f);float newUntil=Get<float>(view,"_catchupAt");
            Call(view,"TickDamageTrail",newUntil-.01f,.2f);Check(red.fillAmount==trail,"Repeated hits retain red trail and restart delay");
            Call(view,"TickDamageTrail",newUntil+2,2f);Check(Mathf.Approximately(red.fillAmount,.4f),"Red reaches real HP and reveals background");
            view.SetHpNormalized(.8f);Check(Mathf.Approximately(red.fillAmount,.8f)&&Mathf.Approximately(green.fillAmount,.8f),"Healing synchronizes layers");
            view.SetHpNormalized(0);Call(view,"OnDisable");Check(red.fillAmount==0&&green.fillAmount==0,"Disable clears stale red");
            view.SetHpNormalized(1);Check(red.fillAmount==1&&green.fillAmount==1,"Revival restores both layers");
            var hud=root.GetComponent<RunHUD>();var player=new GameObject("Player").AddComponent<PlayerController>();var enemy=new GameObject("Enemy").AddComponent<EnemyController>();
            Set(hud,"_player",player);Set(hud,"_enemy",enemy);
            RunSession.Begin("挑戦者",0,0);hud.Init();Call(hud,"LateUpdate");
            var orbs=Get<Image[]>(hud,"_lifeOrbs");
            Check(orbs.Length==3&&orbs[0].color.r==1&&orbs[1].color.r==1&&orbs[2].color.r<.3f,"Two lives illuminate two of three original orbs");
            RunSession.ConsumeLife();Set(hud,"_nextRefresh",0f);Call(hud,"LateUpdate");
            Check(orbs[0].color.r==1&&orbs[1].color.r<.3f,"Losing a life darkens one orb");
            RunSession.ConsumeLife();Set(hud,"_nextRefresh",0f);Call(hud,"LateUpdate");
            Check(orbs[0].color.r<.3f&&orbs[1].color.r<.3f&&orbs[2].color.r<.3f,"Zero lives leaves all orbs dark");
            RunSession.Begin("挑戦者",0,0);Set(hud,"_nextRefresh",0f);player.Hp=60;Call(hud,"LateUpdate");
            view.SetHpNormalized(1);view.SetHpNormalized(.6f);Call(view,"TickDamageTrail",Get<float>(view,"_catchupAt")+.3f,.3f);
            Preview(root,"legacy-health-preview.png");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(player.gameObject);UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/RunSetupCanvas.prefab"));
            var setup=root.GetComponent<RunSetupUI>();setup.Init();var title=new GameObject("Title").AddComponent<TitleManager>();setup.Open(title);
            Get<Toggle[]>(setup,"_attackOptions")[1].isOn=true;
            Check(((Image)Get<Toggle[]>(setup,"_attackOptions")[1].graphic).sprite.name=="SelectionOutline","Selection uses new stamp-free outline");
            Preview(root,"selection-outline-preview.png");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(title.gameObject);
            Debug.Log("HEALTH_TRAIL_RESULT: "+count+" passed");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
