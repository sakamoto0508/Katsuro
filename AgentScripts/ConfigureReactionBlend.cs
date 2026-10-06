using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureReactionBlend
{
    const string Path="Assets/Mock/AnimationController/Enemy.controller";
    const string Prefab="Assets/Mock/Prefabs/Enemy.prefab";
    const string Key="Katsuro.ReactionBlend.Prefab";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Edit Mode and compiled scripts required.");
        var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(Path);
        var layer=c.layers.Single(l=>l.name=="HitReaction");
        Check(layer.stateMachine.anyStateTransitions.Length==8,"Existing 8 routes expected.");
        foreach(var t in layer.stateMachine.anyStateTransitions)
        {
            Check(t.conditions.Any(x=>x.parameter=="IsJustAvoidCounter"),"Counter condition required.");
            t.duration=.08f;t.hasFixedDuration=true;t.hasExitTime=false;t.offset=0f;
            // Current-state interruption lets another authorized counter blend
            // into a different direction even during the previous recovery.
            t.interruptionSource=TransitionInterruptionSource.Source;
            t.orderedInterruption=false;
            EditorUtility.SetDirty(t);
        }
        foreach(var state in layer.stateMachine.states.Where(s=>s.state.motion!=null))
        foreach(var t in state.state.transitions)
        {
            Check(t.destinationState==layer.stateMachine.defaultState,"Expected return to Empty.");
            t.duration=.08f;t.hasFixedDuration=true;t.hasExitTime=true;t.exitTime=1f;t.offset=0f;
            t.interruptionSource=TransitionInterruptionSource.Source;t.orderedInterruption=false;
            EditorUtility.SetDirty(t);
        }
        EditorUtility.SetDirty(c);
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        var so=new SerializedObject(root.GetComponent<EnemyAnimationController>());
        so.FindProperty("_reactionBlendIn").floatValue=.08f;so.FindProperty("_reactionBlendOut").floatValue=.08f;
        so.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,root.GetInstanceID());
        return "Staged entry and recovery .08 seconds, offset zero, source interruption; Enemy prefab blend settings .08/.08; duration .4 and full weight preserved.";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Compile must finish before Save.");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key,0))as GameObject;Check(root!=null,"Staged prefab missing.");
        foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(Path))if(obj!=null)AssetDatabase.SaveAssetIfDirty(obj);
        PrefabUtility.SaveAsPrefabAsset(root,Prefab,out bool saved);Check(saved,"Prefab save failed.");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt(Key);
        return "Saved existing Enemy.controller and Enemy.prefab.";
    }
}
