using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureCounterReaction
{
    const string EnemyPath = "Assets/Mock/AnimationController/Enemy.controller";
    const string PlayerPath = "Assets/Mock/AnimationController/Player.controller";
    const string PrefabPath = "Assets/Mock/Prefabs/Enemy.prefab";
    const string Key = "Katsuro.CounterReaction.Prefab";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Edit Mode, compilation complete required.");
        var enemy=AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyPath);
        var player=AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerPath);
        var reaction=enemy.layers.Single(l=>l.name=="HitReaction").stateMachine;
        Check(reaction.states.Length==9 && reaction.anyStateTransitions.Length==8,"Expected existing reactions.");
        var attacks=player.layers[0].stateMachine.states.Where(s=>s.state.name=="ARPG_Samurai_Attack_Heavy1" || s.state.name=="ARPG_Samurai_Attack_Heavy2").Select(s=>s.state).ToArray();
        Check(attacks.Length==2,"Expected existing Just Avoid attack states.");
        foreach(var state in attacks)
        {
            Check(AssetDatabase.GetAssetPath(state.motion).StartsWith("Assets/Mock/Animation/Player/JustVoidAttack/"),"Counter must use inspected special branch.");
            Check(string.IsNullOrEmpty(state.tag) || state.tag==PlayerAttacker.JustAvoidCounterTag,"Existing tag must be preserved; inspect before retry.");
        }
        if(!enemy.parameters.Any(p=>p.name=="IsJustAvoidCounter"))enemy.AddParameter("IsJustAvoidCounter",AnimatorControllerParameterType.Bool);
        Check(enemy.parameters.Single(p=>p.name=="IsJustAvoidCounter").type==AnimatorControllerParameterType.Bool,"Counter bool parameter required.");
        foreach(var t in reaction.anyStateTransitions)
        {
            if(!t.conditions.Any(c=>c.parameter=="IsJustAvoidCounter"))t.AddCondition(AnimatorConditionMode.If,0,"IsJustAvoidCounter");
            EditorUtility.SetDirty(t);
        }
        foreach(var state in attacks){state.tag=PlayerAttacker.JustAvoidCounterTag;EditorUtility.SetDirty(state);}
        EditorUtility.SetDirty(enemy);EditorUtility.SetDirty(player);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        var feedback=new SerializedObject(root.GetComponent<CombatFeedback>());
        Check(feedback.FindProperty("_justAvoidCounterHitStop")!=null,"New field must compile before staging.");
        feedback.FindProperty("_justAvoidCounterHitStop").floatValue=.1f;
        feedback.ApplyModifiedPropertiesWithoutUndo();
        SessionState.SetInt(Key,root.GetInstanceID());
        return "Staged: 2 existing Player counter states tagged; Enemy 8 reaction routes require IsJustAvoidCounter; counter HitStop .1 seconds. No states/clips deleted or replaced.";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Compile before Save.");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key,0))as GameObject;Check(root!=null,"Staged prefab missing.");
        foreach(var path in new[]{EnemyPath,PlayerPath})foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path))if(obj!=null)AssetDatabase.SaveAssetIfDirty(obj);
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool saved);Check(saved,"Save failed.");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt(Key);
        return "Saved existing Player.controller, Enemy.controller, Enemy.prefab.";
    }
}
