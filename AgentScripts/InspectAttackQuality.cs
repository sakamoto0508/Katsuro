using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectAttackQuality
{
    public static IEnumerable<AnimatorState> States(AnimatorStateMachine sm) { foreach(var s in sm.states) yield return s.state; foreach(var sub in sm.stateMachines) foreach(var s in States(sub.stateMachine)) yield return s; }
    public static bool Normal(string who, AnimatorState s) { string p=AssetDatabase.GetAssetPath(s.motion); return who=="Enemy" ? p.Contains("/Enemy/")&&p.Contains("Attack/") : p.Contains("/Player/")&&(p.Contains("/LightAttack/")||p.Contains("/UnLockLightAttack/")||p.Contains("/StrongAttack/")); }
    public static string Inspect() => Capture("Before");
    public static string After() => Capture("After");
    static string Capture(string suffix)
    {
        var b=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"}) {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab"); var ac=(AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            b.AppendLine(who+" PREFAB rootMotion="+root.GetComponent<Animator>().applyRootMotion+" components="+string.Join(",",root.GetComponents<Component>().Select(x=>x.GetType().Name)));
            foreach(var p in ac.parameters)b.AppendLine("PARAM "+p.name+" "+p.type+" default="+p.defaultFloat);
            foreach(var layer in ac.layers) {
                b.AppendLine("LAYER "+layer.name+" weight="+layer.defaultWeight+" mode="+layer.blendingMode);
                foreach(var s in States(layer.stateMachine)) {
                    if(!Normal(who,s)) continue;
                    var c=s.motion as AnimationClip;
                    b.AppendLine("STATE "+s.name+" speed="+s.speed+" parameter="+s.speedParameter+" active="+s.speedParameterActive+" clip="+AssetDatabase.GetAssetPath(c)+" length="+c.length+" behaviours="+string.Join(",",s.behaviours.Select(x=>x.GetType().Name)));
                    foreach(var e in AnimationUtility.GetAnimationEvents(c))b.AppendLine(" EVENT "+e.time.ToString("F5")+" "+e.functionName+" args="+e.intParameter+"/"+e.floatParameter+"/"+e.stringParameter);
                    foreach(var t in s.transitions)b.AppendLine(" TRANS "+(t.isExit?"EXIT":t.destinationState!=null?t.destinationState.name:t.destinationStateMachine!=null?t.destinationStateMachine.name:"null")+" duration="+t.duration+" fixed="+t.hasFixedDuration+" exit="+t.hasExitTime+" time="+t.exitTime+" offset="+t.offset+" interrupt="+t.interruptionSource+" ordered="+t.orderedInterruption+" cond="+string.Join(",",t.conditions.Select(x=>x.parameter+":"+x.mode+":"+x.threshold)));
                }
                foreach(var t in layer.stateMachine.anyStateTransitions)b.AppendLine("ANY "+t.destinationState?.name+" duration="+t.duration+" exit="+t.hasExitTime+" offset="+t.offset+" interrupt="+t.interruptionSource+" cond="+string.Join(",",t.conditions.Select(x=>x.parameter+":"+x.mode+":"+x.threshold)));
            }
        }
        var config=AssetDatabase.LoadAssetAtPath<PlayerStateConfig>("Assets/Mock/ScriptableObjects/PlayerStateConfig.asset");
        foreach(bool locked in new[]{false,true})for(int i=0;i<config.GetLightAttackComboCount(locked);i++)b.AppendLine("COMBO light locked="+locked+" step="+i+" clip="+config.GetLightAttackClips(locked)[i].name+" delay="+config.GetLightAttackComboWindowDelay(locked,i));
        for(int i=0;i<config.StrongAttackClips.Count;i++)b.AppendLine("COMBO heavy step="+i+" clip="+config.StrongAttackClips[i].name+" delay="+config.GetStrongAttackComboWindowDelay(i));
        System.IO.File.WriteAllText("AgentScripts/AttackQuality"+suffix+".txt",b.ToString());return b.ToString();
    }
}
