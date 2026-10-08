using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class ValidateNormalHitFeedback
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,F).SetValue(o,value);
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,F).GetValue(o);
    static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
    static string Path(AnimatorStateMachine sm,string prefix,string name){foreach(var s in sm.states)if(s.state.name==name)return prefix+"."+name;foreach(var sub in sm.stateMachines){var p=Path(sub.stateMachine,prefix+"."+sub.stateMachine.name,name);if(p!=null)return p;}return null;}
    static readonly HumanBodyBones[] IDs={HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head};
    public static string Validate()
    {
        Check(!EditorApplication.isPlaying&&!EditorUtility.scriptCompilationFailed,"Compiled Edit Mode required");var b=new StringBuilder();var scene=EditorSceneManager.NewPreviewScene();var roots=new List<GameObject>();
        try {
            var enemy=Make("Enemy",scene);roots.Add(enemy);var source=new GameObject("Hit source");roots.Add(source);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(source,scene);
            var a=enemy.GetComponent<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.fireEvents=false;a.Rebind();a.Update(0);
            var bones=IDs.Select(a.GetBoneTransform).Where(t=>t!=null).ToArray();var baseline=bones.Select(t=>t.localRotation).ToArray();var hips=a.GetBoneTransform(HumanBodyBones.Hips);Quaternion hipsBefore=hips.localRotation;Vector3 rootBefore=enemy.transform.position;Quaternion rootRotation=enemy.transform.rotation;
            var reaction=new EnemyBoneHitReaction(enemy.transform,a);
            int directions=0;foreach(float yaw in new[]{0f,90f,180f,270f})foreach(Vector3 localSource in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}) {
                enemy.transform.rotation=Quaternion.Euler(0,yaw,0);source.transform.position=enemy.transform.position+enemy.transform.TransformDirection(localSource);
                var info=new DamageInfo(1,enemy.transform.position,-enemy.transform.forward,source,null);
                Check(Vector3.Dot(EnemyBoneHitReaction.GetLocalImpulse(enemy.transform,info),-localSource)>.999f,"Reaction direction sign wrong");
                Vector3 headBefore=a.GetBoneTransform(HumanBodyBones.Head).position;
                reaction.Begin(info,7.5f,.16f,1f);reaction.Tick(.0288f,1f);reaction.Apply();
                Vector3 headDelta=a.GetBoneTransform(HumanBodyBones.Head).position-headBefore;
                Check(Vector3.Dot(headDelta,enemy.transform.TransformDirection(-localSource))>0f,"Actual skeleton moved toward attacker");reaction.Clear();directions++;
            }
            enemy.transform.rotation=rootRotation;source.transform.position=enemy.transform.position+enemy.transform.forward;
            var light=new DamageInfo(1,enemy.transform.position,-enemy.transform.forward,source,null);var heavy=new DamageInfo(1,enemy.transform.position,-enemy.transform.forward,source,null,true);
            reaction.Begin(light,7.5f,.16f,1);reaction.Tick(.0288f,1);reaction.Apply();float lightPeak=Quaternion.Angle(baseline[1],bones[1].localRotation);
            var previous=bones.Select(t=>t.localRotation).ToArray();reaction.Begin(heavy,13,.22f,1);reaction.Apply();for(int i=0;i<bones.Length;i++)Check(Quaternion.Angle(previous[i],bones[i].localRotation)<.02f,"Repeated hit pose jumped");
            reaction.Clear();reaction.Begin(heavy,13,.22f,1);reaction.Tick(.0396f,1);reaction.Apply();float heavyPeak=Quaternion.Angle(baseline[1],bones[1].localRotation);Check(heavyPeak>lightPeak*1.5f,"Heavy not stronger than Light");
            float elapsed=reaction.Elapsed;var held=bones.Select(t=>t.localRotation).ToArray();for(int i=0;i<100;i++){reaction.RemoveOffsets();reaction.Tick(.016f,0);reaction.Apply();}
            Check(reaction.Elapsed==elapsed,"HitStop advanced reaction");for(int i=0;i<bones.Length;i++)Check(Quaternion.Angle(held[i],bones[i].localRotation)<.02f,"Paused pose accumulated");
            for(int i=0;i<1000;i++){reaction.Begin(i%2==0?light:heavy,i%2==0?7.5f:13f,i%2==0?.16f:.22f,1);reaction.Tick(.01f,1);reaction.Apply();for(int j=0;j<bones.Length;j++)Check(Quaternion.Angle(baseline[j],bones[j].localRotation)<=13.02f,"Repeated hit exceeded angular bound");}
            reaction.Tick(.5f,1);Check(!reaction.IsActive,"Reaction did not finish");for(int i=0;i<bones.Length;i++)Check(Quaternion.Angle(baseline[i],bones[i].localRotation)<.02f,"Recovery retained bone offset");
            reaction.Begin(heavy,13,.22f,.55f);reaction.Tick(.0396f,1);reaction.Apply();Check(Mathf.Abs(Quaternion.Angle(baseline[1],bones[1].localRotation)-heavyPeak*.55f)<.03f,"Attack reaction attenuation failed");reaction.Clear();
            reaction.Begin(light,7.5f,.16f,1);reaction.Apply();Quaternion fresh=Quaternion.Euler(3,4,5)*baseline[1];bones[1].localRotation=fresh;reaction.RemoveOffsets();Check(Quaternion.Angle(fresh,bones[1].localRotation)<.02f,"Fresh Animator pose had old inverse subtracted");reaction.Clear();bones[1].localRotation=baseline[1];
            Check(Quaternion.Angle(hipsBefore,hips.localRotation)<.02f&&enemy.transform.position==rootBefore&&Quaternion.Angle(rootRotation,enemy.transform.rotation)<.02f,"Root/Hips modified");
            b.AppendLine(directions+" direction/yaw cases; Light chest peak="+lightPeak.ToString("F2")+"deg Heavy="+heavyPeak.ToString("F2")+"deg; continuous Light->Heavy; 100 paused frames; 1000 repeated hits; clean recovery; fresh Animator pose preservation; Root/Hips unchanged passed.");

            var ec=enemy.GetComponent<EnemyController>();var animation=enemy.GetComponent<EnemyAnimationController>();animation.Init();
            var fb=enemy.GetComponent<CombatFeedback>();Set(fb,"_initialized",true);Set(fb,"_enemyOwner",ec);Set(fb,"_enemyAnimation",animation);Set(fb,"_reactionAnimator",a);Set(fb,"_enemyBoneReaction",reaction);Set(fb,"_renderers",new Renderer[0]);fb.enabled=true;
            Set(ec,"_combatFeedback",fb);Set(ec,"_enemyAnimController",animation);Set(ec,"_health",new EnemyHealth(null));
            var so=new SerializedObject(ec);var status=so.FindProperty("_enemyStuts").objectReferenceValue as EnemyStuts;var names=so.FindProperty("_animName").objectReferenceValue as AnimationName;
            var mover=new EnemyMover(status,enemy.transform,source.transform,animation,enemy.GetComponent<Rigidbody>(),null,a,names,.18f);Set(ec,"_mover",mover);mover.HoldMovementForAttack();
            var attacker=new EnemyAttacker(animation,new EnemyAttackData[0],new EnemyWeapon[0],status,enemy.transform);Set(ec,"_attacker",attacker);attacker.EnableWeaponHitbox();
            var controller=(AnimatorController)a.runtimeAnimatorController;string attackPath=Path(controller.layers[0].stateMachine,controller.layers[0].name,"LightAttack1");a.Play(attackPath,0,.2f);a.Update(0);mover.BeginAttackRootMotion();ec.EnqueueAction(EnemyActionType.HeavySlash);
            int hash=Get<int>(ec,"_attackAnimationHash");var state=a.GetCurrentAnimatorStateInfo(0);var pending=Get<EnemyActionType?>(ec,"_pendingAction");var nav=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();bool navEnabled=nav.enabled;float navSpeed=nav.speed;Quaternion beforeHitRoot=enemy.transform.rotation;
            ec.ApplyDamage(light);ec.ApplyDamage(heavy);
            Check(reaction.IsActive,"Normal Hit did not start presentation reaction");
            Check(a.GetCurrentAnimatorStateInfo(0).fullPathHash==state.fullPathHash&&Get<int>(ec,"_attackAnimationHash")==hash,"Normal Hit replaced Base Layer/attack owner");
            Check(mover.IsUsingAttackRootMotion&&!animation.IsReacting&&Get<bool>(attacker,"_isHitboxActive")&&Get<EnemyActionType?>(ec,"_pendingAction")==pending,"Normal Hit cancelled root/attack/AI action");
            Check(nav.enabled==navEnabled&&nav.speed==navSpeed&&enemy.transform.position==rootBefore&&Quaternion.Angle(enemy.transform.rotation,beforeHitRoot)<.02f,"Normal Hit changed navigation/root");
            reaction.Apply();fb.CancelNormalHitReaction();Check(!reaction.IsActive,"Counter/Death cancel failed");
            ec.ApplyDamage(new DamageInfo(1,enemy.transform.position,-enemy.transform.forward,source,null,true,default,true));
            Check(animation.IsReacting&&!mover.IsUsingAttackRootMotion&&!Get<bool>(attacker,"_isHitboxActive")&&!reaction.IsActive,"Counter full-body separation failed");
            ec.ApplyDamage(new DamageInfo(1000000,enemy.transform.position,-enemy.transform.forward,source,null));Check(Get<bool>(ec,"_dead")&&!reaction.IsActive&&!animation.IsReacting,"Death reaction cleanup failed");
            b.AppendLine("EnemyController.ApplyDamage integration: Light/Heavy preserve Base Layer, attack hash, root motion, hitbox, pending action, nav settings; Counter retains full-body interruption; Death clears normal/full-body reactions. Injected dependencies; no game loop or physics/AI decisions executed.");

            var player=Make("Player",scene);roots.Add(player);var pf=player.GetComponent<CombatFeedback>();var pa=player.GetComponent<Animator>();Set(pf,"_initialized",true);Set(pf,"_renderers",new Renderer[0]);pf.enabled=true;
            pf.Hit(new DamageInfo(1,player.transform.position,player.transform.forward,null,null));Check(Get<Vector3>(pf,"_reactionEuler")==new Vector3(-5,0,0)&&Get<EnemyBoneHitReaction>(pf,"_enemyBoneReaction")==null,"Player Light behavior changed");
            pf.Hit(new DamageInfo(1,player.transform.position,player.transform.forward,null,null,true));Check(Get<Vector3>(pf,"_reactionEuler")==new Vector3(-10,0,0)&&Get<float>(pf,"_reactionDuration")==.18f,"Player Heavy behavior changed");
            b.AppendLine("Player shared-code regression: original 5/10deg Chest Euler path and .18s duration retained; no Enemy reaction helper.");
            System.IO.File.WriteAllText("AgentScripts/NormalHitFeedbackTests.txt",b.ToString());return b.ToString();
        }finally{foreach(var root in roots)if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static GameObject Make(string name,UnityEngine.SceneManagement.Scene scene)
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+name+".prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;return root;
    }
}
