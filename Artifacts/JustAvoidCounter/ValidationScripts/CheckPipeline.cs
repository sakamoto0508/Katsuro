using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public sealed class CounterDamageProbe : MonoBehaviour,IDamageable
{
 public DamageInfo Last;public int Hits;public CombatFeedback Feedback;
 public void ApplyDamage(DamageInfo info){Last=info;Hits++;Feedback.Hit(info);}
}
public static class CounterPipelineCheck
{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 public static string Main()
 {
  var preview=new PreviewRenderUtility();CombatFeedback feedback=null;object pulse=null;PlayerAttacker attacker=null;
  var report=new System.Text.StringBuilder();
  try {
   var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab"));preview.AddSingleGO(player);
   foreach(var b in player.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var anim=player.GetComponent<Animator>();anim.fireEvents=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;anim.Rebind();anim.Update(0);
   var blade=new GameObject("Probe blade",typeof(BoxCollider),typeof(WeaponHitboxRelay));preview.AddSingleGO(blade);
   var box=blade.GetComponent<BoxCollider>();var relay=blade.GetComponent<WeaponHitboxRelay>();
   var weapon=new PlayerWeapon(new Collider[]{box});weapon.Init(Vector3.zero);
   attacker=new PlayerAttacker(null,null,weapon,null,null,player.transform);
   var target=new GameObject("Probe target",typeof(BoxCollider));preview.AddSingleGO(target);
   feedback=target.AddComponent<CombatFeedback>();feedback.Init(vfxConfig:AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset"),enableJustAvoid:false);
   var probe=target.AddComponent<CounterDamageProbe>();probe.Feedback=feedback;
   var first=Get<JustAvoidCounterCutVFX[]>(feedback,"_counterFirstCuts");var second=Get<JustAvoidCounterCutVFX[]>(feedback,"_counterCuts");
   foreach(var c in first)preview.AddSingleGO(c.gameObject);foreach(var c in second)preview.AddSingleGO(c.gameObject);
   foreach(var go in Get<GameObject[]>(feedback,"_hitRoots"))preview.AddSingleGO(go);
   pulse=Get<object>(feedback,"_contactPulse");foreach(var go in Get<GameObject[]>(pulse,"objects"))preview.AddSingleGO(go);
   int expected=0;
   for(int stage=1;stage<=2;stage++){
    anim.Play(Animator.StringToHash("Base Layer.ARPG_Samurai_Attack_Heavy"+stage),0,.3f);anim.Update(0);
    var controller=(UnityEditor.Animations.AnimatorController)anim.runtimeAnimatorController; foreach(var state in controller.layers[0].stateMachine.states) if(state.state.name=="ARPG_Samurai_Attack_Heavy"+stage) foreach(var behaviour in state.state.behaviours) if(behaviour is JustAvoidCounterSpeedState callback) callback.OnStateEnter(anim,anim.GetCurrentAnimatorStateInfo(0),0); attacker.EnableWeaponHitbox();
    blade.transform.position=Vector3.zero;Physics.SyncTransforms();typeof(WeaponHitboxRelay).GetMethod("LateUpdate",F).Invoke(relay,null);
    blade.transform.position=new Vector3(.25f,.1f,0);Physics.SyncTransforms();
    typeof(WeaponHitboxRelay).GetMethod("OnTriggerEnter",F).Invoke(relay,new object[]{target.GetComponent<Collider>()});expected++;
    if(probe.Hits!=expected || (int)probe.Last.CounterStrike!=stage || !probe.Last.IsJustAvoidCounter || Vector3.Dot(probe.Last.SlashDirection,new Vector3(.25f,.1f,0).normalized)<.99f)throw new Exception("Stage/sweep handoff failed: "+stage+" hits="+probe.Hits+" strike="+probe.Last.CounterStrike+" counter="+probe.Last.IsJustAvoidCounter+" slash="+probe.Last.SlashDirection+" relay="+relay.SweepDirection+" tag="+anim.GetCurrentAnimatorStateInfo(0).IsTag(PlayerAttacker.JustAvoidCounterTag)+" component="+player.GetComponent<JustAvoidCounterAnimation>().Strike);
    var pool=stage==1?first:second;if(!Array.Exists(pool,c=>c.gameObject.activeSelf))throw new Exception("Dedicated CFX not active: "+stage);
    typeof(WeaponHitboxRelay).GetMethod("OnTriggerEnter",F).Invoke(relay,new object[]{target.GetComponent<Collider>()});if(probe.Hits!=expected)throw new Exception("Duplicate damage accepted");
    foreach(var c in first)c.Stop();foreach(var c in second)c.Stop();attacker.DisableWeaponHitbox();
    report.AppendLine("PASS Animator _strike -> Hitbox latch -> WeaponHitboxRelay -> PlayerAttacker -> DamageInfo -> dedicated CFX stage "+stage+"; SweepDirection preserved; duplicate suppressed");
   }
   var enemy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));preview.AddSingleGO(enemy);foreach(var b in enemy.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
   var ea=enemy.GetComponent<Animator>();ea.fireEvents=false;ea.cullingMode=AnimatorCullingMode.AlwaysAnimate;ea.Rebind();ea.Update(0);var reaction=enemy.GetComponent<EnemyAnimationController>();reaction.Init();
   var chest=ea.GetBoneTransform(HumanBodyBones.Chest);var start=chest.localRotation;
   var info=new DamageInfo(1,enemy.transform.position+Vector3.up,Vector3.forward,player,null,isJustAvoidCounter:true,counterStrike:JustAvoidCounterStrike.Second);
   reaction.TryPlayHitReaction(info);float angle=Quaternion.Angle(start,chest.localRotation);
   if(ea.IsInTransition(1)||angle<5)throw new Exception("Impact pose not evaluated");
   var held=chest.localRotation;ea.speed=0;reaction.TickHitReaction(.155f);typeof(EnemyAnimationController).GetMethod("LateUpdate",F).Invoke(reaction,null);
   if(Quaternion.Angle(held,chest.localRotation)>.05f)throw new Exception("Impact pose drift during stop");
   ea.speed=1;ea.Update(.1f);reaction.TickHitReaction(.1f);typeof(EnemyAnimationController).GetMethod("LateUpdate",F).Invoke(reaction,null);
   if(Quaternion.Angle(held,chest.localRotation)<1)throw new Exception("No follow through");
   reaction.CancelHitReaction();if(reaction.IsReacting||ea.GetLayerWeight(1)!=0)throw new Exception("Reaction cancel failed");
   report.AppendLine("PASS Second impact chest delta="+angle+" degrees; no transition; frozen pose held; follow through changes pose; cancel clears layer/offsets");
   System.IO.File.WriteAllText("Temp/CounterV2/PipelineVerification.txt",report.ToString());return report.ToString();
  } finally {
   attacker?.Dispose();
   if(feedback!=null){feedback.enabled=false;var material=Get<Material>(feedback,"_material");if(material!=null)UnityEngine.Object.DestroyImmediate(material);Set(feedback,"_material",null);Set(feedback,"_hitRoots",null);Set(feedback,"_counterCuts",null);Set(feedback,"_counterFirstCuts",null);Set(feedback,"_contactPulse",null);}
   if(pulse!=null){UnityEngine.Object.DestroyImmediate(Get<Material>(pulse,"material"));UnityEngine.Object.DestroyImmediate(Get<Mesh>(pulse,"mesh"));}
   preview.Cleanup();
  }
 }
}


