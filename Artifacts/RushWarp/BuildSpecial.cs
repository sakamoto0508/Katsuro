using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildSpecial
{
 const string Clips="Assets/Mock/Animation/Enemy/SpecialAttacks";
 const string Data="Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks";
 static void Folder(string p){if(!AssetDatabase.IsValidFolder(p))AssetDatabase.CreateFolder(p.Substring(0,p.LastIndexOf('/')),p.Substring(p.LastIndexOf('/')+1));}
 public static void Rush()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");Folder(Clips);Folder(Data);
  Create("RushAttack",EnemyActionType.RushAttack,1.3f,4,.45f,.65f,3,4.25f,5);
 }
 public static void Warp()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");Folder(Clips);Folder(Data);
  var p=Create("WarpAttack",EnemyActionType.WarpAttack,.7f,2f,.35f,.8f,3,18,8);
  p.TeleportDistance=2.5f;p.TeleportAngleOffset=0;p.DisappearDuration=.18f;p.ReappearWindupDuration=.5f;
  p.DisappearSound=AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("2f89b99ce7fc729468feb891fd98f74c"));p.ReappearSound=p.DisappearSound;
  p.WarpEffectPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/InportAssets/Effects/InabStudio/VfxAssets/CharacterEffects/Character Effects/Effect Prefabs/Vol 2/Smoke 1.prefab");EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
  string tellPath=Clips+"/EnemyWarpDeparture.anim";var tell=AssetDatabase.LoadAssetAtPath<AnimationClip>(tellPath);if(tell==null){AssetDatabase.CopyAsset(Clips+"/EnemyWarpAttack.anim",tellPath);tell=AssetDatabase.LoadAssetAtPath<AnimationClip>(tellPath);}
  AnimationUtility.SetAnimationEvents(tell,Array.Empty<AnimationEvent>());var settings=AnimationUtility.GetAnimationClipSettings(tell);settings.loopBlendPositionXZ=true;AnimationUtility.SetAnimationClipSettings(tell,settings);AssetDatabase.SaveAssetIfDirty(tell);
  var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");var sm=ctrl.layers[0].stateMachine.stateMachines.Single(s=>s.stateMachine.name=="Attacks").stateMachine;
  var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="WarpDeparture")??sm.AddState("WarpDeparture");state.motion=tell;state.speedParameter="EnemyComboPlaybackSpeed";state.speedParameterActive=true;state.tag="WarpDeparture";EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssetIfDirty(ctrl);
  var main=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");if(!main.FarCandidates.Any(c=>c.Pattern==p)){main.FarCandidates=main.FarCandidates.Concat(new[]{new EnemyDecisionConfig.ActionWeight{Action=EnemyActionType.WarpAttack,Pattern=p,Weight=2}}).ToArray();EditorUtility.SetDirty(main);AssetDatabase.SaveAssetIfDirty(main);}
  Smoke();
 }
 static EnemyAttackPattern Create(string name,EnemyActionType action,float scale,float max,float windup,float recovery,float min,float distance,float cooldown)
 {
  string path=Clips+"/Enemy"+name+".anim";
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
  if(clip==null){AssetDatabase.CopyAsset("Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Sprint.anim",path);clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);}
  clip.name="Enemy"+name;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlendOrientation=true;settings.keepOriginalOrientation=true;settings.orientationOffsetY=0;settings.loopBlendPositionY=true;settings.keepOriginalPositionY=true;settings.loopBlendPositionXZ=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
  AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent{time=.28f,functionName="AnimEvent_ComboEnableWeaponHitbox",intParameter=1},new AnimationEvent{time=.28f,functionName="AnimEvent_ComboSound",intParameter=1,stringParameter="BigSlash"},new AnimationEvent{time=.45f,functionName="AnimEvent_ComboDisableWeaponHitbox",intParameter=1},new AnimationEvent{time=.48f,functionName="AnimEvent_StopAttackRootMotion"},new AnimationEvent{time=clip.length-.03f,functionName="AnimEvent_ComboFinished"}});AssetDatabase.SaveAssetIfDirty(clip);
  var attack=AssetDatabase.LoadAssetAtPath<EnemyAttackData>(Data+"/"+name+"Data.asset");if(attack==null){attack=ScriptableObject.CreateInstance<EnemyAttackData>();AssetDatabase.CreateAsset(attack,Data+"/"+name+"Data.asset");}
  var so=new SerializedObject(attack);so.FindProperty("_strength").intValue=1;so.FindProperty("_actionType").intValue=(int)action;so.FindProperty("_animatorTrigger").stringValue=name;so.FindProperty("_damage").floatValue=10;so.FindProperty("_hitboxIndex").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(attack);
  var p=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(Data+"/"+name+".asset");if(p==null){p=ScriptableObject.CreateInstance<EnemyAttackPattern>();AssetDatabase.CreateAsset(p,Data+"/"+name+".asset");}
  p.PatternName=name;p.Attack=attack;p.AnimatorState="Base Layer.Attacks."+name;p.PlaybackSpeed=.9f;p.WindupDuration=windup;p.WindupEndTime=.22f;p.RootMotionScale=scale;p.MaximumTravelDistance=max;p.RecoveryDuration=recovery;p.MinAttackDistance=min;p.MaxAttackDistance=distance;p.Cooldown=cooldown;p.Timeout=3;EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
  var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
  if(!ctrl.parameters.Any(x=>x.name==name))ctrl.AddParameter(name,AnimatorControllerParameterType.Trigger);
  var sm=ctrl.layers[0].stateMachine.stateMachines.Single(s=>s.stateMachine.name=="Attacks").stateMachine;
  var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);state.motion=clip;state.tag="EnemyCombo";state.speed=1;state.speedParameter="EnemyComboPlaybackSpeed";state.speedParameterActive=true;if(!state.behaviours.Any(b=>b is AttackRootMotionState))state.AddStateMachineBehaviour<AttackRootMotionState>();
  if(!ctrl.layers[0].stateMachine.anyStateTransitions.Any(t=>t.destinationState==state)){var t=ctrl.layers[0].stateMachine.AddAnyStateTransition(state);t.AddCondition(AnimatorConditionMode.If,0,name);t.duration=.08f;t.hasFixedDuration=true;t.hasExitTime=false;t.canTransitionToSelf=false;}
  EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssetIfDirty(ctrl);
  TestConfig(name,action,p);
  var main=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");if(!main.MidCandidates.Any(c=>c.Pattern==p)){main.MidCandidates=main.MidCandidates.Concat(new[]{new EnemyDecisionConfig.ActionWeight{Action=action,Pattern=p,Weight=action==EnemyActionType.RushAttack?5:2}}).ToArray();EditorUtility.SetDirty(main);AssetDatabase.SaveAssetIfDirty(main);}
  return p;
 }
 static void TestConfig(string name,EnemyActionType action,EnemyAttackPattern p)
 {
  string path=Data+"/EnemyDecisionConfig_"+name+"Test.asset";var c=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>(path);if(c==null){c=ScriptableObject.CreateInstance<EnemyDecisionConfig>();AssetDatabase.CreateAsset(c,path);}
  var row=new[]{new EnemyDecisionConfig.ActionWeight{Action=action,Pattern=p,Weight=1}};c.FarCandidates=row;c.MidCandidates=row;c.NearCandidates=row;c.RepeatPenalty=1;c.ObserveSeconds=.3f;EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
 }
 public static void Smoke()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  const string source="Assets/InportAssets/Effects/InabStudio/VfxAssets/CharacterEffects/Character Effects/Effect Prefabs/Vol 2/Smoke 1.prefab";
  var root=PrefabUtility.LoadPrefabContents(source);
  try{foreach(var binder in root.GetComponentsInChildren<INab.CommonVFX.VFXLossyTransformBinder>(true))binder.Target=root.transform;PrefabUtility.SaveAsPrefabAsset(root,"Assets/Mock/Prefabs/EnemyWarpSmoke.prefab");}
  finally{PrefabUtility.UnloadPrefabContents(root);}
  var p=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(Data+"/WarpAttack.asset");p.WarpEffectPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/EnemyWarpSmoke.prefab");EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
 }
}
