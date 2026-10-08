using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class ValidateAttackQuality
{
    static IEnumerable<AnimatorState> States(AnimatorStateMachine sm){foreach(var s in sm.states)yield return s.state;foreach(var sub in sm.stateMachines)foreach(var s in States(sub.stateMachine))yield return s;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Validate()
    {
        Check(!EditorApplication.isPlaying&&!EditorUtility.scriptCompilationFailed,"Compiled Edit Mode required");var b=new StringBuilder();int count=0;
        foreach(string who in new[]{"Player","Enemy"}) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab");var ac=(AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
            Check(!prefab.GetComponent<Animator>().applyRootMotion,"Automatic root motion changed");
            var controller=(UnityEngine.Object)(who=="Player"?(Component)prefab.GetComponent<PlayerController>():prefab.GetComponent<EnemyController>());
            Check(Mathf.Abs(new SerializedObject(controller).FindProperty("_attackRootMotionScale").floatValue-(who=="Player"?.25f:.18f))<.00001f,"Root scale changed");
            var affected=States(ac.layers[0].stateMachine).Where(s=>s.behaviours.OfType<NormalAttackSpeedState>().Any()).ToArray();
            Check(affected.Select(s=>s.speedParameter).Distinct().Count()==affected.Length,"State multiplier ownership collision");
            foreach(var s in affected) {
                count++;var phase=s.behaviours.OfType<NormalAttackSpeedState>().Single();var c=(AnimationClip)s.motion;
                Check(s.tag==NormalAttackSpeedState.AttackTag&&s.speedParameterActive,"Missing phase setup");
                Check(ac.parameters.Any(p=>p.name==s.speedParameter&&p.type==AnimatorControllerParameterType.Float&&p.defaultFloat==1f),"Invalid speed parameter");
                Check(s.behaviours.OfType<AttackRootMotionState>().Count()==1,"Root gate changed");
                var events=AnimationUtility.GetAnimationEvents(c);Check(events.Any(e=>e.functionName=="AnimEvent_EnableWeaponHitbox")&&events.Any(e=>e.functionName=="AnimEvent_DisableWeaponHitbox")&&events.Any(e=>e.functionName=="AnimEvent_OnAttackFinished"),"Missing gameplay event");
                foreach(var t in s.transitions.Where(t=>t.destinationState!=null&&t.destinationState.name.Contains("Idle")))Check(t.hasExitTime&&t.exitTime>=1f&&t.offset==0f&&t.hasFixedDuration,"Premature Idle transition");
                foreach(var e in events.Where(e=>e.functionName=="AnimEvent_OnComboWindowOpened")) {
                    var close=events.First(x=>x.functionName=="AnimEvent_OnComboWindowClosed");float before=(close.time-e.time)/s.speed;float after=WallTime(c,phase,close.time,s.speed)-WallTime(c,phase,e.time,s.speed);
                    Check(after/before>.9f,"Combo window shortened over 10%: "+s.name);b.AppendLine(who+"/"+s.name+" window seconds "+before.ToString("F3")+" -> "+after.ToString("F3"));
                }
                var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var graph=PlayableGraph.Create("Phase root motion check");
                try {
                    foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
                    var a=root.GetComponent<Animator>();a.fireEvents=false;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,c);p.SetApplyFootIK(false);var output=AnimationPlayableOutput.Create(graph,"Root",a);output.SetSourcePlayable(p);graph.Play();
                    Vector3 baseline=Travel(graph,p,a,c,null,s.speed);Vector3 changed=Travel(graph,p,a,c,phase,s.speed);
                    Check(Vector3.Distance(baseline,changed)<.015f,"Phase changed total clip root motion: "+s.name+" "+baseline+" / "+changed);
                    p.SetTime(c.length*.3f);p.SetSpeed(0);graph.Evaluate(0);double paused=p.GetTime();graph.Evaluate(.1f);Check(Math.Abs(p.GetTime()-paused)<.000001,"Paused clip advanced");
                    b.AppendLine(who+"/"+s.name+" root XZ baseline="+baseline.ToString("F4")+" phase="+changed.ToString("F4")+" full duration="+(c.length/s.speed).ToString("F3")+" -> "+WallTime(c,phase,c.length,s.speed).ToString("F3")+"s; frozen clip test passed");
                }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
            }
            foreach(var s in States(ac.layers[0].stateMachine).Where(s=>s.tag==PlayerAttacker.JustAvoidCounterTag))Check(!s.behaviours.OfType<NormalAttackSpeedState>().Any()&&s.speedParameter==JustAvoidCounterAnimation.SpeedParameter,"Counter overlap");
        }
        b.AppendLine("Validated "+count+" normal attack profiles. Isolated clip/root math; no Play Mode or input/physics runtime claims.");System.IO.File.WriteAllText("AgentScripts/AttackQualityValidation.txt",b.ToString());return b.ToString();
    }
    static float WallTime(AnimationClip c,NormalAttackSpeedState phase,float end,float baseSpeed) {float total=0;int n=10000;float dt=end/n;for(int i=0;i<n;i++)total+=dt/(baseSpeed*phase.EvaluateSpeed((i+.5f)*dt/c.length));return total;}
    static Vector3 Travel(PlayableGraph graph,AnimationClipPlayable p,Animator a,AnimationClip c,NormalAttackSpeedState phase,float baseSpeed)
    {
        p.SetSpeed(0);p.SetTime(0);graph.Evaluate(0);Vector3 sum=Vector3.zero;
        int guard=0;while(p.GetTime()<c.length-.0000001&&guard++<30000) {
            double time=p.GetTime();float speed=baseSpeed*(phase==null?1:phase.EvaluateSpeed((float)time/c.length));float dt=Mathf.Min(1f/1000f,(c.length-(float)time)/speed);p.SetSpeed(speed);graph.Evaluate(dt);Vector3 d=a.deltaPosition;d.y=0;sum+=d;
        }
        return sum;
    }
}
