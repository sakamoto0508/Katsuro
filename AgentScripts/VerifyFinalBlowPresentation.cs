using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class VerifyFinalBlowPresentation
{
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Flags).SetValue(obj,value);
    static T Get<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Flags).GetValue(obj);
    static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Flags).Invoke(obj,args);
    public static string Main()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode only.");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var overlay=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/FinalBlowOverlay.prefab"),preview);
            var p=overlay.GetComponent<FinalBlowPresentation>();var ps=new SerializedObject(p);
            var hud=new GameObject("Temporary HUD",typeof(CanvasGroup),typeof(RunHUD),typeof(DamageNumbers));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hud,preview);
            var group=hud.GetComponent<CanvasGroup>();group.alpha=.75f;group.interactable=group.blocksRaycasts=true;
            var owner=hud.GetComponent<RunHUD>();Set(owner,"_visibility",group);
            Set(p,"_hudGroups",new[]{group});Set(p,"_hudVisibilityOwners",new[]{owner});Set(p,"_damageNumbers",hud.GetComponent<DamageNumbers>());
            var textObject=new GameObject("Temporary Text",typeof(RectTransform),typeof(TextMeshProUGUI));textObject.transform.SetParent(overlay.transform,false);
            var text=textObject.GetComponent<TextMeshProUGUI>();text.color=new Color(1,1,1,.9f);textObject.SetActive(false);
            var white=(CanvasGroup)ps.FindProperty("_whiteFlash").objectReferenceValue;
            var top=(RectTransform)ps.FindProperty("_letterboxTop").objectReferenceValue;
            var bottom=(RectTransform)ps.FindProperty("_letterboxBottom").objectReferenceValue;
            float savedTimeScale=Time.timeScale;
            try
            {
                Time.timeScale=0;
                p.Begin(text,.1f,1.05f,.35f);
                p.RenderAt(.02f);Check(Mathf.Approximately(white.alpha,.8f),"Flash peak missing.");
                p.RenderAt(.07f);Check(white.alpha>0&&white.alpha<.8f,"Flash must decay.");
                p.RenderAt(.12f);Check(white.alpha<.0001f,"Flash must end.");
                p.RenderAt(.23f);float faded=group.alpha;Check(faded>0&&faded<.75f,"HUD fade must be smooth.");Call(owner,"LateUpdate");Check(Mathf.Approximately(group.alpha,faded),"RunHUD must not overwrite presentation fade.");
                p.RenderAt(.4f);Check(group.alpha==0&&Mathf.Abs(top.anchorMin.y-.92f)<.0001f&&Mathf.Abs(bottom.anchorMax.y-.08f)<.0001f,"HUD / bar targets incorrect.");Check(!textObject.activeSelf,"Text must be delayed.");
                p.RenderAt(1.225f);Check(textObject.activeSelf&&text.alpha>0&&text.alpha<.9f,"Text fade missing.");p.RenderAt(1.4f);Check(Mathf.Approximately(text.alpha,.9f),"Text fade target incorrect.");
                Check(Get<bool>(hud.GetComponent<DamageNumbers>(),"_presentationSuppressed"),"Damage suppression not set.");
                p.ResetPresentation();Check(white.alpha==0&&top.anchorMin.y>=1&&bottom.anchorMax.y<=0,"Overlays not cleared.");
                Check(Mathf.Approximately(group.alpha,.75f)&&group.interactable&&group.blocksRaycasts&&!textObject.activeSelf&&Mathf.Approximately(text.alpha,.9f),"UI state restoration failed.");
                Check(!Get<bool>(owner,"_presentationControlsVisibility")&&!Get<bool>(hud.GetComponent<DamageNumbers>(),"_presentationSuppressed"),"Visibility ownership not released.");
            }
            finally{Time.timeScale=savedTimeScale;p.ResetPresentation();}
            var cameraObject=new GameObject("Temporary Camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=60;
            var managerObject=new GameObject("Temporary CameraManager",typeof(CameraManager));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject,preview);
            var manager=managerObject.GetComponent<CameraManager>();Set(manager,"_outputCamera",camera);Set(manager,"_initialized",true);Set(manager,"_config",AssetDatabase.LoadAssetAtPath<CameraConfig>("Assets/Mock/ScriptableObjects/CameraConfig.asset"));
            manager.PlayFinalBlowFeedback();Set(manager,"_finalBlowStarted",Time.unscaledTime-.5f);manager.PlayJustAvoidFeedback();
            Check(float.IsNegativeInfinity(Get<float>(manager,"_fovStarted")),"Just Avoid must not override final camera.");
            Call(manager,"BeginRendering",default(ScriptableRenderContext),camera);Check(Mathf.Approximately(camera.fieldOfView,56),"FOV push incorrect.");Call(manager,"EndRendering",default(ScriptableRenderContext),camera);Check(Mathf.Approximately(camera.fieldOfView,60),"Render-end camera restoration failed.");
            Call(manager,"BeginRendering",default(ScriptableRenderContext),camera);manager.StopFinalBlowFeedback();Check(Mathf.Approximately(camera.fieldOfView,60),"Cancel restoration failed.");
            return "PASS (Edit Mode direct presentation checks): timescale0 does not change supplied unscaled UI timeline; Flash peak/decay/end, HUD smooth fade and RunHUD ownership,8% bar targets, delayed Text, damage suppression and Cleanup restoration checked. Camera -4 FOV, JustAvoid priority, render-end and Cancel restoration checked. No gameplay/Play Mode/visual appearance test.";
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}
