using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;
using TMPro;
public static class ConfigureFinalBlow
{
    const string ScenePath="Assets/Mock/Scenes/GameScene.unity",OverlayPath="Assets/Mock/UI/FinalBlowOverlay.prefab";
    const string CameraPath="Assets/Mock/Prefabs/Manager/CameraManager.prefab",NumbersPath="Assets/Mock/UI/DamageNumbersCanvas.prefab",AudioPath="Assets/Mock/Prefabs/Manager/AudioManager.prefab";
    const string Key="Katsuro.FinalBlow.";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Float(SerializedObject so,string name,float value){so.FindProperty(name).floatValue=value;}
    static void Ref(SerializedObject so,string name,UnityEngine.Object value){so.FindProperty(name).objectReferenceValue=value;}
    static T One<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
    static UnityEngine.UI.Image Image(Transform parent,string name,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
        var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;
        image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
        return image;
    }
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compiled Edit Mode required.");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPath)==null,"Overlay exists; inspect before creating.");
        Check(AssetDatabase.FindAssets("FinalBlowOverlay t:Prefab").Length==0,"Same named overlay exists.");
        var root=new GameObject("FinalBlowOverlay",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(FinalBlowPresentation));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var white=Image(root.transform,"WhiteFlash",Color.white);var flash=white.gameObject.AddComponent<CanvasGroup>();flash.alpha=0;flash.interactable=flash.blocksRaycasts=false;
        var top=Image(root.transform,"LetterboxTop",Color.black).rectTransform;top.anchorMin=new Vector2(0,1);top.anchorMax=new Vector2(1,1.08f);top.offsetMin=top.offsetMax=Vector2.zero;
        var bottom=Image(root.transform,"LetterboxBottom",Color.black).rectTransform;bottom.anchorMin=new Vector2(0,-.08f);bottom.anchorMax=new Vector2(1,0);bottom.offsetMin=bottom.offsetMax=Vector2.zero;
        var presentation=new SerializedObject(root.GetComponent<FinalBlowPresentation>());Ref(presentation,"_whiteFlash",flash);Ref(presentation,"_letterboxTop",top);Ref(presentation,"_letterboxBottom",bottom);presentation.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root,OverlayPath,out bool created);Check(created,"Overlay creation failed.");UnityEngine.Object.DestroyImmediate(root);
        var cameraRoot=PrefabUtility.LoadPrefabContents(CameraPath);var camSO=new SerializedObject(cameraRoot.GetComponent<CameraManager>());Float(camSO,"_finalBlowFOVOffset",-4);Float(camSO,"_finalBlowPushDuration",.42f);camSO.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key+"Camera",cameraRoot.GetInstanceID());
        var numbersRoot=PrefabUtility.LoadPrefabContents(NumbersPath);var group=numbersRoot.GetComponent<CanvasGroup>();if(group==null)group=numbersRoot.AddComponent<CanvasGroup>();group.alpha=1;group.interactable=group.blocksRaycasts=false;SessionState.SetInt(Key+"Numbers",numbersRoot.GetInstanceID());
        var audioRoot=PrefabUtility.LoadPrefabContents(AudioPath);var audioSO=new SerializedObject(audioRoot.GetComponent<AudioManager>());var list=audioSO.FindProperty("seList");
        // 既存Audio Assetを再利用。最終撃のImpactと、死亡Clip Eventの倒れる音を別時刻に鳴らす。
        foreach(var pair in new[]{new[]{"EnemyDead","Assets/InportAssets/Sounds/SE/Damage/547036__cogfirestudios__hit-impact-sword-2.wav"},new[]{"Death","Assets/InportAssets/Sounds/SE/sen_ge_taoreru08.mp3"}})
        {
            bool exists=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue==pair[0])exists=true;
            if(exists)continue;
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(pair[1]);Check(clip!=null,"Existing audio missing: "+pair[1]);
            int index=list.arraySize;list.InsertArrayElementAtIndex(index);var entry=list.GetArrayElementAtIndex(index);entry.FindPropertyRelative("name").stringValue=pair[0];entry.FindPropertyRelative("clip").objectReferenceValue=clip;
        }
        audioSO.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key+"Audio",audioRoot.GetInstanceID());
        // SceneのPrefab Instanceに設定が継承されるよう、共通Prefabを保存する。
        foreach(var item in new[]{new[]{"Camera",CameraPath},new[]{"Numbers",NumbersPath},new[]{"Audio",AudioPath}})
        {
            var staged=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key+item[0],0))as GameObject;
            PrefabUtility.SaveAsPrefabAsset(staged,item[1],out bool saved);Check(saved,"Prefab save failed.");PrefabUtility.UnloadPrefabContents(staged);SessionState.EraseInt(Key+item[0]);
        }
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;SessionState.SetBool(Key+"Opened",opened);
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var manager=One<FinalBlowManager>(scene);var managerSO=new SerializedObject(manager);
        var existingText=managerSO.FindProperty("_finalBlowText").objectReferenceValue as TextMeshProUGUI;Check(existingText!=null,"Preserve existing Final Blow Text reference.");
        SessionState.SetString(Key+"Text",existingText.text);
        var overlay=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPath),scene);
        var p=overlay.GetComponent<FinalBlowPresentation>();var pSO=new SerializedObject(p);
        var battle=scene.GetRootGameObjects().Single(r=>r.name=="BattleHUD").GetComponent<CanvasGroup>();Check(battle!=null,"BattleHUD CanvasGroup missing.");
        var damage=One<DamageNumbers>(scene);var damageGroup=damage.GetComponent<CanvasGroup>();Check(damageGroup!=null,"Numbers group inheritance missing.");
        var groups=pSO.FindProperty("_hudGroups");groups.arraySize=2;groups.GetArrayElementAtIndex(0).objectReferenceValue=battle;groups.GetArrayElementAtIndex(1).objectReferenceValue=damageGroup;
        var owners=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunHUD>(true)).ToArray();var ownerRefs=pSO.FindProperty("_hudVisibilityOwners");ownerRefs.arraySize=owners.Length;for(int i=0;i<owners.Length;i++)ownerRefs.GetArrayElementAtIndex(i).objectReferenceValue=owners[i];Ref(pSO,"_damageNumbers",damage);pSO.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(p);
        SetManager(scene,managerSO,p);
        EditorSceneManager.MarkSceneDirty(scene);
        return "Created FinalBlowOverlay prefab and scene instance (Flash0, bars outside screen, sorting200). Staged GameScene manager: HitStop .20, Flash .02-.12, HUD .12-.35, bars .15-.40 at8%, camera .18-.60 FOV-4, sheath .80, text1.05-1.40, Scene Fade3.10. Existing text preserved: "+existingText.text+". Shared prefab settings saved: Camera, DamageNumbers group, existing audio EnemyDead/Death registration.";
    }
    static void SetManager(Scene scene,SerializedObject so,FinalBlowPresentation p)
    {
        var gameSO=new SerializedObject(One<GameManager>(scene));var camera=gameSO.FindProperty("_cameraManager").objectReferenceValue as CameraManager;Check(camera!=null,"GameManager camera reference missing.");
        Ref(so,"_presentation",p);Ref(so,"_cameraFeedback",camera);Float(so,"_phase1HitStop",.2f);Float(so,"_whiteFlashDuration",.1f);Float(so,"_phase2Duration",2.3f);Float(so,"_finalBlowTextFadeIn",.35f);Float(so,"_cameraPushDelay",.18f);Float(so,"_sheathingDelay",.8f);Float(so,"_textDelay",1.05f);so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static string FinishStage()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);Check(scene.isLoaded,"Staged scene must remain loaded.");
        SetManager(scene,new SerializedObject(One<FinalBlowManager>(scene)),One<FinalBlowPresentation>(scene));EditorSceneManager.MarkSceneDirty(scene);
        return "Staging complete: CameraManager selected from actual GameManager reference, preserving other camera components. Final Blow timings and overlay assigned.";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compile before Save.");
        var scene=SceneManager.GetSceneByPath(ScenePath);Check(scene.isLoaded,"Staged scene missing.");Check(EditorSceneManager.SaveScene(scene),"Scene save failed.");
        if(SessionState.GetBool(Key+"Opened",false))EditorSceneManager.CloseScene(scene,true);
        return "Saved GameScene and FinalBlowOverlay / CameraManager / DamageNumbersCanvas / AudioManager prefabs. Original active scene preserved.";
    }
    public static string Verify()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var b=new StringBuilder();var manager=One<FinalBlowManager>(scene);var so=new SerializedObject(manager);var p=so.FindProperty("_presentation").objectReferenceValue as FinalBlowPresentation;Check(p!=null,"Presentation reference not saved.");
            Check(Mathf.Approximately(so.FindProperty("_phase1HitStop").floatValue,.2f)&&Mathf.Approximately(so.FindProperty("_whiteFlashDuration").floatValue,.1f),"Final Blow timings not saved.");
            var text=so.FindProperty("_finalBlowText").objectReferenceValue as TextMeshProUGUI;Check(text!=null&&text.text==SessionState.GetString(Key+"Text",""),"Existing text changed.");
            var ps=new SerializedObject(p);Check(ps.FindProperty("_hudGroups").arraySize==2,"HUD groups missing.");Check(ps.FindProperty("_damageNumbers").objectReferenceValue!=null,"Damage Number suppress hookup missing.");
            var white=ps.FindProperty("_whiteFlash").objectReferenceValue as CanvasGroup;Check(white!=null&&white.alpha==0,"White Flash must start transparent.");
            var top=ps.FindProperty("_letterboxTop").objectReferenceValue as RectTransform;var bottom=ps.FindProperty("_letterboxBottom").objectReferenceValue as RectTransform;Check(top.anchorMin.y>=1&&bottom.anchorMax.y<=0,"Bars must begin offscreen.");
            Check(p.GetComponent<Canvas>().sortingOrder==200,"Overlay order incorrect.");
            var cam=so.FindProperty("_cameraFeedback").objectReferenceValue as CameraManager;Check(cam!=null,"Camera hookup missing.");
            var camSO=new SerializedObject(cam);Check(camSO.FindProperty("_finalBlowFOVOffset").floatValue==-4,"Camera override missing.");
            foreach(var pair in new[]{new[]{"Enemy","EnemyDead"},new[]{"Player","SwordSheathing"}})
            {
                var actor=scene.GetRootGameObjects().Single(r=>r.name==pair[0]);var controller=(AnimatorController)actor.GetComponent<Animator>().runtimeAnimatorController;
                var transition=controller.layers[0].stateMachine.anyStateTransitions.Single(t=>t.conditions.Any(c=>c.parameter==pair[1]));
                b.AppendLine(pair[0]+" "+pair[1]+" hasExit="+transition.hasExitTime+" blend="+transition.duration+" state="+transition.destinationState.name+" clip="+AssetDatabase.GetAssetPath(transition.destinationState.motion));
            }
            var audio=new SerializedObject(One<AudioManager>(scene));var sounds=audio.FindProperty("seList");foreach(var name in new[]{"EnemyDead","Death","Sheathing"}){bool valid=false;for(int i=0;i<sounds.arraySize;i++){var e=sounds.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("name").stringValue==name&&e.FindPropertyRelative("clip").objectReferenceValue!=null)valid=true;}Check(valid,"Scene audio mapping missing: "+name);}
            b.AppendLine("RE-READ PASS: manager refs="+EditorJsonUtility.ToJson(manager)+" presentation="+EditorJsonUtility.ToJson(p)+"; HUD BattleHUD + DamageNumbersCanvas; text="+text.text+"; Flash transparent, letterbox hidden, Canvas order200 below GlobalFader32767; FOV -4/.42s; EnemyDead/Death/Sheathing mapped. Play Mode not run.");return b.ToString();
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
