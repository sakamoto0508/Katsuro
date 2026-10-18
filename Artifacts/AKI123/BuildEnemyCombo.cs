using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildEnemyCombo
{
 const string AnimFolder="Assets/Mock/Animation/Enemy/TwoHitCombo";
 const string DataFolder="Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo";
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode only");
  Folder(AnimFolder); Folder(DataFolder);
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
  var machine=controller.layers[0].stateMachine;
  var steps=new EnemyAttackPattern.AttackStep[2];
  for(int i=0;i<2;i++)
  {
   int n=i+1; string trigger="EnemyCombo"+n;
   string path=AnimFolder+"/EnemyCombo"+n+".anim";
   if(AssetDatabase.LoadAssetAtPath<AnimationClip>(path)==null)
    if(!AssetDatabase.CopyAsset("Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo"+n+".anim",path)) throw new Exception("Cannot copy clip");
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
   var originalClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo"+n+".anim");
   EditorUtility.CopySerialized(originalClip,clip);
   clip.name=trigger;
   var settings=AnimationUtility.GetAnimationClipSettings(clip);
   settings.loopTime=false;
   // 実Avatarの手・頭・腕の接続姿勢を比較し、復帰動作/重複する構え直しだけを省く。
   float start=i==0?0f:5f/60f, stop=i==0?.4f:originalClip.length;
   var bindings=AnimationUtility.GetCurveBindings(clip);
   var curves=bindings.Select(binding=>
   {
    var original=AnimationUtility.GetEditorCurve(clip,binding);
    Func<float,float> slope=t=>(original.Evaluate(t+.0001f)-original.Evaluate(t-.0001f))/.0002f;
    var keys=original.keys.Where(k=>k.time>start && k.time<stop).Select(k=> {k.time-=start;return k;}).ToList();
    keys.Insert(0,new Keyframe(0f,original.Evaluate(start),slope(start),slope(start)));
    keys.Add(new Keyframe(stop-start,original.Evaluate(stop),slope(stop),slope(stop)));
    return new AnimationCurve(keys.ToArray());
   }).ToArray();
   AnimationUtility.SetEditorCurves(clip,bindings,curves);
   settings.startTime=0f;settings.stopTime=stop-start;
   AnimationUtility.SetAnimationClipSettings(clip,settings);
   float on=i==0 ? 11f/60f : 18f/60f-start;
   float off=i==0 ? 19f/60f : 27f/60f-start;
   float end=i==0 ? 23f/60f : 57f/60f-start;
   AnimationUtility.SetAnimationEvents(clip,new[]{
    Event(on,"AnimEvent_ComboEnableWeaponHitbox",n),
    Event(on,"AnimEvent_ComboSound",n,i==0?"NormalSlash":"Slash2"),
    Event(off,"AnimEvent_ComboDisableWeaponHitbox",n),
    Event(end,"AnimEvent_ComboStepFinished",n)
   });
   EditorUtility.SetDirty(clip);
   var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==trigger) ?? machine.AddState(trigger,new Vector3(650,i*100+100,0));
   state.motion=clip; state.speed=1f; state.tag="EnemyCombo";
   if(!state.behaviours.Any(b=>b is AttackRootMotionState)) state.AddStateMachineBehaviour<AttackRootMotionState>();
   if(!controller.parameters.Any(p=>p.name==trigger)) controller.AddParameter(trigger,AnimatorControllerParameterType.Trigger);
   if(!machine.anyStateTransitions.Any(t=>t.destinationState==state && t.conditions.Any(c=>c.parameter==trigger)))
   {
    var transition=machine.AddAnyStateTransition(state);
    transition.AddCondition(AnimatorConditionMode.If,0,trigger);
    transition.hasExitTime=false; transition.hasFixedDuration=true; transition.duration=.08f;
    transition.canTransitionToSelf=false; transition.interruptionSource=TransitionInterruptionSource.None;
   }
   // 終了EventとRunnerが段間/Recoveryを制御。自動Idle遷移を置かず終端姿勢を保持する。
   string dataPath=DataFolder+"/ComboStep"+n+".asset";
   var data=AssetDatabase.LoadAssetAtPath<EnemyAttackData>(dataPath);
   if(data==null) { data=ScriptableObject.CreateInstance<EnemyAttackData>();AssetDatabase.CreateAsset(data,dataPath); }
   var so=new SerializedObject(data);
   so.FindProperty("_strength").enumValueIndex=0; so.FindProperty("_actionType").enumValueIndex=3;
   so.FindProperty("_animatorTrigger").stringValue=trigger; so.FindProperty("_damage").floatValue=10;
   so.FindProperty("_range").floatValue=1.2f;so.FindProperty("_hitboxIndex").intValue=0;
   so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
   steps[i]=new EnemyAttackPattern.AttackStep{Attack=data,AnimatorState="Base Layer."+trigger,Timeout=3f};
  }
  string patternPath=DataFolder+"/TwoHitCombo.asset";
  var pattern=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(patternPath);
  if(pattern==null) {pattern=ScriptableObject.CreateInstance<EnemyAttackPattern>();AssetDatabase.CreateAsset(pattern,patternPath);}
  pattern.PatternName="TwoHitCombo";pattern.AttackSteps=steps;pattern.Windup=.15f;pattern.ComboInterval=.12f;pattern.RecoveryDuration=.65f;
  EditorUtility.SetDirty(pattern);EditorUtility.SetDirty(controller);
  var config=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");
  // 攻撃合計50/後退10を維持。例の攻撃内比率20:10:10:10:20を50へ換算。
  var list=config.NearCandidates.Where(c=>c.Pattern!=pattern).ToList();
  float[] weights={100f/7f,50f/7f,50f/7f,50f/7f};
  int k=0;
  for(int i=0;i<list.Count;i++) if(list[i].Attack!=null && k<4) {var item=list[i];item.Weight=weights[k++];list[i]=item;}
  list.Insert(Math.Min(4,list.Count),new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.Slash,Pattern=pattern,Weight=100f/7f});
  config.NearCandidates=list.ToArray(); EditorUtility.SetDirty(config);
  foreach (var step in steps) AssetDatabase.SaveAssetIfDirty(step.Attack);
  foreach (string clipPath in AssetDatabase.FindAssets("t:AnimationClip",new[]{AnimFolder}).Select(AssetDatabase.GUIDToAssetPath))
      AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath));
  AssetDatabase.SaveAssetIfDirty(pattern); AssetDatabase.SaveAssetIfDirty(controller); AssetDatabase.SaveAssetIfDirty(config);
 }
 static AnimationEvent Event(float time,string function,int step,string sound="") => new AnimationEvent {time=time,functionName=function,intParameter=step,stringParameter=sound};
 static void Folder(string path) { if(AssetDatabase.IsValidFolder(path))return;int split=path.LastIndexOf('/');Folder(path.Substring(0,split));AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1)); }
}
