using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ConfigureAttackQuality
{
    static IEnumerable<AnimatorState> States(AnimatorStateMachine sm){foreach(var s in sm.states)yield return s.state;foreach(var child in sm.stateMachines)foreach(var s in States(child.stateMachine))yield return s;}
    // Clip seconds, sampled blade trajectory; rates multiply the existing state speed.
    static readonly Dictionary<string,float[]> Profiles=new Dictionary<string,float[]> {
        {"Player/LockOnLightAttack",new[]{.96f,1.13f,1f,.18f,.467f,.55f,.14f}},
        {"Player/LOLightAttack2",new[]{.98f,1.10f,1f,.15f,.35f,.43f,.14f}},
        {"Player/LOLightAttack3",new[]{.97f,1.12f,1f,.20f,.50f,.58f,.15f}},
        {"Player/LOLightAttack4",new[]{1f,1.08f,1f,.08f,.35f,.43f,.14f}},
        {"Player/LOLightAttack5",new[]{.94f,1.15f,.98f,.20f,.433f,.52f,.16f}},
        {"Player/ARPG_Samurai_Attack_Combo1",new[]{.96f,1.12f,1f,.17f,.35f,.43f,.14f}},
        {"Player/ARPG_Samurai_Attack_Combo2",new[]{.96f,1.13f,1f,.29f,.45f,.53f,.15f}},
        {"Player/ARPG_Samurai_Attack_Combo3",new[]{.98f,1.10f,1f,.18f,.30f,.40f,.14f}},
        {"Player/ARPG_Samurai_Attack_Combo4",new[]{.94f,1.12f,.99f,.40f,.53f,.78f,.16f}},
        {"Player/StrongAttack",new[]{.88f,1.15f,.97f,.10f,.23f,.65f,.18f}},
        {"Player/ARPG_Halberd_Attack_Heavy2",new[]{.87f,1.16f,.96f,.24f,.467f,.57f,.20f}},
        {"Enemy/LightAttack1",new[]{.96f,1.10f,1f,.50f,.73f,.82f,.16f}},
        {"Enemy/LightAttack2",new[]{.95f,1.10f,1f,.38f,.60f,.70f,.16f}},
        {"Enemy/AttackHeavy",new[]{.92f,1.14f,.98f,.14f,.45f,.56f,.20f}},
        {"Enemy/AttackHeavy2",new[]{.93f,1.13f,.98f,.17f,.467f,.57f,.20f}}
    };
    static bool Representative(string key)=>key=="Player/LockOnLightAttack"||key=="Player/StrongAttack"||key=="Enemy/LightAttack1"||key=="Enemy/AttackHeavy";
    public static string Representatives()=>Apply(true);
    public static string All()=>Apply(false);
    static string Apply(bool representative)
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Compiled Edit Mode required");
        var b=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"}) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab");var ac=(AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
            foreach(var s in States(ac.layers[0].stateMachine)) {
                string key=who+"/"+s.name;if(!Profiles.ContainsKey(key)||representative&&!Representative(key))continue;
                if(s.speedParameterActive&&!s.speedParameter.StartsWith(NormalAttackSpeedState.ParameterPrefix))throw new Exception("Existing speed multiplier on "+key);
                var c=(AnimationClip)s.motion;var values=Profiles[key];string param=NormalAttackSpeedState.ParameterPrefix+s.name;
                if(!ac.parameters.Any(p=>p.name==param))ac.AddParameter(new AnimatorControllerParameter{name=param,type=AnimatorControllerParameterType.Float,defaultFloat=1f});
                s.speedParameter=param;s.speedParameterActive=true;s.tag=NormalAttackSpeedState.AttackTag;
                var behaviour=s.behaviours.OfType<NormalAttackSpeedState>().SingleOrDefault()??s.AddStateMachineBehaviour<NormalAttackSpeedState>();behaviour.name=s.name+" NormalAttackPhase";
                var so=new SerializedObject(behaviour);so.FindProperty("_speedParameter").stringValue=param;
                so.FindProperty("_windupSpeed").floatValue=values[0];so.FindProperty("_slashSpeed").floatValue=values[1];so.FindProperty("_recoverySpeed").floatValue=values[2];
                so.FindProperty("_slashStart").floatValue=values[3]/c.length;so.FindProperty("_slashEnd").floatValue=values[4]/c.length;so.FindProperty("_recoveryStart").floatValue=values[5]/c.length;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(behaviour);
                foreach(var t in s.transitions)if(t.destinationState!=null&&t.destinationState.name.Contains("Idle")) {
                    if(!t.hasExitTime||t.exitTime<1f||t.offset!=0f)throw new Exception("Unsafe existing idle transition "+key);
                    t.duration=values[6];t.hasFixedDuration=true;EditorUtility.SetDirty(t);
                }
                EditorUtility.SetDirty(s);b.AppendLine(key+" phase="+values[0]+"/"+values[1]+"/"+values[2]+" boundaries(s)="+values[3]+"/"+values[4]+"/"+values[5]+" idle blend="+values[6]+" base speed="+s.speed);
            }
            EditorUtility.SetDirty(ac);AssetDatabase.SaveAssetIfDirty(ac);
            // Prefab is intentionally unchanged; confirm and save only if already dirty.
            AssetDatabase.SaveAssetIfDirty(prefab);
        }
        return b.ToString();
    }
}
