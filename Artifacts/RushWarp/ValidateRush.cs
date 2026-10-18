using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ValidateRush
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static StringBuilder log=new StringBuilder();
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);log.AppendLine("PASS "+text);}
 static EnemyAttackPattern P(string kind)=>AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/"+kind+"Attack.asset");
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  log.Clear();try{foreach(string kind in new[]{"Rush"}){Runner(kind);Integration(kind);WindupCancel(kind);}File.WriteAllText("Artifacts/RushWarp/RushValidation.txt",log.ToString());}
  catch(Exception e){File.WriteAllText("Artifacts/RushWarp/RushValidation.txt",log+"FAIL "+e);throw;}
 }
 static void Assets()
 {
  var config=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");
  Check(config.NearCandidates.Where(c=>c.Attack!=null).Select(c=>c.Attack).Distinct().Count()==4 && config.NearCandidates.Where(c=>c.Pattern!=null).Select(c=>c.Pattern).Distinct().Count()==2,"Four singles plus two independent combos (user duplicate weighted rows preserved)");
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
   Check(Math.Abs(pattern.PlaybackSpeed-.85f)<.0001f && Math.Abs(pattern.WindupDuration-(kind=="Regular"?.35f:.5f))<.0001f,"Requested speed and windup initial values "+kind);
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
   var sm=controller.layers[0].stateMachine.stateMachines.Single(c=>c.stateMachine.name=="Attacks").stateMachine;
   var state=sm.states.Single(s=>s.state.name==kind+"Combo").state;var clip=(AnimationClip)state.motion;
   Check(clip.humanMotion&&clip.hasRootCurves,"Humanoid and root curves "+kind);
   Check(state.tag=="EnemyCombo"&&state.transitions.Length==0&&state.behaviours.Any(b=>b is AttackRootMotionState),"Attack root ownership and no early automatic exit "+kind);
   var settings=AnimationUtility.GetAnimationClipSettings(clip);
   Check(settings.loopBlendOrientation&&settings.keepOriginalOrientation&&Math.Abs(settings.orientationOffsetY-180)<.01&&settings.loopBlendPositionY&&!settings.loopBlendPositionXZ,"Root Transform settings "+kind);
   var events=AnimationUtility.GetAnimationEvents(clip);int count=kind=="Rush"?1:(kind=="Regular"?3:4);
   Check(pattern.WindupEndTime<events.Where(e=>e.functionName=="AnimEvent_ComboEnableWeaponHitbox").Min(e=>e.time),"Windup ends before any damage event "+kind);
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
  Check(r.Begin(p),"Runner begins "+kind);r.Tick(.01f);
  Check(starts==1 && !r.OpenWindow(1,p.StateHash),"Clip starts immediately; windup hitbox rejected "+kind);
  r.ObserveClipTime(p.WindupEndTime-.01f,p.StateHash);Check(r.CurrentPhase==EnemyAttackPatternRunner.Phase.Windup,"Windup waits for actual clip progress "+kind);
  r.ObserveClipTime(p.WindupEndTime,p.StateHash);
  Check(r.OpenWindow(1,p.StateHash)&&!r.OpenWindow(1,p.StateHash),"Duplicate ON does not reset target hits "+kind);
  Check(!r.CloseWindow(2,p.StateHash)&&r.CloseWindow(1,p.StateHash),"Wrong window OFF rejected "+kind);
  Check(!r.OpenWindow(1,p.StateHash)&&r.OpenWindow(2,p.StateHash),"Only next strike can open "+kind);
  Check(!r.EndClip(p.StateHash+1)&&r.EndClip(p.StateHash),"Finish validates state "+kind);
  Check(finishes==0&&r.IsActive,"Recovery still busy "+kind);r.Tick(p.RecoveryDuration+.01f);
  Check(finishes==1&&success&&!r.IsActive&&starts==1,"One clip and one final completion "+kind);
  r.Tick(20);Check(!r.EndClip(p.StateHash)&&finishes==1,"Late completion ignored "+kind);
  r.Begin(p);r.Tick(p.WindupDuration+.01f);r.Cancel();r.Tick(100);Check(!r.IsActive&&!r.OpenWindow(1,p.StateHash),"Cancellation rejects all stale events "+kind);
  r.Begin(p);r.Tick(.01f);r.ObserveClipTime(p.WindupEndTime,p.StateHash);r.Tick(p.Timeout/p.PlaybackSpeed+.1f);Check(!r.IsActive&&!success,"Missing finish fails safely "+kind);
  r.Begin(p);r.Tick(.01f);r.Tick(p.WindupDuration+1.01f);Check(!r.IsActive&&!success,"Stalled windup fails safely "+kind);
 }
 sealed class Fixture:IDisposable
 {
  public GameObject Go,Target;public EnemyController Enemy;public Animator Animator;public EnemyAnimationController Animation;public EnemyAttacker Attacker;public EnemyWeapon Weapon;public EnemyMover Mover;public EnemyAI AI;public EnemyDecisionConfig Config;public EnemyAttackPattern Pattern;
  public EnemyAttackPatternRunner Runner=>(EnemyAttackPatternRunner)Get(Enemy,"_patternRunner");
  public int Hits;public float Damage;public bool AllExpected=true;public Vector3 RootOutput;
  readonly UnityEngine.SceneManagement.Scene scene;readonly HashSet<string> delivered=new HashSet<string>();
  bool rootEntered;
  public Fixture(string kind, float? speed=null, float? duration=null)
  {
   Pattern=UnityEngine.Object.Instantiate(P(kind));if(speed.HasValue)Pattern.PlaybackSpeed=speed.Value;if(duration.HasValue)Pattern.WindupDuration=duration.Value;scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
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
   if(Enemy.IsAttackPatternActive){Call(Enemy,"ObserveAttackPatternAnimation");Runner.Tick(dt*Mathf.Max(0,Animator.speed));}Animator.Update(dt);
   // Edit ModeではStateBehaviour/OnAnimatorMoveも明示通知して経路を検証する。
   var info=Animator.IsInTransition(0)?Animator.GetNextAnimatorStateInfo(0):Animator.GetCurrentAnimatorStateInfo(0);
   if(Enemy.IsAttackPatternActive && Runner.OwnsAnimationState(info.fullPathHash))
   {
    if(!rootEntered){Enemy.BeginAnimationAttackRootMotion(info.fullPathHash);rootEntered=true;}
    if(((Vector3)Get(Mover,"_pendingAttackRootMotion")).sqrMagnitude<.00000001f)Call(Enemy,"OnAnimatorMove");
   }
   var delta=(Vector3)Get(Mover,"_pendingAttackRootMotion");RootOutput+=delta;Set(Mover,"_pendingAttackRootMotion",Vector3.zero);
   if(!route||!Enemy.IsAttackPatternActive)return;
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
  public void Dispose(){UnityEngine.Object.DestroyImmediate(Go);UnityEngine.Object.DestroyImmediate(Target);UnityEngine.Object.DestroyImmediate(Config);UnityEngine.Object.DestroyImmediate(Pattern);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 static void Integration(string kind)
 {
  using(var f=new Fixture(kind))
  {
   bool prior=false,recovery=false;int windows=0;
   for(int i=0;i<1200;i++)
   {
    f.Advance(1f/120f);bool active=(bool)Get(f.Attacker,"_isHitboxActive");
    if(active&&!prior){windows++;Call(f.Attacker,"HandleWeaponHit",f.Target.GetComponent<Collider>(),f.Weapon);Call(f.Attacker,"HandleWeaponHit",f.Target.GetComponent<Collider>(),f.Weapon);}prior=active;
    if(f.Enemy.IsAttackPatternActive){if(Get(f.Enemy,"_pendingAction")!=null||!(bool)Get(f.Mover,"_isMovementHeldForAttack"))throw new Exception("AI/movement released mid-combo");recovery|=f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Recovery;}
   }
   int expected=kind=="Rush"?1:(kind=="Regular"?3:4);
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
  using(var f=new Fixture(kind)){for(int i=0;i<1600;i++)f.Advance(1f/120f,false);Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy"),"Missing all events exits by watchdog "+kind);}
 }
 static void Timings(string kind)
 {
  var roots=new List<float>();
  foreach(float speed in new[]{1f,.85f}) foreach(float duration in new[]{0f,.2f,.35f,.5f,.8f})
  using(var f=new Fixture(kind,speed,duration))
  {
   const float dt=1f/240;float time=0,windupEnd=duration==0?0:-1,first=-1,finish=-1,recoveryStart=-1;bool windupSafe=true;int windows=0;bool prior=false;
   for(int i=0;i<3000 && finish<0;i++)
   {
    f.Advance(dt);time+=dt;
    bool active=(bool)Get(f.Attacker,"_isHitboxActive");
    windupSafe &= f.Runner.CurrentPhase!=EnemyAttackPatternRunner.Phase.Windup || !active;
    if(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Playing && windupEnd<0)windupEnd=time;
    if(active&&!prior){windows++;if(first<0)first=time;}prior=active;
    if(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Recovery && recoveryStart<0)recoveryStart=time;
    if(!f.Runner.IsActive)finish=time;
   }
   float firstClip=kind=="Regular"?.17f:.28f;float prefix=duration==0?0:f.Pattern.WindupEndTime;
   float expectedFirst=duration+(firstClip-prefix)/speed;
   Check(windupSafe && Math.Abs(windupEnd-duration)<.025f,"Native windup duration "+kind+" duration="+duration+" speed="+speed+" measured="+windupEnd);
   Check(windows==(kind=="Rush"?1:(kind=="Regular"?3:4))&&Math.Abs(first-expectedFirst)<.025f,"Clip-time events scale naturally "+kind+" first="+first+" expected="+expectedFirst);
   Check(finish>0 && Math.Abs((finish-recoveryStart)-f.Pattern.RecoveryDuration)<.015f,"Recovery independent of clip playback speed "+kind);
   Check(Math.Abs(f.Animator.speed-1)<.0001f,"Animator global speed retained "+kind);
   Check(f.RootOutput.z>.35f && f.RootOutput.z<.85f && Math.Abs(f.RootOutput.y)<.0001f,"Transition-inclusive horizontal root output "+kind+" speed="+speed+" windup="+duration+" root="+f.RootOutput.ToString("F5"));roots.Add(f.RootOutput.z);
  }
  log.AppendLine("INFO Transition-inclusive root range "+kind+"="+roots.Min()+".."+roots.Max()+" (initial AnyState blend weights affect displacement)");
  NativeRoot(kind);
  using(var f=new Fixture(kind))
  {
   for(int i=0;i<4;i++)f.Advance(1f/120);
   var before=f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;var elapsed=(float)Get(f.Runner,"_elapsed");
   f.Animator.speed=0;f.Advance(20);
   Check(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Windup && Math.Abs((float)Get(f.Runner,"_elapsed")-elapsed)<.0001f && Math.Abs(f.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime-before)<.0001f,"HitStop freezes windup timer and native pose "+kind);f.Animator.speed=1;
   for(int i=0;i<1200&&f.Runner.CurrentPhase!=EnemyAttackPatternRunner.Phase.Recovery;i++)f.Advance(1f/120);
   elapsed=(float)Get(f.Runner,"_elapsed");f.Animator.speed=0;f.Advance(20);
   Check(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Recovery && Math.Abs((float)Get(f.Runner,"_elapsed")-elapsed)<.0001f,"HitStop freezes recovery "+kind);
  }
  using(var f=new Fixture(kind))
  {
   int windows=0;bool prior=false;
   for(int i=0;i<160;i++){f.Advance(.1f);bool active=(bool)Get(f.Attacker,"_isHitboxActive");if(active&&!prior)windows++;prior=active;}
   Check(windows==(kind=="Rush"?1:(kind=="Regular"?3:4))&&!f.Runner.IsActive,"Low FPS crossing windup boundary retains first strike "+kind);
  }
 }
 static void NativeRoot(string kind)
 {
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"),scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
  var a=go.GetComponent<Animator>();var c=new AnimatorController();c.AddLayer("Base Layer");var state=c.layers[0].stateMachine.AddState("Probe");c.layers[0].stateMachine.defaultState=state;state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2"+kind+"Combo.anim");state.speedParameter="speed";state.speedParameterActive=true;c.AddParameter("speed",AnimatorControllerParameterType.Float);a.runtimeAnimatorController=c;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  Vector3 reference=Vector3.zero;
  try
  {
   foreach(float duration in new[]{0f,.35f,.5f,.8f})
   {
    var p=P(kind);a.Rebind();a.SetFloat("speed",duration==0?1:p.WindupEndTime/duration);a.Play("Base Layer.Probe",0,0);a.Update(0);Vector3 sum=Vector3.zero;
    const float dt=1f/240;var clip=(AnimationClip)state.motion;
    for(int i=0;i<2500;i++)
    {
     float time=a.GetCurrentAnimatorStateInfo(0).normalizedTime*clip.length;
     if(time>=clip.length-.0001f)break;
     if(duration>0 && time>=p.WindupEndTime)a.SetFloat("speed",.85f);
     float step=Mathf.Min(dt,(clip.length-time)/a.GetFloat("speed"));a.Update(step);sum+=a.deltaPosition;
    }
    if(duration==0)reference=sum;
    Check(Vector3.Distance(reference,sum)<.015f && Math.Abs(sum.y)<.0001f && sum.z>0,"Native full-clip root conserved with speed change "+kind+" duration="+duration+" sum="+sum.ToString("F5"));
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(c);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
 static void WindupCancel(string kind)
 {
  using(var f=new Fixture(kind))
  {
   f.Advance(.02f);f.Advance(.02f);Check(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Windup,"Counter test enters visible windup "+kind);
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true,default,true,JustAvoidCounterStrike.First));
   Check(!f.Enemy.IsAttackPatternActive && !(bool)Get(f.Attacker,"_isHitboxActive") && !f.Mover.IsUsingAttackRootMotion && Get(f.Mover,"_attackRootMotionScaleOverride")==null,"JustAvoid cancels visible windup "+kind);
  }
 }
}
