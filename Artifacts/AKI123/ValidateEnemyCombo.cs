using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ValidateEnemyCombo
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly StringBuilder Log=new StringBuilder();
 static void Check(bool value,string message) { if(!value) throw new Exception(message);Log.AppendLine("PASS "+message); }
 static object Get(object obj,string name)=>obj.GetType().GetField(name,Flags).GetValue(obj);
 static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Flags).SetValue(obj,value);
 static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode only");
  Log.Clear();
  try { ValidateAssets();ValidateRunner();ValidateAnimator();File.WriteAllText("Artifacts/AKI123/Validation.txt",Log.ToString()); }
  catch(Exception error) {File.WriteAllText("Artifacts/AKI123/Validation.txt",Log+"FAIL "+error);throw;}
 }
 static EnemyAttackPattern Pattern()=>AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/TwoHitCombo.asset");
 static void ValidateAssets()
 {
  var p=Pattern(); Check(p!=null && p.IsValid && p.AttackSteps.Length==2,"Serialized two-step Pattern");
  var c=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
  foreach(var step in p.AttackSteps)
  {
   Check(step.Attack!=null && step.Attack.Damage==10 && !step.Attack.IsHeavy,"Step data uses Light damage 10");
   var state=c.layers[0].stateMachine.states.Single(s=>"Base Layer."+s.state.name==step.AnimatorState).state;
   Check(state.speed==1 && state.tag=="EnemyCombo" && state.transitions.Length==0,"Pattern State speed/hold policy "+state.name);
   Check(state.behaviours.Any(b=>b is AttackRootMotionState),"Shared Root Motion behaviour "+state.name);
   var clip=(AnimationClip)state.motion; var events=AnimationUtility.GetAnimationEvents(clip);
   Check(clip.humanMotion && events.Length==4,"Humanoid clip and dedicated events "+clip.name);
   Check(events[0].functionName=="AnimEvent_ComboEnableWeaponHitbox" && events[2].functionName=="AnimEvent_ComboDisableWeaponHitbox" && events[3].functionName=="AnimEvent_ComboStepFinished","Hitbox/event order "+clip.name);
   Check(events.Last().time<clip.length,"Step finish before clip end "+clip.name+" length="+clip.length);
  }
  var config=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");
  Check(config.NearCandidates.Count(a=>a.Attack!=null)==4 && config.NearCandidates.Count(a=>a.Pattern==p)==1,"Four single attacks plus Pattern candidate");
  Check(Math.Abs(config.NearCandidates.Where(a=>a.Action==EnemyActionType.Slash).Sum(a=>a.Weight)-50)<.001,"Original attack weight total retained");
  var test=UnityEngine.Object.Instantiate(config);var invalid=ScriptableObject.CreateInstance<EnemyAttackPattern>();
  try
  {
   var maker=new EnemyDecisionMaker(new System.Random(123));
   test.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Attack=p.AttackSteps[0].Attack,Pattern=p,Weight=1}};
   var result=maker.DecideAction(1,new EnemyDecision(EnemyActionType.Wait),test);
   Check(result.Pattern==p && result.Attack==null,"Pattern wins over simultaneous AttackData");
   test.RepeatPenalty=0;test.RepeatPenaltyPerAttack=true;
   Check(maker.DecideAction(1,new EnemyDecision(EnemyActionType.Slash,null,p),test).Action==EnemyActionType.Wait,"RepeatPenalty identifies same Pattern");
   test.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=invalid,Weight=100},new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Wait,Weight=1}};
   Check(maker.DecideAction(1,new EnemyDecision(EnemyActionType.Slash),test).Action==EnemyActionType.Wait,"Invalid Pattern excluded from decision");
   foreach(var single in config.NearCandidates.Where(a=>a.Attack!=null))
   {
    test.NearCandidates=new[]{single};test.RepeatPenalty=1;
    Check(maker.DecideAction(1,new EnemyDecision(EnemyActionType.Wait),test).Attack==single.Attack,"Single attack still individually selectable "+single.Attack.AnimatorTrigger);
   }
  }
  finally {UnityEngine.Object.DestroyImmediate(test);UnityEngine.Object.DestroyImmediate(invalid);}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");var so=new SerializedObject(prefab.GetComponent<EnemyController>());
  Check(so.FindProperty("_decisionConfig").objectReferenceValue==config && so.FindProperty("_attackData").arraySize==4,"Prefab references preserved, tutorial registration still four attacks");
 }
 static void ValidateRunner()
 {
  var p=Pattern();int starts=0,finish=0,closed=0;bool success=false;
  var r=new EnemyAttackPatternRunner(s=>{starts++;return true;},()=>closed++,ok=>{finish++;success=ok;});
  Check(r.Begin(p),"Runner begins valid Pattern");r.Tick(p.Windup+.001f);
  Check(starts==1 && r.CurrentPhase==EnemyAttackPatternRunner.Phase.Playing,"Windup starts only step one");
  Check(!r.EndStep(2,p.AttackSteps[1].StateHash),"Out-of-order event ignored");
  Check(r.EndStep(1,p.AttackSteps[0].StateHash) && finish==0,"First finish does not complete Pattern");
  Check(!r.EndStep(1,p.AttackSteps[0].StateHash),"Duplicate step-one finish ignored");
  r.Tick(p.ComboInterval+.001f);Check(starts==2,"Interval starts step two");
  Check(!r.AcceptsEvent(1,p.AttackSteps[0].StateHash),"Outgoing step events rejected during next step");
  r.EndStep(2,p.AttackSteps[1].StateHash);Check(finish==0 && r.IsActive,"Final hit enters Recovery while still busy");
  r.Tick(p.RecoveryDuration/2);Check(finish==0,"Recovery cannot end early");r.Tick(p.RecoveryDuration);
  Check(finish==1 && success && !r.IsActive,"Recovery completes exactly once");r.Tick(10);r.EndStep(2,p.AttackSteps[1].StateHash);Check(finish==1,"Late finish cannot complete twice");
  r.Begin(p);r.Tick(p.Windup+.001f);r.Cancel();int before=starts;r.Tick(10);
  Check(starts==before && !r.IsActive && !r.AcceptsEvent(1,p.AttackSteps[0].StateHash),"Cancel clears next step and rejects stale events");
  var failed=new EnemyAttackPatternRunner(s=>false,()=>{},ok=>success=ok);success=true;failed.Begin(p);failed.Tick(1);
  Check(!failed.IsActive && !success,"Failed step start exits without Busy lock");
  r.Begin(p);r.Tick(p.Windup+.001f);r.Tick(4);Check(!r.IsActive && !success,"Missing finish event times out safely");
 }
 sealed class Fixture : IDisposable
 {
  public GameObject Go,Target;
  public EnemyController Enemy;
  public Animator Animator;
  public EnemyAnimationController Animation;
  public EnemyAttacker Attacker;
  public EnemyMover Mover;
  public EnemyAI AI;
  public EnemyAttackPatternRunner Runner=>(EnemyAttackPatternRunner)Get(Enemy,"_patternRunner");
  public EnemyWeapon Weapon;
  public EnemyDecisionConfig Config;
  public int Hits;
  public bool AllLight=true;
  readonly System.Collections.Generic.HashSet<string> Delivered = new System.Collections.Generic.HashSet<string>();
  readonly UnityEngine.SceneManagement.Scene Scene;
  public Fixture()
  {
   Scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
   Go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(Go,Scene);
   Target=new GameObject("combo validation target");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(Target,Scene);Target.transform.position=Vector3.forward;
   var victim=Target.AddComponent<EnemyController>();Set(victim,"_health",new EnemyHealth(null));
   Target.AddComponent<BoxCollider>();victim.HitReceived+=info=>{Hits++;AllLight &= !info.IsHeavy;};
   Enemy=Go.GetComponent<EnemyController>();Animator=Go.GetComponent<Animator>();Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.fireEvents=true;Animator.Rebind();Animator.Update(0);
   Animation=Go.GetComponent<EnemyAnimationController>();Animation.Init();
   var status=(EnemyStuts)Get(Enemy,"_enemyStuts");
   Weapon=new EnemyWeapon((Collider[])Get(Enemy,"_enemyWeaponColliders"),10);Weapon.Init();
   Attacker=new EnemyAttacker(Animation,(EnemyAttackData[])Get(Enemy,"_attackData"),new[]{Weapon},status,Go.transform);
   Mover=new EnemyMover(status,Go.transform,Target.transform,Animation,null,null,Animator,(AnimationName)Get(Enemy,"_animName"),.18f);
   Set(Enemy,"_enemyAnimController",Animation);Set(Enemy,"_attacker",Attacker);Set(Enemy,"_mover",Mover);Set(Enemy,"_health",new EnemyHealth(status));
   Config=ScriptableObject.CreateInstance<EnemyDecisionConfig>();Config.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=Pattern(),Weight=1}};Config.RepeatPenalty=1;
   AI=new EnemyAI(Enemy,Target.transform,null,Config);Set(Enemy,"_ai",AI);Set(Enemy,"_pendingAction",null);
   Mover.HoldMovementForAttack();Check((bool)Call(Enemy,"BeginAttackPattern",Pattern()),"Controller starts Pattern with real Animator/weapon");
   Config.NearCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Wait,Weight=1}};
  }
  public void Advance(float dt)
  {
   if(Enemy.IsAttackPatternActive)Runner.Tick(dt*Mathf.Max(0,Animator.speed));
   Animator.Update(dt);
   // Edit Modeはイベントを自動通知しないため、実State時刻で実ClipのEventをルーティングする。
   if(Enemy.IsAttackPatternActive && Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Playing)
   {
    var state=Animator.IsInTransition(0)?Animator.GetNextAnimatorStateInfo(0):Animator.GetCurrentAnimatorStateInfo(0);
    var step=Runner.Pattern.AttackSteps[Runner.StepIndex];
    if(state.fullPathHash==step.StateHash)
    {
     var c=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
     var clip=(AnimationClip)c.layers[0].stateMachine.states.Single(s=>"Base Layer."+s.state.name==step.AnimatorState).state.motion;
     foreach(var evt in AnimationUtility.GetAnimationEvents(clip))
     {
      string key=step.StateHash+"/"+evt.functionName+"/"+evt.time;
      if(evt.time>state.normalizedTime*clip.length || !Delivered.Add(key))continue;
      typeof(AnimationEvent).GetField("m_AnimatorStateInfo",Flags).SetValue(evt,state);
      var source=typeof(AnimationEvent).GetField("m_Source",Flags);source.SetValue(evt,Enum.Parse(source.FieldType,"Animator"));
      Go.SendMessage(evt.functionName,evt,SendMessageOptions.RequireReceiver);
     }
    }
   }
  }
  public void Dispose()
  {
   UnityEngine.Object.DestroyImmediate(Go);UnityEngine.Object.DestroyImmediate(Target);UnityEngine.Object.DestroyImmediate(Config);
   UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(Scene);
  }
 }
 static void ValidateAnimator()
 {
  using(var f=new Fixture())
  {
   bool prior=false;int windows=0;bool interval=false,recovery=false;float first=-1,second=-1;
   for(int i=0;i<300;i++)
   {
    f.Advance(1f/120f);

    bool active=(bool)Get(f.Attacker,"_isHitboxActive");
    if(active&&!prior)
    {
     windows++;if(windows==1)first=i/120f;else second=i/120f;
     var collider=f.Target.GetComponent<Collider>();
     Call(f.Attacker,"HandleWeaponHit",collider,f.Weapon);Call(f.Attacker,"HandleWeaponHit",collider,f.Weapon);
    }
    prior=active;
    if(f.Enemy.IsAttackPatternActive)
    {
     if(Get(f.Enemy,"_pendingAction")!=null || !(bool)Get(f.Mover,"_isMovementHeldForAttack")) throw new Exception("AI or movement released during Pattern");
     interval|=f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Interval;
     recovery|=f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Recovery;
    }
   }
   Check(windows==2,"Real Clip events routed at Animator times open two Hitbox windows: first="+first+" second="+second);
   Check(f.Hits==2 && f.AllLight,"Same target hit once per step, both Light; duplicate contacts suppressed");
   Check(interval&&recovery,"Animator sequence includes Interval and Recovery");
   Check(!f.Enemy.IsAttackPatternActive && Get(f.Enemy,"_pendingAction")!=null,"Only final Recovery returns control to AI");
   Check(!(bool)Get(f.Attacker,"_isHitboxActive") && !(bool)Get(f.Mover,"_isMovementHeldForAttack"),"Final finish clears Hitbox and movement hold");
  }
  using(var f=new Fixture())
  {
   f.Advance(.2f);float before=f.Runner.StepIndex;f.Animator.speed=0;f.Advance(10);
   Check(f.Runner.IsActive && f.Runner.StepIndex==before,"Animator freeze does not consume combo timers");f.Animator.speed=1;
   for(int i=0;i<120 && !(bool)Get(f.Attacker,"_isHitboxActive");i++)f.Advance(1f/120f);
   Check((bool)Get(f.Attacker,"_isHitboxActive"),"Counter test reaches first active hit window");
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null));
   Check(f.Enemy.IsAttackPatternActive,"Normal Light hit preserves existing interruption policy");
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true));
   Check(f.Enemy.IsAttackPatternActive,"Normal Heavy hit preserves existing interruption policy");
   f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true,default,true,JustAvoidCounterStrike.First));
   Check(!f.Enemy.IsAttackPatternActive && !(bool)Get(f.Attacker,"_isHitboxActive"),"Counter hit immediately cancels Pattern and Hitbox");
   Check(!(bool)Get(f.AI,"_isBusy") && Get(f.Enemy,"_pendingAction")==null,"Counter clears Busy and queued next action");
   for(int i=0;i<200;i++)f.Advance(1f/60f);
   Check(!f.Enemy.IsAttackPatternActive && !(bool)Get(f.Attacker,"_isHitboxActive"),"Cancelled animation cannot restart second strike");
  }
  using(var f=new Fixture())
  {
   f.Advance(.3f);f.Enemy.ApplyDamage(new DamageInfo(100000,Vector3.zero,Vector3.forward,f.Target,null));
   Check((bool)Get(f.Enemy,"_dead")&&!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy"),"Death cancels Pattern and AI Busy");
  }
  using(var f=new Fixture())
  {
   f.Advance(.2f);
   int first=Pattern().AttackSteps[0].StateHash,second=Pattern().AttackSteps[1].StateHash;
   f.Enemy.BeginAnimationAttackRootMotion(first);f.Enemy.EndAnimationAttackRootMotion(first);
   Check((bool)Get(f.Mover,"_isMovementHeldForAttack") && !f.Mover.IsUsingAttackRootMotion,"Outgoing root owner ends without resuming movement during Pattern");
   f.Runner.EndStep(1,first);f.Runner.Tick(Pattern().ComboInterval+.001f);
   f.Enemy.BeginAnimationAttackRootMotion(second);f.Enemy.EndAnimationAttackRootMotion(first);
   Check((int)Get(f.Enemy,"_attackAnimationHash")==second && f.Mover.IsUsingAttackRootMotion,"Late outgoing StateExit cannot clear incoming root owner");
   f.Enemy.CancelAttackPattern(false);
   Check(!f.Mover.IsUsingAttackRootMotion && !(bool)Get(f.Mover,"_isMovementHeldForAttack"),"Cancel releases root motion and movement hold");
  }
  using(var f=new Fixture())
  {
   f.Advance(.2f);var game=f.Target.AddComponent<GameManager>();Set(f.Enemy,"_game",game);Call(f.Enemy,"Update");
   Check(!f.Enemy.IsAttackPatternActive && !(bool)Get(f.AI,"_isBusy") && Get(f.Enemy,"_pendingAction")==null,"Combat end clears Pattern, Busy and pending action");
  }
  using(var f=new Fixture())
  {
   for(int i=0;i<120 && f.Runner.CurrentPhase!=EnemyAttackPatternRunner.Phase.Interval;i++)f.Advance(1f/120f);
   Check(f.Runner.CurrentPhase==EnemyAttackPatternRunner.Phase.Interval,"Invalid Animator test reaches interval");
   f.Animator.enabled=false;f.Advance(Pattern().ComboInterval+.01f);
   Check(!f.Enemy.IsAttackPatternActive && !(bool)Get(f.AI,"_isBusy"),"Invalid Animator before next step aborts Pattern safely");
  }
  using(var f=new Fixture())
  {
   f.Advance(.2f);f.Animation.InterruptAttackForHitReaction();
   Check(!f.Enemy.IsAttackPatternActive && Get(f.Enemy,"_pendingAction")==null,"Existing forced interrupt clears Pattern reservation");
   Check((bool)Get(f.AI,"_isObserving"),"Forced interrupt schedules safe AI reconsideration");
  }
 }
}
