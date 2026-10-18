using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
public static class ValidateSafety
{
 static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;static StringBuilder log=new();
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
 static void Check(bool ok,string s){if(!ok)throw new Exception(s);log.AppendLine("PASS "+s);}
 static EnemyAttackPattern P(string name)=>AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/"+name+"Attack.asset");
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  log.Clear();try{Assets();Selection();using(var world=new World()){Destinations(world);Motion(world);Warp(world);}File.WriteAllText("Artifacts/RushWarp/SafetyValidation.txt",log.ToString());}catch(Exception e){File.WriteAllText("Artifacts/RushWarp/SafetyValidation.txt",log+"FAIL "+e);throw;}
 }
 static void Assets()
 {
  var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");var sm=ctrl.layers[0].stateMachine.stateMachines.Single(s=>s.stateMachine.name=="Attacks").stateMachine;
  foreach(string name in new[]{"Rush","Warp"})
  {
   var p=P(name);var state=sm.states.Single(s=>s.state.name==name+"Attack").state;var clip=(AnimationClip)state.motion;var e=AnimationUtility.GetAnimationEvents(clip);
   Check(p.IsValid && p.Attack.ActionType==(name=="Rush"?EnemyActionType.RushAttack:EnemyActionType.WarpAttack),name+" valid independent Pattern and AttackData");
   Check(clip.humanMotion&&clip.hasRootCurves && state.behaviours.Any(b=>b is AttackRootMotionState)&&state.speedParameterActive&&state.speedParameter=="EnemyComboPlaybackSpeed",name+" Humanoid/root/state playback ownership");
   Check(e.Count(x=>x.functionName=="AnimEvent_ComboEnableWeaponHitbox")==1 && e.Count(x=>x.functionName=="AnimEvent_ComboDisableWeaponHitbox")==1&&e.Count(x=>x.functionName=="AnimEvent_ComboFinished")==1,name+" one strike and one completion");
   Check(e.Single(x=>x.functionName=="AnimEvent_ComboEnableWeaponHitbox").time>p.WindupEndTime,name+" windup precedes damage window");
   Check(ctrl.layers[0].stateMachine.anyStateTransitions.Any(t=>t.destinationState==state&&!t.canTransitionToSelf&&t.conditions.Any(c=>c.parameter==name+"Attack")),name+" independent trigger transition");
   var c=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/EnemyDecisionConfig_"+name+"AttackTest.asset");Check(c.NearCandidates.Single().Pattern==p,name+" isolated AI test config");
  }
  var tell=sm.states.Single(s=>s.state.name=="WarpDeparture").state;Check(tell.behaviours.Length==0&&AnimationUtility.GetAnimationEvents((AnimationClip)tell.motion).Length==0,"Warp departure has no hit/root callbacks");
  Check(P("Warp").WarpEffectPrefab!=null && P("Warp").DisappearSound!=null&&P("Warp").ReappearSound!=null,"Warp existing smoke and SE references resolve");
 }
 static void Selection()
 {
  var c=ScriptableObject.CreateInstance<EnemyDecisionConfig>();var row=new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.RushAttack,Pattern=P("Rush"),Weight=3};var wait=new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Wait,Weight=1};c.NearCandidates=c.MidCandidates=c.FarCandidates=new[]{row};c.RepeatPenalty=1;var m=new EnemyDecisionMaker(new System.Random(452));
  Check(m.DecideAction(4,default,c).Pattern==P("Rush"),"Rush eligible at 4m");Check(m.DecideAction(1,default,c).Action==EnemyActionType.Wait&&m.DecideAction(6,default,c).Action==EnemyActionType.Wait,"Rush excluded before draw outside min/max");
  c.NearCandidates=c.MidCandidates=c.FarCandidates=new[]{row,wait};Check(m.DecideAction(4,default,c,x=>x.Pattern==null).Action==EnemyActionType.Wait,"Cooldown/unavailable predicate filters before draw");
  int n=0;for(int i=0;i<4000;i++)if(m.DecideAction(4,default,c).Pattern==P("Rush"))n++;Check(n>2850&&n<3150,"Selection weights respected: Rush="+n+"/4000 (3:1)");
  c.RepeatPenalty=0;c.RepeatPenaltyPerAttack=true;Check(m.DecideAction(4,new EnemyDecision(EnemyActionType.RushAttack,null,P("Rush")),c).Action==EnemyActionType.Wait,"RepeatPenalty retained for rush");UnityEngine.Object.DestroyImmediate(c);
 }
 sealed class World:IDisposable
 {
  public Vector3 Origin=new(10000,0,10000);public UnityEngine.SceneManagement.Scene Scene;public NavMeshData Data;NavMeshDataInstance instance;UnityEngine.SceneManagement.Scene original;
  public World(){original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);var settings=NavMesh.GetSettingsByID(0);Data=NavMeshBuilder.BuildNavMeshData(settings,new List<NavMeshBuildSource>{new(){shape=NavMeshBuildSourceShape.Box,size=new Vector3(30,.2f,30),transform=Matrix4x4.TRS(Origin-Vector3.up*.1f,Quaternion.identity,Vector3.one),area=0}},new Bounds(Origin,new Vector3(40,10,40)),Vector3.zero,Quaternion.identity);if(Data==null)throw new Exception("No temp navmesh");instance=NavMesh.AddNavMeshData(Data);var floor=Object("floor",Origin-Vector3.up*.1f);floor.AddComponent<BoxCollider>().size=new Vector3(30,.2f,30);Physics.SyncTransforms();}
  public GameObject Object(string name,Vector3 position){var o=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(o,Scene);o.transform.position=position;return o;}
  public void Dispose(){instance.Remove();UnityEngine.Object.DestroyImmediate(Data);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(Scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);}
 }
 sealed class Fixture:IDisposable
 {
  public GameObject Go,Target;public EnemyController Enemy;public EnemyAttackPattern Pattern;public EnemyMover Mover;public Animator Animator;public EnemyAnimationController Animation;public EnemyAttacker Attacker;public EnemyWeapon Weapon;public NavMeshAgent Agent;public CapsuleCollider Body;public Rigidbody RB;public EnemyAI AI;public EnemyDecisionConfig Config;public EnemyAttackPatternRunner Runner=>(EnemyAttackPatternRunner)Get(Enemy,"_patternRunner");public Vector3 Start;public int Hits;HashSet<string> delivered=new();bool rootEntered;
  public Fixture(World w,bool begin=true)
  {
   Pattern=UnityEngine.Object.Instantiate(P("Warp"));Pattern.WarpEffectPrefab=null;Pattern.DisappearSound=null;Pattern.ReappearSound=null;
   Go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"),w.Scene);Go.transform.SetPositionAndRotation(w.Origin+Vector3.forward*4,Quaternion.identity);Agent=Go.GetComponent<NavMeshAgent>();Agent.Warp(Go.transform.position);RB=Go.GetComponent<Rigidbody>();RB.isKinematic=true;RB.position=Go.transform.position;Start=RB.position;Body=Go.GetComponent<CapsuleCollider>();
   Target=w.Object("target",w.Origin);var capsule=Target.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.3f;capsule.center=Vector3.up*.9f;var victim=Target.AddComponent<EnemyController>();Set(victim,"_health",new EnemyHealth(null));victim.HitReceived+=info=>Hits++;
   Enemy=Go.GetComponent<EnemyController>();Animator=Go.GetComponent<Animator>();Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Animator.Rebind();Animator.Update(0);Animation=Go.GetComponent<EnemyAnimationController>();Animation.Init();var status=(EnemyStuts)Get(Enemy,"_enemyStuts");Weapon=new EnemyWeapon((Collider[])Get(Enemy,"_enemyWeaponColliders"),10);Weapon.Init();Attacker=new EnemyAttacker(Animation,(EnemyAttackData[])Get(Enemy,"_attackData"),new[]{Weapon},status,Go.transform);Mover=new EnemyMover(status,Go.transform,Target.transform,Animation,RB,Agent,Animator,(AnimationName)Get(Enemy,"_animName"),.18f);Set(Enemy,"_mover",Mover);Set(Enemy,"_attacker",Attacker);Set(Enemy,"_enemyAnimController",Animation);Set(Enemy,"_health",new EnemyHealth(status));Set(Enemy,"_attackTarget",Target.transform);
   Config=ScriptableObject.CreateInstance<EnemyDecisionConfig>();var row=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.WarpAttack,Pattern=Pattern,Weight=1}};Config.NearCandidates=Config.MidCandidates=Config.FarCandidates=row;Config.RepeatPenalty=1;Physics.SyncTransforms();AI=new EnemyAI(Enemy,Target.transform,Agent,Config);Set(Enemy,"_ai",AI);Set(Enemy,"_pendingAction",null);Config.NearCandidates=Config.MidCandidates=Config.FarCandidates=new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Wait,Weight=1}};
   if(begin){Mover.HoldMovementForAttack();Check((bool)Call(Enemy,"BeginAttackPattern",Pattern),"Warp begins with a valid destination");}
  }
  public void Advance(float dt,bool events=true)
  {
   // Edit ModeではAgentが登録されない。安全位置検証は実処理、成功後の境界だけ明示通知する。
   if((bool)Get(Enemy,"_warpHidden") && Animator.speed>0 && (float)Get(Enemy,"_warpElapsed")+dt>=Pattern.DisappearDuration && EnemyWarpDestination.TryFind(Target.transform,Body,Agent,Pattern,out var arrival))
   {RB.position=arrival;Go.transform.position=arrival;Call(Enemy,"CompleteWarpReappearance",Pattern);log.AppendLine("INFO Explicit arrival boundary in Edit Mode; native Agent.Warp success unverified");}
   else if(Enemy.IsAttackPatternActive)Call(Enemy,"TickAttackPattern",dt*Mathf.Max(0,Animator.speed));Animator.Update(dt);
   if(!Enemy.IsAttackPatternActive||Runner==null)return;var info=Animator.IsInTransition(0)?Animator.GetNextAnimatorStateInfo(0):Animator.GetCurrentAnimatorStateInfo(0);if(!Runner.OwnsAnimationState(info.fullPathHash))return;
   if(!rootEntered){Enemy.BeginAnimationAttackRootMotion(info.fullPathHash);rootEntered=true;}Call(Enemy,"OnAnimatorMove");Set(Mover,"_pendingAttackRootMotion",Vector3.zero);
   if(!events)return;var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mock/Animation/Enemy/SpecialAttacks/EnemyWarpAttack.anim");
   foreach(var e in AnimationUtility.GetAnimationEvents(clip))
   {if(e.time>info.normalizedTime*clip.length||!delivered.Add(e.functionName+"/"+e.time))continue;typeof(AnimationEvent).GetField("m_AnimatorStateInfo",F).SetValue(e,info);var source=typeof(AnimationEvent).GetField("m_Source",F);source.SetValue(e,Enum.Parse(source.FieldType,"Animator"));Go.SendMessage(e.functionName,e,SendMessageOptions.RequireReceiver);if(e.functionName=="AnimEvent_ComboEnableWeaponHitbox"){Call(Attacker,"HandleWeaponHit",Target.GetComponent<Collider>(),Weapon);Call(Attacker,"HandleWeaponHit",Target.GetComponent<Collider>(),Weapon);}}
  }
  public void Dispose(){UnityEngine.Object.DestroyImmediate(Go);UnityEngine.Object.DestroyImmediate(Target);UnityEngine.Object.DestroyImmediate(Config);UnityEngine.Object.DestroyImmediate(Pattern);}
 }
 static void Destinations(World w)
 {
  using(var f=new Fixture(w,false))
  {
   Check(EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out var p)&&Vector3.Distance(p,w.Origin-Vector3.forward*2.5f)<.15f,"Clear behind point on temporary navmesh");
   var wall=w.Object("blocking wall",p+Vector3.up);wall.AddComponent<BoxCollider>().size=new Vector3(2,2,1);Physics.SyncTransforms();Check(!EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out _),"Wall overlap rejected");UnityEngine.Object.DestroyImmediate(wall);
   wall=w.Object("unbaked corridor wall",w.Origin-Vector3.forward+Vector3.up);wall.AddComponent<BoxCollider>().size=new Vector3(3,2,.1f);Physics.SyncTransforms();Check(!EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out _),"Unbaked wall between player and destination rejected");UnityEngine.Object.DestroyImmediate(wall);
   f.Pattern.TeleportDistance=30;Check(!EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out _),"Cliff/outside navmesh rejected");f.Pattern.TeleportDistance=14.6f;Check(!EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out _),"Navmesh edge capsule clearance rejected");f.Pattern.TeleportDistance=2.5f;
   f.Target.transform.rotation=Quaternion.Euler(0,90,0);var desired=EnemyWarpDestination.DesiredPoint(f.Target.transform,2.5f,0);Check(Vector3.Distance(desired,w.Origin-Vector3.right*2.5f)<.001f,"Behind uses horizontal player forward");f.Target.transform.rotation=Quaternion.identity;
   f.Target.transform.position+=Vector3.right*2;Physics.SyncTransforms();Check(EnemyWarpDestination.TryFind(f.Target.transform,f.Body,f.Agent,f.Pattern,out p)&&Math.Abs(p.x-(w.Origin.x+2))<.1f,"Moving player yields updated destination");
   log.AppendLine("INFO before hold agent="+f.Agent.enabled+" active="+f.Agent.isActiveAndEnabled+" onMesh="+f.Agent.isOnNavMesh+" rb="+f.RB.position+" target="+p);
   f.Mover.HoldMovementForAttack();var source=f.RB.position;bool moved=f.Mover.TryTeleportDuringAttack(p);Check(!moved&&Vector3.Distance(f.RB.position,source)<.001f&&!f.Agent.enabled,"Unregistered Edit Mode Agent.Warp fails closed and retains source");f.Mover.ReleaseMovementAfterAttack();Check(f.Agent.enabled,"Agent enabled ownership restored after failed warp");
   var before=f.RB.position;f.Mover.HoldMovementForAttack();Check(!f.Mover.TryTeleportDuringAttack(new Vector3(float.NaN,0,0))&&Vector3.Distance(f.RB.position,before)<.001f,"Invalid teleport leaves source position");f.Mover.ReleaseMovementAfterAttack();
  }
 }
 static void Motion(World w)
 {
  using(var f=new Fixture(w,false))
  {
   f.Mover.HoldMovementForAttack();f.Mover.SetAttackTravelLimit(1);f.Mover.BeginAttackRootMotion();Set(f.Mover,"_pendingAttackRootMotion",Vector3.forward*8);f.Mover.FixedUpdateAttackRootMotion();Check(Math.Abs((float)Get(f.Mover,"_attackTravel")-1)<.01f,"Rush maximum travel clamps accumulated path");Set(f.Mover,"_pendingAttackRootMotion",Vector3.forward*8);f.Mover.FixedUpdateAttackRootMotion();Check(Math.Abs((float)Get(f.Mover,"_attackTravel")-1)<.01f,"Rush cannot exceed budget on later frames");
   f.Animator.speed=0;Set(f.Mover,"_pendingAttackRootMotion",Vector3.forward);f.Mover.FixedUpdateAttackRootMotion();Check(((Vector3)Get(f.Mover,"_pendingAttackRootMotion")).sqrMagnitude==0,"HitStop discards pending movement");f.Animator.speed=1;
   var wall=w.Object("sweep wall",f.RB.position+Vector3.forward*1.5f+Vector3.up*.9f);wall.AddComponent<BoxCollider>().size=new Vector3(3,2,.2f);Physics.SyncTransforms();var delta=AttackRootMotionPhysics.LimitDisplacement(f.RB,Vector3.forward*4+Vector3.up);Check(delta.z>0&&delta.z<1.2f&&delta.y==0,"Rush wall sweep stops before collider");UnityEngine.Object.DestroyImmediate(wall);
   f.Target.transform.position=f.RB.position+Vector3.forward*1.5f;Physics.SyncTransforms();delta=AttackRootMotionPhysics.LimitDisplacement(f.RB,Vector3.forward*4);Check(delta.z>0&&delta.z<1,"Rush player capsule sweep stops before overlap");
  }
 }
 static void Warp(World w)
 {
  using(var f=new Fixture(w))
  {
   bool hidden=false;float appear=-1,first=-1;bool previousHidden=false;float time=0;bool safe=true;
   for(int i=0;i<900;i++){f.Advance(1f/120);time+=1f/120;bool now=(bool)Get(f.Enemy,"_warpHidden");hidden|=now;if(previousHidden&&!now)appear=time;previousHidden=now;if(f.Hits>0&&first<0)first=time;if(now)safe&=!(bool)Get(f.Attacker,"_isHitboxActive")&&Vector3.Distance(f.Go.transform.position,f.Start)<.01f;}
   Check(hidden&&safe,"Vanish occurs at source with hitbox disabled");Check(appear>0&&first-appear>=.49f,"Reappearance precedes first damage by visible windup: "+(first-appear));Check(f.Hits==1,"Warp strike deduplicates repeated contact");Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.AI,"_isBusy")&&!f.Mover.IsUsingAttackRootMotion&&f.Agent.enabled,"Warp completion releases AI/root/agent");Check(!f.Enemy.CanSelectAttackCandidate(new EnemyDecisionConfig.ActionWeight{Pattern=f.Pattern}),"Cooldown persists after successful warp");
  }
  foreach(string mode in new[]{"counter","death","disable","combat","blocked","missing"})using(var f=new Fixture(w))
  {
   for(int i=0;i<100&&!(bool)Get(f.Enemy,"_warpHidden");i++)f.Advance(1f/120);Check((bool)Get(f.Enemy,"_warpHidden"),mode+" test reaches vanish");
   float elapsed=(float)Get(f.Enemy,"_warpElapsed");f.Animator.speed=0;f.Advance(10);Check((bool)Get(f.Enemy,"_warpHidden")&&Math.Abs((float)Get(f.Enemy,"_warpElapsed")-elapsed)<.0001f,"HitStop freezes hidden interval "+mode);f.Animator.speed=1;
   GameObject wall=null;
   if(mode=="counter")f.Enemy.ApplyDamage(new DamageInfo(1,Vector3.zero,Vector3.forward,f.Target,null,true,default,true,JustAvoidCounterStrike.First));
   if(mode=="death")f.Enemy.ApplyDamage(new DamageInfo(100000,Vector3.zero,Vector3.forward,f.Target,null));
   if(mode=="disable")Call(f.Enemy,"OnDisable");
   if(mode=="combat"){Set(f.Enemy,"_game",f.Target.AddComponent<GameManager>());Call(f.Enemy,"Update");}
   if(mode=="blocked"){wall=w.Object("late wall",w.Origin-Vector3.forward*2.5f+Vector3.up);wall.AddComponent<BoxCollider>().size=new Vector3(2,2,1);Physics.SyncTransforms();for(int i=0;i<60;i++)f.Advance(1f/120);Check(Vector3.Distance(f.Go.transform.position,f.Start)<.01f,"Late invalid destination aborts at source");}
   if(mode=="missing")for(int i=0;i<1400;i++)f.Advance(1f/120,false);
   Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.Enemy,"_warpHidden")&&((Dictionary<Renderer,bool>)Get(f.Enemy,"_warpRenderers")).Count==0&&!(bool)Get(f.Attacker,"_isHitboxActive")&&!f.Mover.IsUsingAttackRootMotion&&!(bool)Get(f.AI,"_isBusy"),"Warp cleanup "+mode);
   if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);
  }
  using(var f=new Fixture(w))
  {
   var renderers=f.Go.GetComponentsInChildren<Renderer>(true);if(renderers.Length>0)renderers[0].enabled=false;var before=renderers.Select(r=>r.enabled).ToArray();
   for(int i=0;i<100&&!(bool)Get(f.Enemy,"_warpHidden");i++)f.Advance(1f/120);f.Enemy.CancelAttackPattern(true);Check(renderers.Select((r,i)=>r.enabled==before[i]).All(x=>x),"Cancellation restores exact original renderer enabled values");
  }
  using(var f=new Fixture(w))
  {
   var wall=w.Object("blocked before hide",w.Origin-Vector3.forward*2.5f+Vector3.up);wall.AddComponent<BoxCollider>().size=new Vector3(2,2,1);Physics.SyncTransforms();f.Advance(.4f);Check(!f.Enemy.IsAttackPatternActive&&!(bool)Get(f.Enemy,"_warpHidden")&&Vector3.Distance(f.Go.transform.position,f.Start)<.01f,"Destination invalid before vanish aborts while visible");UnityEngine.Object.DestroyImmediate(wall);
  }
 }
}
