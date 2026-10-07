using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VerifyCounterTempoControl
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Speed(Animator animator,float expected,string where)=>Check(Mathf.Abs(animator.GetFloat(JustAvoidCounterAnimation.SpeedParameter)-expected)<.0001f,where+": "+animator.GetFloat(JustAvoidCounterAnimation.SpeedParameter));
    static AnimationEvent PreviewEvent(AnimationClip clip,string name,AnimatorStateInfo state)
    {
        var e=AnimationUtility.GetAnimationEvents(clip).Single(x=>x.functionName==name);
        var fields=typeof(AnimationEvent).GetFields(BindingFlags.NonPublic|BindingFlags.Instance);
        fields.Single(f=>f.FieldType==typeof(AnimatorStateInfo)).SetValue(e,state);
        var source=fields.Single(f=>f.Name=="m_Source");source.SetValue(e,Enum.Parse(source.FieldType,"Animator"));
        return e;
    }
    public static string Main()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode only.");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
            foreach(var behaviour in root.GetComponents<MonoBehaviour>())
                if(!(behaviour is JustAvoidCounterAnimation)&&!(behaviour is AnimationSpeedController))UnityEngine.Object.DestroyImmediate(behaviour);
            var a=root.GetComponent<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=false;a.Rebind();a.Update(0);
            var counter=root.GetComponent<JustAvoidCounterAnimation>();
            var speed=root.GetComponent<AnimationSpeedController>();Check(speed!=null,"Existing speed controller missing.");speed.Init();
            foreach(var state in new[]{"ARPG_Samurai_Attack_Heavy1","ARPG_Samurai_Attack_Heavy2"})
            {
                a.Play("Base Layer."+state,0,0);a.Update(0);
                Check(a.GetCurrentAnimatorStateInfo(0).IsTag(PlayerAttacker.JustAvoidCounterTag),"Preview state evaluation unavailable: hash="+a.GetCurrentAnimatorStateInfo(0).fullPathHash+" expected="+Animator.StringToHash("Base Layer."+state));
                var info=a.GetCurrentAnimatorStateInfo(0);
                var controller=(UnityEditor.Animations.AnimatorController)a.runtimeAnimatorController;
                var savedState=controller.layers[0].stateMachine.states.Single(s=>s.state.name==state).state;
                var behaviour=savedState.behaviours.OfType<JustAvoidCounterSpeedState>().Single();
                behaviour.OnStateEnter(a,info,0);
                Speed(a,.78f,state+" entry");
                // Editor Previewでは通知が自動実行されないため、保存Eventと実State情報で通知処理のみ検証する。
                var clip=(AnimationClip)savedState.motion;
                counter.AnimEvent_JustAvoidCounterSlash(PreviewEvent(clip,"AnimEvent_JustAvoidCounterSlash",info));
                Speed(a,1.1f,state+" slash event");
                speed.SetStatus(.8f);speed.SetTemporary(0,.14f);Check(a.speed==0,"Temporary HitStop must stop Animator.");
                counter.OnCounterHit();Speed(a,1,state+" contact follow-through");Check(a.speed==0,"Counter speed must not release HitStop.");
                speed.ClearTemporary();Check(Mathf.Approximately(a.speed,.8f),"HitStop release must preserve status slow.");Speed(a,1,"Follow-through after HitStop");speed.SetStatus(1);
                behaviour.OnStateExit(a,info,0);Speed(a,1,"State exit reset");
                a.Play("Base Layer.NoWeaponIdle",0,0);a.Update(0);
            }
            int first=Animator.StringToHash("Base Layer.ARPG_Samurai_Attack_Heavy1"),second=Animator.StringToHash("Base Layer.ARPG_Samurai_Attack_Heavy2");
            counter.BeginCounter(first);Speed(a,.78f,"First counter entry");
            counter.BeginCounter(second);counter.ExitCounter(first);Speed(a,.78f,"Previous state exit must preserve next windup");
            var attacker=new PlayerAttacker(null,null,null,null,null,root.transform);attacker.EndAttack();Speed(a,1,"Cancel reset");attacker.Dispose();
            a.Play("Base Layer.NoWeaponIdle",0,0);a.Update(0);
            a.Play("Base Layer.ARPG_Samurai_Attack_Heavy2",0,0);a.Update(0);counter.BeginCounter(second);Speed(a,.78f,"Air swing entry");
            var lastState=((UnityEditor.Animations.AnimatorController)a.runtimeAnimatorController).layers[0].stateMachine.states.Single(s=>s.state.name.EndsWith("Heavy2")).state;
            counter.AnimEvent_JustAvoidCounterResetSpeed(PreviewEvent((AnimationClip)lastState.motion,"AnimEvent_JustAvoidCounterResetSpeed",a.GetCurrentAnimatorStateInfo(0)));
            Speed(a,1,"Air swing recovery event");
            return "PASS (Edit Mode direct notification tests only): saved behaviour and Event callbacks .78->1.10 in both states, contact notification resets state multiplier1 while temporary Animator speed remains0; temporary release preserves existing status .8; exit/cancel/air swing recovery callbacks restore1; old state exit preserves next windup. Preview notifications explicitly invoked using saved Event / State info; actual runtime Event delivery, Game, VFX and Play Mode not tested.";
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}
