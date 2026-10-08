using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
public static class TestAttackController
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static string Path(AnimatorStateMachine sm,string prefix,string name){foreach(var s in sm.states)if(s.state.name==name)return prefix+"."+name;foreach(var sub in sm.stateMachines){var p=Path(sub.stateMachine,prefix+"."+sub.stateMachine.name,name);if(p!=null)return p;}return null;}
    public static string Test()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        try {
            foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;
            foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
            var a=root.GetComponent<Animator>();var ac=(AnimatorController)a.runtimeAnimatorController;string first=Path(ac.layers[0].stateMachine,ac.layers[0].name,"LockOnLightAttack"),second=Path(ac.layers[0].stateMachine,ac.layers[0].name,"LOLightAttack2");
            a.fireEvents=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.SetBool("IsSwordDrawn",true);a.SetBool("IsLockOn",true);a.Play(first,0,0f);a.Update(0);
            var b=new StringBuilder();b.AppendLine("Initial state="+a.GetCurrentAnimatorStateInfo(0).fullPathHash+" expected="+Animator.StringToHash(first)+" path="+first);
            Check(a.GetCurrentAnimatorStateInfo(0).IsTag(NormalAttackSpeedState.AttackTag),"Actual controller failed to enter normal attack");
            for(int i=0;i<30;i++)a.Update(.01f);
            float paused=a.GetCurrentAnimatorStateInfo(0).normalizedTime;float multiplier=a.GetFloat("AttackPhase_LockOnLightAttack");a.speed=0;a.Update(.2f);
            Check(Mathf.Abs(a.GetCurrentAnimatorStateInfo(0).normalizedTime-paused)<.000001f,"HitStop advanced actual controller");a.speed=1;
            a.SetInteger("ComboStep",1);a.SetTrigger("LightAttack");a.Update(.01f);
            Check(a.IsInTransition(0)&&a.GetNextAnimatorStateInfo(0).fullPathHash==Animator.StringToHash(second),"Combo did not transition to step 2");
            for(int i=0;i<40;i++)a.Update(.01f);
            Check(a.GetCurrentAnimatorStateInfo(0).fullPathHash==Animator.StringToHash(second),"Old state exit cancelled next combo");
            Check(a.GetFloat("AttackPhase_LockOnLightAttack")==1f,"Outgoing parameter not reset");
            b.AppendLine("Actual controller: state entry, phase speed="+multiplier+", HitStop freeze, combo 1->2, outgoing multiplier reset passed.");
            Chain(a,ac,new[]{"LockOnLightAttack","LOLightAttack2","LOLightAttack3","LOLightAttack4","LOLightAttack5"},"LightAttack",true,b);
            Chain(a,ac,new[]{"ARPG_Samurai_Attack_Combo1","ARPG_Samurai_Attack_Combo2","ARPG_Samurai_Attack_Combo3","ARPG_Samurai_Attack_Combo4"},"LightAttack",false,b);
            Chain(a,ac,new[]{"StrongAttack","ARPG_Halberd_Attack_Heavy2"},"StrongAttack",false,b);
            b.AppendLine("Events disabled in controller checks; physical input routing/runtime untested.");System.IO.File.WriteAllText("AgentScripts/AttackControllerTests.txt",b.ToString());return b.ToString();
        }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void Chain(Animator a,AnimatorController ac,string[] names,string trigger,bool locked,StringBuilder b)
    {
        a.Rebind();a.SetBool("IsSwordDrawn",true);a.SetBool("IsLockOn",locked);a.SetInteger("ComboStep",0);a.Play(Path(ac.layers[0].stateMachine,ac.layers[0].name,names[0]),0,0f);a.Update(0);
        for(int i=1;i<names.Length;i++) {
            for(int n=0;n<12;n++)a.Update(.01f);
            a.SetInteger("ComboStep",i);a.SetTrigger(trigger);a.Update(.01f);
            Check(a.IsInTransition(0)&&a.GetNextAnimatorStateInfo(0).fullPathHash==Animator.StringToHash(Path(ac.layers[0].stateMachine,ac.layers[0].name,names[i])),"Chain skips/repeats "+names[i]);
            for(int n=0;n<27;n++)a.Update(.01f);
            Check(a.GetCurrentAnimatorStateInfo(0).fullPathHash==Animator.StringToHash(Path(ac.layers[0].stateMachine,ac.layers[0].name,names[i])),"Chain exit reset "+names[i]);
        }
        for(int n=0;n<300;n++)a.Update(.01f);
        Check(!a.GetCurrentAnimatorStateInfo(0).IsTag(NormalAttackSpeedState.AttackTag),"Final state stuck: "+trigger);
        foreach(var p in ac.parameters.Where(p=>p.name.StartsWith(NormalAttackSpeedState.ParameterPrefix)))Check(a.GetFloat(p.name)==1f,"Stale phase multiplier: "+p.name);
        b.AppendLine("Actual controller chain "+string.Join(" -> ",names)+" -> Idle passed; all phase multipliers reset.");
        for(int stop=0;stop<names.Length;stop++) {
            a.Rebind();a.SetBool("IsSwordDrawn",true);a.SetBool("IsLockOn",locked);a.SetInteger("ComboStep",stop);a.Play(Path(ac.layers[0].stateMachine,ac.layers[0].name,names[stop]),0,0f);a.Update(0);
            for(int n=0;n<300;n++)a.Update(.01f);
            Check(!a.GetCurrentAnimatorStateInfo(0).IsTag(NormalAttackSpeedState.AttackTag),"Single/interrupted chain state stuck: "+names[stop]);
        }
        b.AppendLine("No next trigger: every stage returns to Idle, including single-stage and intermediate stops.");
    }
}
