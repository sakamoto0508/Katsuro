using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class TestAttackBuffer
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    sealed class Harness : PlayerAttackState {
        readonly int count;public readonly List<int> Steps=new List<int>();
        public Harness(PlayerStateContext c,PlayerStateMachine sm,int n):base(c,sm,100f){count=n;}
        public override PlayerStateId Id=>PlayerStateId.LightAttack;
        protected override int MaxComboSteps=>count;
        protected override float ResolveComboWindowDelay(int step)=>.1f;
        protected override void TriggerAttack(int step)=>Steps.Add(step);
    }
    static string Path(AnimatorStateMachine sm,string prefix,string name){foreach(var s in sm.states)if(s.state.name==name)return prefix+"."+name;foreach(var sub in sm.stateMachines){var p=Path(sub.stateMachine,prefix+"."+sub.stateMachine.name,name);if(p!=null)return p;}return null;}
    public static string Test()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        try {
            foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;
            var rb=root.GetComponent<Rigidbody>();rb.isKinematic=false;rb.linearVelocity=Vector3.zero;
            var a=root.GetComponent<Animator>();a.fireEvents=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();
            var attacker=new PlayerAttacker(null,null,null,null,null,root.transform);
            var mover=new PlayerMover(null,rb,root.transform,null,root.transform,null,.25f);
            var context=new PlayerStateContext(root.GetComponent<PlayerController>(),null,null,null,mover,null,null,null,null,null,null,attacker,null,null,null);
            var b=new StringBuilder();
            foreach(int count in new[]{5,4,2}) {
                var sm=new PlayerStateMachine(context);var h=new Harness(context,sm,count);var states=(Dictionary<PlayerStateId,PlayerState>)typeof(PlayerStateMachine).GetField("_states",Flags).GetValue(sm);states[PlayerStateId.LightAttack]=h;sm.ChangeState(PlayerStateId.LightAttack);
                Check(h.Steps.SequenceEqual(new[]{0}),"Initial stage wrong");
                for(int step=1;step<count;step++) {
                    h.OnLightAttack();h.OnLightAttack();Check(h.Steps.Count==step,"Closed window consumed input");
                    h.OnComboWindowOpened();h.Update(.05f);Check(h.Steps.Count==step,"Combo delay changed");
                    a.speed=0;h.Update(1f);h.OnStrongAttack();Check(h.Steps.Count==step,"HitStop advanced buffer");
                    a.speed=1;h.Update(.06f);Check(h.Steps.Count==step+1&&h.Steps.Last()==step,"One buffered input skipped/repeated stage");
                    h.Update(.2f);Check(h.Steps.Count==step+1,"One buffer consumed multiple times");
                }
                h.OnLightAttack();h.OnComboWindowOpened();h.Update(.2f);Check(h.Steps.Count==count,"Final step accepted extra input");
                h.OnAttackAnimationFinished();Check(typeof(PlayerStateMachine).GetField("_currentState",Flags).GetValue(sm) is PlayerLocomotionState,"Finished failed to return Idle logic");sm.Dispose();
                b.AppendLine("Base combo logic "+count+" stages: buffered closed-window input, open delay, pause/resume, one consume per queued input, no skip/repeat, final completion passed.");
            }
            var machine=new PlayerStateMachine(context);var harness=new Harness(context,machine,5);var dictionary=(Dictionary<PlayerStateId,PlayerState>)typeof(PlayerStateMachine).GetField("_states",Flags).GetValue(machine);dictionary[PlayerStateId.LightAttack]=harness;machine.ChangeState(PlayerStateId.LightAttack);
            harness.OnLightAttack();harness.OnComboWindowOpened();harness.OnComboWindowClosed();harness.Update(.2f);Check(harness.Steps.Count==1,"Closed window still consumed input");
            var ac=(AnimatorController)a.runtimeAnimatorController;a.Play(Path(ac.layers[0].stateMachine,ac.layers[0].name,"LockOnLightAttack"),0,.4f);a.Update(0);
            typeof(PlayerAttackState).GetField("_currentAttackDuration",Flags).SetValue(harness,.01f);harness.Update(.3f);Check(ReferenceEquals(typeof(PlayerStateMachine).GetField("_currentState",Flags).GetValue(machine),harness),"Clip length timer interrupted active animation");
            attacker.EnableWeaponHitbox();machine.ChangeState(PlayerStateId.Locomotion);Check(!(bool)typeof(PlayerAttacker).GetField("_isHitboxActive",Flags).GetValue(attacker),"Cancel leaked hitbox");machine.Dispose();
            b.AppendLine("Closed window, premature timeout protection, cancellation hitbox cleanup passed. Harness uses existing base logic; hardware input/real Event dispatch/physics are not runtime-tested.");System.IO.File.WriteAllText("AgentScripts/AttackBufferTests.txt",b.ToString());return b.ToString();
        }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
