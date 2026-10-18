using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ValidateUAL
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static StringBuilder log=new StringBuilder();
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);log.AppendLine("PASS "+text);}
 static EnemyAttackPattern P(string kind)=>AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/"+kind+"Combo.asset");
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  log.Clear();try{Assets();foreach(string kind in new[]{"Regular","Heavy"}){Runner(kind);Integration(kind);}File.WriteAllText("Artifacts/UAL2Combo/Validation.txt",log.ToString());}
  catch(Exception e){File.WriteAllText("Artifacts/UAL2Combo/Validation.txt",log+"FAIL "+e);throw;}
 }
 static void Assets()
 {
  var config=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");
  Check(config.NearCandidates.Count(c=>c.Attack!=null)==4 && config.NearCandidates.Count(c=>c.Pattern!=null)==2,"Four singles plus two independent combos");
  var maker=new EnemyDecisionMaker(new System.Random(312));var copy=UnityEngine.Object.Instantiate(config);
  try
  {
   foreach(var candidate in config.NearCandidates.Where(c=>c.Attack!=null||c.Pattern!=null))
   {
    copy.NearCandidates=new[]{candidate};copy.RepeatPenalty=1;var d=maker.DecideAction(1,new EnemyDecision(EnemyActionType.Wait),copy);
    Check(d.Attack==candidate.Attack&&d.Pattern==candidate.Pattern,"Individual selection "+(candidate.Pattern!=null?candidate.Pattern.PatternName:candidate.Attack.AnimatorTrigger));
   }
   copy.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=P("Regular"),Weight=1},new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=P("Heavy"),Weight=1}};
   copy.RepeatPenalty=0;copy.RepeatPenaltyPerAttack=true;
   Check(maker.DecideAction(1,new EnemyDecision(EnemyActionType.Slash,null,P("Regular")),copy).Pattern==P("Heavy"),"RepeatPenalty distinguishes combo identity");
  }
  finally{UnityEngine.Object.DestroyImmediate(copy);}
  var test=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig_ComboTest.asset");
  Check(test.NearCandidates.Count(c=>c.Pattern!=null)==2 && test.NearCandidates.Where(c=>c.Attack!=null).All(c=>c.Weight==0),"User combo-test config migrated without enabling singles");
  foreach(string kind in new[]{"Regular","Heavy"})
  {
   var pattern=P(kind);Check(pattern!=null&&pattern.IsValid,"Pattern serialized "+kind);
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
   var sm=controller.layers[0].stateMachine.stateMachines.Single(c=>c.stateMachine.name=="Attacks").stateMachine;
   var state=sm.states.Single(s=>s.state.name==kind+"Combo").state;var clip=(AnimationClip)state.motion;
   Check(clip.humanMotion&&clip.hasRootCurves,"Humanoid and root curves "+kind);
   Check(state.tag=="EnemyCombo"&&state.transitions.Length==0&&state.behaviours.Any(b=>b is AttackRootMotionState),"Attack root ownership and no early automatic exit "+kind);
   var settings=AnimationUtility.GetAnimationClipSettings(clip);
   Check(settings.loopBlendOrientation&&settings.keepOriginalOrientation&&Math.Abs(settings.orientationOffsetY-180)<.01&&settings.loopBlendPositionY&&!settings.loopBlendPositionXZ,"Root Transform settings "+kind);
   var events=AnimationUtility.GetAnimationEvents(clip);int count=kind=="Regular"?3:4;
   Check(events.Count(e=>e.functionName=="AnimEvent_ComboEnableWeaponHitbox")==count && events.Count(e=>e.functionName=="AnimEvent_ComboFinished")==1,"Strike window and unique finish event count "+kind);
   for(int i=1;i<=count;i++)
   {
    var on=events.Single(e=>e.functionName=="AnimEvent_ComboEnableWeaponHitbox"&&e.intParameter==i);
    var off=events.Single(e=>e.functionName=="AnimEvent_ComboDisableWeaponHitbox"&&e.intParameter==i);
    Check(on.time<off.time&&off.time<clip.length,"Ordered strike "+kind+" "+i+" "+on.time+"-"+off.time);
   }
  }
  Check(!AssetDatabase.IsValidFolder("Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo")&&!AssetDatabase.IsValidFolder("Assets/Mock/Animation/Enemy/TwoHitCombo"),"Obsolete TwoHit assets removed");
 }
 static void Runner(string kind)
 {
  var p=P(kind);int starts=0,finishes=0;bool success=false;
  var r=new EnemyAttackPatternRunner(x=>{starts++;return true;},()=>{},ok=>{finishes++;success=ok;});
  Check(r.Begin(p),"Runner begins "+kind);r.Tick(p.Windup+.01f);
  Check(r.OpenWindow(1,p.StateHash)&&!r.OpenWindow(1,p.StateHash),"Duplicate ON does not reset target hits "+kind);
  Check(!r.CloseWindow(2,p.StateHash)&&r.CloseWindow(1,p.StateHash),"Wrong window OFF rejected "+kind);
  Check(!r.OpenWindow(1,p.StateHash)&&r.OpenWindow(2,p.StateHash),"Only next strike can open "+kind);
  Check(!r.EndClip(p.StateHash+1)&&r.EndClip(p.StateHash),"Finish validates state "+kind);
  Check(finishes==0&&r.IsActive,"Recovery still busy "+kind);r.Tick(p.RecoveryDuration+.01f);
  Check(finishes==1&&success&&!r.IsActive&&starts==1,"One clip and one final completion "+kind);
  r.Tick(20);Check(!r.EndClip(p.StateHash)&&finishes==1,"Late completion ignored "+kind);
  r.Begin(p);r.Tick(p.Windup+.01f);r.Cancel();r.Tick(100);Check(!r.IsActive&&!r.OpenWindow(1,p.StateHash),"Cancellation rejects all stale events "+kind);
  r.Begin(p);r.Tick(p.Windup+.01f);r.Tick(p.Timeout/p.PlaybackSpeed+.1f);Check(!r.IsActive&&!success,"Missing finish fails safely "+kind);
 }
 sealed class Fixture:IDisposable
 {
  public GameObject Go,Target;public EnemyController Enemy;public Animator Animator;public EnemyAnimationController Animation;public EnemyAttacker Attacker;public EnemyWeapon Weapon;public EnemyMover Mover;public EnemyAI AI;public EnemyDecisionConfig Config;public EnemyAttackPattern Pattern;
  public EnemyAttackPatternRunner Runner=>(EnemyAttackPatternRunner)Get(Enemy,"_patternRunner");
  public int Hits;public float Damage;public bool AllExpected=true;public Vector3 RootOutput;
  readonly UnityEngine.SceneManagement.Scene scene;readonly HashSet<string> delivered=new HashSet<string>();
  public Fixture(string kind)
  {
   Pattern=P(kind);scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
   Go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(Go,scene);Go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   Target=new GameObject("UAL validation target");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(Target,scene);Target.transform.position=Vector3.forward;
   var victim=Target.AddComponent<EnemyController>();Set(victim,"_health",new EnemyHealth(null));Target.AddComponent<BoxCollider>();victim.HitReceived+=info=>{Hits++;Damage+=info.DamageAmount;AllExpected&=info.IsHeavy==Pattern.Attack.IsHeavy;};
   Enemy=Go.GetComponent<EnemyController>();Animator=Go.GetComponent<Animator>();Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);
   Animation=Go.GetComponent<EnemyAnimationController>();Animation.Init();var status=(EnemyStuts)Get(Enemy,"_enemyStuts");
   Weapon=new EnemyWeapon((Collider[])Get(Enemy,"_enemyWeaponColliders"),10);Weapon.Init();Attacker=new EnemyAttacker(Animation,(EnemyAttackData[])Get(Enemy,"_attackData"),new[]{Weapon},status,Go.transform);
   Mover=new EnemyMover(status,Go.transform,Target.transform,Animation,null,null,Animator,(AnimationName)Get(Enemy,"_animName"),.18f);
   Set(Enemy,"_enemyAnimController",Animation);Set(Enemy,"_attacker",Attacker);Set(Enemy,"_mover",Mover);Set(Enemy,"_health",new EnemyHealth(status));
   Config=ScriptableObject.CreateInstance<EnemyDecisionConfig>();Config.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=Pattern,Weight=1}};Config.RepeatPenalty=1;
   AI=new EnemyAI(Enemy,Target.transform,null,Config);Set(Enemy,"_ai",AI);Set(Enemy,"_pendingAction",null);Mover.HoldMovementForAttack();
   Check((bool)Call(Enemy,"BeginAttackPattern",Pattern),"Controller begins real Pattern "+kind);
   Config.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Wait,Weight=1}};
  }
  public void Advance(float dt,bool route=true)
  {
   if(Enemy.IsAttackPatternActive)Runner.Tick(dt*Mathf.Max(0,Animator.speed));Animator.Update(dt);
   // Edit ModeではStateBehaviour/OnAnimatorMoveも明示通知して経路を検証する。
   var info=Animator.IsInTransition(0)?Animator.GetNextAnimatorStateInfo(0):Animator.GetCurrentAnimatorStateInfo(0);
   if(Enemy.IsAttackPatternActive && Runner.AcceptsState(info.fullPathHash))
   {
    if(!Enemy.IsAttackAnimationActive)Enemy.BeginAnimationAttackRootMotion(info.fullPathHash);
    if(((Vector3)Get(Mover,"_pendingAttackRootMotion")).sqrMagnitude<.00000001f)Call(Enemy,"OnAnimatorMove");
   }
   var delta=(Vector3)Get(Mover,"_pendingAttackRootMotion");RootOutput+=delta;Set(Mover,"_pendingAttackRootMotion",Vector3.zero);
   if(!route||!Enemy.IsAttackPatternActive||Runner.CurrentPhase!=EnemyAttackPatternRunner.Phase.Playing)return;
   var state=Animator.IsInTransition(0)?Animator.GetNextAnimatorStateInfo(0):Animator.GetCurrentAnimatorStateInfo(0);if(state.fullPathHash!=Pattern.StateHash)return;
   var controller=(AnimatorController)Animator.runtimeAnimatorController;var sm=controller.layers[0].stateMachine.stateMachines.Single(c=>c.stateMachine.name=="Attacks").stateMachine;
   var clip=(AnimationClip)sm.states.Single(s=>s.state.name==Pattern.PatternName).state.motion;
   foreach(var evt in AnimationUtility.GetAnimationEvents(clip))
   {
    string key=evt.functionName+"/"+evt.time;if(evt.time>state.normalizedTime*clip.length||!delivered.Add(key))continue;
    typeof(AnimationEvent).GetField("m_AnimatorStateInfo",F).SetValue(evt,state);var source=typeof(AnimationEvent).GetField("m_Source",F);source.SetValue(evt,Enum.Parse(source.FieldType,"Animator"));
    Go.SendMessage(evt.functionName,evt,SendMessageOptions.RequireReceiver);
   }
  }
  public void Dispose(){UnityEngine.Object.DestroyImmediate(Go);UnityEngine.Object.DestroyImmediate(Target);UnityEngine.Object.DestroyImmediate(Config);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 static void Integration(string kind)
 {
  using(var f=new Fixture(kind))
  {
   bool prior=false,recovery=false;int windows=0;
   for(int i=0;i<700;i++)
   {
    f.Advance(1f/120f);bool active=(bool)Get(f.Attacker,"_isHitboxActive");
    if(active&&!prior){windows++;Call(f.Attacker,"HandleWeaponHit",f.Target.GetComponent<Collider>(),f.Weapon);Call(f.Attacker,"HandleWeaponHit",f.Target.GetComponent<Collider>(),f.Weapon);}prior=active;
    if(f.Enemy.IsAttackPatternActive){if(Get(f.Enemy,"_pendingAction")!=null||!(bool)Get(f.Mover,"_isMovementHeldForAttack"))throw new Exception("AI/movement released mid-combo");recovery|=f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Recovery;}
   }
   int expected=kind=="Regular"?3:4;
   Check(windows==expected&&f.Hits==expected&&f.AllExpected,"Real clip events route "+expected+" strikes with dedup and correct strength "+kind+" damage="+f.Damage);
   Check(recovery&&!f.Enemy.IsAttackPatternActive&&Get(f.Enemy,"_pendingAction")!=null,"Final Recovery returns AI exactly after combo "+kind);
   Check(!(bool)Get(f.Attacker,"_isHitboxActive")&&!f.Mover.IsUsingAttackRootMotion&&Get(f.Mover,"_attackRootMotionScaleOverride")==null,"Completion clears Hitbox/root override "+kind);
   Check(f.RootOutput.z>0&&Math.Abs(f.RootOutput.y)<.0001,"Controller/Mover root output forward, horizontal "+kind+" "+f.RootOutput.ToString("F5"));
  }
  using(var f=new Fixture(kind))
  {
   for(int i=0;i<100&&!(bool)Get(f.Attacker,"_isHitboxActive");i++)f.Advance(1f/120f);
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true));Check(f.Enemy.IsAttackPatternActive,"Normal Heavy does not interrupt "+kind);
   int index=f.Runner.WindowIndex;f.Animator.speed=0;f.Advance(20);Check(f.Runner.IsActive&&f.Runner.WindowIndex==index,"HitStop freezes timer "+kind);f.Animator.speed=1;
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true,default,true,JustAvoidCounterStrike.First));
   Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy")&&!(bool)Get(f.Attacker,"_isHitboxActive")&&Get(f.Mover,"_attackRootMotionScaleOverride")==null,"JustAvoid counter clears combo, Hitbox, root and Busy "+kind);
   for(int i=0;i<400;i++)f.Advance(1f/60f);Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.Attacker,"_isHitboxActive"),"Cancelled clip cannot resume strikes "+kind);
  }
  using(var f=new Fixture(kind)){f.Advance(.2f);f.Enemy.ApplyDamage(new DamageInfo(100000,Vector3.zero,Vector3.forward,f.Target,null));Check((bool)Get(f.Enemy,"_dead")&&!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy"),"Death cancellation "+kind);}
  using(var f=new Fixture(kind)){f.Advance(.2f);Set(f.Enemy,"_game",f.Target.AddComponent<GameManager>());Call(f.Enemy,"Update");Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy"),"Combat end cancellation "+kind);}
  using(var f=new Fixture(kind)){f.Advance(.2f);Call(f.Enemy,"OnDisable");Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy")&&Get(f.Enemy,"_pendingAction")==null,"Disable cancellation "+kind);}
  using(var f=new Fixture(kind)){f.Advance(.2f);f.Animation.InterruptAttackForHitReaction();Check(!f.Enemy.IsAttackPatternActive&&(bool)Get(f.AI,"_isObserving"),"Forced interrupt schedules reconsideration "+kind);}
  using(var f=new Fixture(kind)){for(int i=0;i<1000;i++)f.Advance(1f/120f,false);Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy"),"Missing all events exits by watchdog "+kind);}
 }
}
