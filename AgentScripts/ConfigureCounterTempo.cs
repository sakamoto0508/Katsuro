using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureCounterTempo
{
    const string ControllerPath="Assets/Mock/AnimationController/Player.controller";
    const string PlayerPath="Assets/Mock/Prefabs/Player.prefab";
    const string EnemyPath="Assets/Mock/Prefabs/Enemy.prefab";
    const string ConfigPath="Assets/Mock/ScriptableObjects/VFXConfig.asset";
    const string Key="Katsuro.CounterTempo.";
    const string Slash="AnimEvent_JustAvoidCounterSlash",Reset="AnimEvent_JustAvoidCounterResetSpeed";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static AnimatorState[] States(AnimatorStateMachine sm)=>sm.states.Select(c=>c.state).Concat(sm.stateMachines.SelectMany(c=>States(c.stateMachine))).ToArray();
    static string EventSignature(AnimationClip clip)=>string.Join("|",AnimationUtility.GetAnimationEvents(clip).Where(e=>e.functionName!=Slash&&e.functionName!=Reset).Select(e=>e.time+":"+e.functionName+":"+e.stringParameter+":"+e.floatParameter+":"+e.intParameter+":"+e.messageOptions));
    static string NormalSignature(AnimatorController controller)=>string.Join("|",controller.layers.SelectMany(l=>States(l.stateMachine)).Where(s=>s.tag!=PlayerAttacker.JustAvoidCounterTag).Select(s=>s.name+":"+s.speed+":"+s.speedParameter+":"+s.speedParameterActive+":"+AssetDatabase.GetAssetPath(s.motion)));
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compiled Edit Mode required.");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var counter=States(controller.layers[0].stateMachine).Where(s=>s.tag==PlayerAttacker.JustAvoidCounterTag).ToArray();Check(counter.Length==2,"Expected two tagged states.");
        SessionState.SetString(Key+"Normal",NormalSignature(controller));
        if(!controller.parameters.Any(p=>p.name==JustAvoidCounterAnimation.SpeedParameter))controller.AddParameter(JustAvoidCounterAnimation.SpeedParameter,AnimatorControllerParameterType.Float);
        var parameters=controller.parameters;var parameter=parameters.Single(p=>p.name==JustAvoidCounterAnimation.SpeedParameter);
        Check(parameter.type==AnimatorControllerParameterType.Float,"Expected Float.");parameter.defaultFloat=1;controller.parameters=parameters;
        foreach(var state in counter)
        {
            var clip=state.motion as AnimationClip;Check(clip!=null&&AssetDatabase.GetAssetPath(clip).StartsWith("Assets/Mock/Animation/Player/JustVoidAttack/"),"Dedicated existing clip required.");
            Check(controller.layers.SelectMany(l=>States(l.stateMachine)).Where(s=>s.motion==clip).All(s=>s.tag==PlayerAttacker.JustAvoidCounterTag),"Do not alter a shared ordinary attack clip.");
            SessionState.SetString(Key+state.name,EventSignature(clip));
            int frame=state.name.EndsWith("Heavy1")?8:17;
            float off=clip.events.Single(e=>e.functionName=="AnimEvent_DisableWeaponHitbox").time;
            var events=AnimationUtility.GetAnimationEvents(clip).Where(e=>e.functionName!=Slash&&e.functionName!=Reset).Concat(new[]{
                new AnimationEvent{time=frame/clip.frameRate,functionName=Slash},
                new AnimationEvent{time=off,functionName=Reset}}).OrderBy(e=>e.time).ToArray();
            AnimationUtility.SetAnimationEvents(clip,events);EditorUtility.SetDirty(clip);
            state.speed=1;state.speedParameter=JustAvoidCounterAnimation.SpeedParameter;state.speedParameterActive=true;
            if(!state.behaviours.OfType<JustAvoidCounterSpeedState>().Any())state.AddStateMachineBehaviour<JustAvoidCounterSpeedState>();
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);
        var player=PrefabUtility.LoadPrefabContents(PlayerPath);
        var speed=player.GetComponent<JustAvoidCounterAnimation>();if(speed==null)speed=player.AddComponent<JustAvoidCounterAnimation>();
        var speedSO=new SerializedObject(speed);speedSO.FindProperty("_counterWindupSpeed").floatValue=.78f;speedSO.FindProperty("_counterSlashSpeed").floatValue=1.1f;speedSO.ApplyModifiedPropertiesWithoutUndo();
        foreach(var root in new[]{player,PrefabUtility.LoadPrefabContents(EnemyPath)})
        {
            var so=new SerializedObject(root.GetComponent<CombatFeedback>());so.FindProperty("_justAvoidCounterHitStop").floatValue=.14f;so.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key+root.name,root.GetInstanceID());
        }
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath);var configSO=new SerializedObject(config);
        configSO.FindProperty("_counterHitVFXSpeed").floatValue=1.3f;configSO.FindProperty("_counterHitVFXSize").floatValue=1.05f;configSO.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
        return "Staged 2 counter states, base speed .9 -> 1 with counter-only Float multiplier(default1). Windup .78 on entry, slash Heavy1 f8 / Heavy2 f17, reset on existing hitbox OFF f22 / f36. Original hitbox/combo/SE/finish events preserved. Counter HitStop .14 in Enemy and Player prefab, Blood speed1.3 size1.05; normal profile untouched.";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compile before Save.");
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        foreach(var clip in States(controller.layers[0].stateMachine).Where(s=>s.tag==PlayerAttacker.JustAvoidCounterTag).Select(s=>s.motion))AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath));
        foreach(var path in new[]{PlayerPath,EnemyPath})
        {
            var name=System.IO.Path.GetFileNameWithoutExtension(path);
            var root=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key+name,0)) as GameObject;Check(root!=null,"Staged prefab missing: "+name);
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);Check(saved,"Save failed: "+path);
            PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt(Key+name);
        }
        return "Saved Player.controller, both dedicated counter .anim files, Player.prefab, Enemy.prefab, VFXConfig.asset.";
    }
    public static string Verify()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode required.");
        var b=new StringBuilder();var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Check(NormalSignature(controller)==SessionState.GetString(Key+"Normal",""),"Normal states changed.");
        var p=controller.parameters.Single(x=>x.name==JustAvoidCounterAnimation.SpeedParameter);Check(p.type==AnimatorControllerParameterType.Float&&p.defaultFloat==1,"Parameter default incorrect.");
        var counter=States(controller.layers[0].stateMachine).Where(s=>s.tag==PlayerAttacker.JustAvoidCounterTag).ToArray();Check(counter.Length==2,"Counter count changed.");
        foreach(var state in counter)
        {
            Check(state.speed==1&&state.speedParameterActive&&state.speedParameter==JustAvoidCounterAnimation.SpeedParameter&&state.behaviours.OfType<JustAvoidCounterSpeedState>().Count()==1,"State multiplier/behaviour not saved.");
            var clip=state.motion as AnimationClip;Check(EventSignature(clip)==SessionState.GetString(Key+state.name,""),"Original events changed.");
            Check(clip.events.Count(e=>e.functionName==Slash)==1&&clip.events.Count(e=>e.functionName==Reset)==1,"Phase events missing/duplicated.");
            b.AppendLine(state.name+" -> "+AssetDatabase.GetAssetPath(clip)+" events="+string.Join(",",clip.events.Select(e=>e.functionName+"@"+e.time+" f"+Mathf.RoundToInt(e.time*clip.frameRate))));
        }
        foreach(var path in new[]{PlayerPath,EnemyPath})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var feedback=new SerializedObject(root.GetComponent<CombatFeedback>());
            Check(Mathf.Approximately(feedback.FindProperty("_justAvoidCounterHitStop").floatValue,.14f),"Counter hitstop not saved.");
            if(path==PlayerPath){var speed=root.GetComponent<JustAvoidCounterAnimation>();Check(speed!=null,"Speed component missing.");b.AppendLine("Speed component="+EditorJsonUtility.ToJson(speed));}
            b.AppendLine(path+" counter HitStop=.14");
        }
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath);
        Check(Mathf.Approximately(config.CounterHitVFXSpeed,1.3f)&&Mathf.Approximately(config.CounterHitVFXSize,1.05f),"Blood profile not saved.");
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath).GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        Check(enemy.layers.Single(l=>l.name=="HitReaction").stateMachine.anyStateTransitions.All(t=>t.conditions.Any(c=>c.parameter=="IsJustAvoidCounter"&&c.mode==AnimatorConditionMode.If)),"Normal full-body rejection changed.");
        b.AppendLine("PASS: original events and all normal state speeds/motions unchanged; saved counter parameter / StateMachineBehaviour / prefab values correct; Enemy full-body counter guard retained. Blood amount="+config.HeavyHitVFXAmount+" scale="+config.HeavyHitVFXScale+" speed="+config.CounterHitVFXSpeed+" size="+config.CounterHitVFXSize+". Play Mode not run.");
        return b.ToString();
    }
    public static string GameScene()
    {
        Check(!EditorApplication.isPlaying,"Edit Mode only.");
        const string path="Assets/Mock/Scenes/GameScene.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var b=new StringBuilder();var roots=scene.GetRootGameObjects();
            var player=roots.SelectMany(r=>r.GetComponentsInChildren<PlayerController>(true)).Single();
            Check(player.GetComponent<JustAvoidCounterAnimation>()!=null,"Scene player missing inherited component.");
            Check(AssetDatabase.GetAssetPath(player.GetComponent<Animator>().runtimeAnimatorController)==ControllerPath,"Scene Animator controller differs.");
            foreach(var feedback in roots.SelectMany(r=>r.GetComponentsInChildren<CombatFeedback>(true)))
            {
                var so=new SerializedObject(feedback);float stop=so.FindProperty("_justAvoidCounterHitStop").floatValue;
                Check(Mathf.Approximately(stop,.14f),"Scene feedback override: "+feedback.name+" ="+stop);
                b.AppendLine(feedback.name+" Counter HitStop="+stop);
            }
            foreach(var h in roots.SelectMany(r=>r.GetComponentsInChildren<HitStopManager>(true)))
            {
                Check(h.LightHitStop<h.HeavyHitStop&&h.HeavyHitStop<.14f&&.14f<h.LastHitStopTime,"HitStop hierarchy differs.");
                b.AppendLine("HitStop hierarchy: "+h.LightHitStop+" < "+h.HeavyHitStop+" < .14 < "+h.LastHitStopTime);
            }
            b.AppendLine("Scene Player inherits CounterAnimation and saved Player.controller; active scene unchanged, GameScene read-only. Play Mode not run.");return b.ToString();
        }
        finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }
}
