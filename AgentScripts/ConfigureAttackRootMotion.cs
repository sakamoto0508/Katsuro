using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureAttackRootMotion
{
    static IEnumerable<AnimatorState> States(AnimatorStateMachine sm)
    {foreach(var s in sm.states)yield return s.state;foreach(var child in sm.stateMachines)foreach(var s in States(child.stateMachine))yield return s;}
    static bool IsAttack(string who, AnimatorState s)
    {
        string p=AssetDatabase.GetAssetPath(s.motion);
        return who=="Enemy" ? p.StartsWith("Assets/Mock/Animation/Enemy/")&&p.Contains("Attack/") :
            p.StartsWith("Assets/Mock/Animation/Player/")&&(p.Contains("/LightAttack/")||p.Contains("/UnLockLightAttack/")||p.Contains("/StrongAttack/")||p.Contains("/JustVoidAttack/")&&!p.Contains("_Start.anim"));
    }
    static string ClipSignature(AnimationClip c) => JsonUtility.ToJson(AnimationUtility.GetAnimationClipSettings(c))+"|"+string.Join(";",AnimationUtility.GetAnimationEvents(c).Select(e=>e.time.ToString("R")+":"+e.functionName+":"+e.stringParameter+":"+e.floatParameter+":"+e.intParameter));
    public static string Save()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Compiled Edit Mode required");
        var output=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"})
        {
            string prefabPath="Assets/Mock/Prefabs/"+who+".prefab";
            var root=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var a=root.GetComponent<Animator>(); var ac=a.runtimeAnimatorController as AnimatorController;
                var signatures=ac.animationClips.Distinct().ToDictionary(c=>c,ClipSignature);
                foreach(var layer in ac.layers)
                    foreach(var state in States(layer.stateMachine))
                    {
                        bool attack=layer.name==ac.layers[0].name&&IsAttack(who,state);
                        if(attack)
                        {
                            var gate=state.behaviours.OfType<AttackRootMotionState>().FirstOrDefault()??state.AddStateMachineBehaviour<AttackRootMotionState>();
                            gate.name=state.name+" AttackRootMotion";EditorUtility.SetDirty(gate);
                        }
                        if(!attack&&state.behaviours.OfType<AttackRootMotionState>().Any())throw new Exception("Unexpected root motion behaviour on nonattack "+state.name);
                    }
                var controller=who=="Player"?(UnityEngine.Object)root.GetComponent<PlayerController>():root.GetComponent<EnemyController>();
                var so=new SerializedObject(controller);so.FindProperty("_attackRootMotionScale").floatValue=who=="Player"?.25f:.18f;so.ApplyModifiedPropertiesWithoutUndo();
                // OnAnimatorMove handles extraction; automatic application stays off.
                a.applyRootMotion=false;
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool saved);if(!saved)throw new Exception("Save failed");
                EditorUtility.SetDirty(ac);AssetDatabase.SaveAssetIfDirty(ac);
                foreach(var pair in signatures)if(ClipSignature(pair.Key)!=pair.Value)throw new Exception("Clip settings/events modified");
                output.AppendLine("Saved "+who+" prefab/controller; Clip settings and Events unchanged.");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        return output+Verify();
    }
    public static string Verify()
    {
        var output=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab");var a=root.GetComponent<Animator>();var ac=a.runtimeAnimatorController as AnimatorController;
            var controller=who=="Player"?(UnityEngine.Object)root.GetComponent<PlayerController>():root.GetComponent<EnemyController>();
            output.AppendLine(who+" scale="+new SerializedObject(controller).FindProperty("_attackRootMotionScale").floatValue+" automatic applyRootMotion="+a.applyRootMotion+" RB kinematic="+root.GetComponent<Rigidbody>().isKinematic+" collisions="+root.GetComponent<Rigidbody>().detectCollisions);
            int count=0;
            foreach(var layer in ac.layers)
                foreach(var state in States(layer.stateMachine))
                {
                    bool expected=layer.name==ac.layers[0].name&&IsAttack(who,state);int actual=state.behaviours.OfType<AttackRootMotionState>().Count();
                    if(actual!=(expected?1:0))throw new Exception("Gate mismatch: "+state.name);
                    if(expected){count++;output.AppendLine(" Attack: "+state.name+" clip="+AssetDatabase.GetAssetPath(state.motion));}
                }
            output.AppendLine("Verified "+count+" attack states; locomotion/reaction/death/StepBack have no attack gate.");
            foreach(var c in ac.animationClips.Distinct().Where(c=>AssetDatabase.GetAssetPath(c).Contains("Attack/")))
            {
                var s=AnimationUtility.GetAnimationClipSettings(c);
                if(s.loopBlendPositionXZ||!s.loopBlendPositionY||!s.loopBlendOrientation)throw new Exception("Unexpected attack bake settings: "+c.name);
            }
        }
        System.IO.File.WriteAllText("AgentScripts/AttackRootMotionVerification.txt",output.ToString());return output.ToString();
    }
}
