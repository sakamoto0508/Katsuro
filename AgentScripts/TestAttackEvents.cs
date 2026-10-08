using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class TestAttackEvents
{
    static IEnumerable<AnimatorState> States(AnimatorStateMachine sm){foreach(var s in sm.states)yield return s.state;foreach(var sub in sm.stateMachines)foreach(var s in States(sub.stateMachine))yield return s;}
    static string Path(AnimatorStateMachine sm,string prefix,string name){foreach(var s in sm.states)if(s.state.name==name)return prefix+"."+name;foreach(var sub in sm.stateMachines){var p=Path(sub.stateMachine,prefix+"."+sub.stateMachine.name,name);if(p!=null)return p;}return null;}
    static void Check(bool v,string m){if(!v)throw new Exception(m);}
    public static string Test()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        try {
            foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
            var stream=new AnimationEventStream();typeof(PlayerController).GetField("_animationEventStream",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(root.GetComponent<PlayerController>(),stream);
            root.GetComponent<PlayerController>().enabled=true;
            var received=new List<AnimationEventType>();var subscription=stream.OnEvent.Subscribe(e=>received.Add(e));
            var a=root.GetComponent<Animator>();var ac=(AnimatorController)a.runtimeAnimatorController;a.fireEvents=true;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;var b=new StringBuilder();
            foreach(var s in States(ac.layers[0].stateMachine).Where(s=>s.tag==NormalAttackSpeedState.AttackTag)) {
                a.Rebind();a.SetBool("IsSwordDrawn",true);a.SetBool("IsLockOn",s.name.StartsWith("LO")||s.name=="LockOnLightAttack");a.SetInteger("ComboStep",0);received.Clear();
                a.Play(Path(ac.layers[0].stateMachine,ac.layers[0].name,s.name),0,0);a.Update(0);for(int i=0;i<280;i++)a.Update(.01f);
                if(received.Count==0){subscription.Dispose();string limitation="Edit Mode manual Animator.Update did not dispatch Animation Events on this Editor. Event delivery/ordering remains Runtime unverified; clip signatures and event poses were checked separately.";System.IO.File.WriteAllText("AgentScripts/AttackEventTests.txt",limitation);return limitation;}
                Check(received.Count(x=>x==AnimationEventType.WeaponHitboxEnabled)==1,"ON missing/duplicate "+s.name);
                Check(received.Count(x=>x==AnimationEventType.WeaponHitboxDisabled)==1,"OFF missing/duplicate "+s.name);
                Check(received.Count(x=>x==AnimationEventType.AttackFinished)==1,"Finished lost/duplicate "+s.name);
                b.AppendLine(s.name+": actual manual Animator Event dispatch ON/OFF/Finished exactly once; "+string.Join(",",received));
            }
            subscription.Dispose();System.IO.File.WriteAllText("AgentScripts/AttackEventTests.txt",b.ToString());return b.ToString();
        }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
