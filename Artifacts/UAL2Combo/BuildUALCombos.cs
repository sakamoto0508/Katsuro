using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BuildUALCombos
{
 const string Clips="Assets/Mock/Animation/Enemy/UAL2Combos";
 const string Data="Assets/Mock/ScriptableObjects/Enemy/UAL2Combos";
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  if(!AssetDatabase.IsValidFolder(Data))AssetDatabase.CreateFolder("Assets/Mock/ScriptableObjects/Enemy","UAL2Combos");
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
  var root=controller.layers[0].stateMachine;var all=States(root).ToArray();
  var regularState=all.Single(s=>s.name=="LightCombo" || s.name=="RegularCombo");var heavyState=all.Single(s=>s.name=="HeavyCombo");
  // ユーザーの既存Stateを再利用。攻撃途中の自動遷移や旧Idleへのoffset付き復帰だけを整理する。
  foreach(var state in all)foreach(var t in state.transitions.ToArray())
   if(state==regularState||state==heavyState||t.destinationState==regularState||t.destinationState==heavyState)state.RemoveTransition(t);
  foreach(var t in root.anyStateTransitions.ToArray())if(t.destinationState==regularState||t.destinationState==heavyState)root.RemoveAnyStateTransition(t);
  regularState.name="RegularCombo";
  for(int i=controller.parameters.Length-1;i>=0;i--)if(controller.parameters[i].name=="LightCombo"||controller.parameters[i].name=="EnemyCombo1"||controller.parameters[i].name=="EnemyCombo2")controller.RemoveParameter(i);
  if(!controller.parameters.Any(p=>p.name=="RegularCombo"))controller.AddParameter("RegularCombo",AnimatorControllerParameterType.Trigger);
  if(!controller.parameters.Any(p=>p.name=="EnemyComboPlaybackSpeed"))controller.AddParameter("EnemyComboPlaybackSpeed",AnimatorControllerParameterType.Float);
  var pp=controller.parameters;for(int i=0;i<pp.Length;i++)if(pp[i].name=="EnemyComboPlaybackSpeed")pp[i].defaultFloat=1;controller.parameters=pp;
  var patterns=new List<EnemyAttackPattern>();
  foreach(string kind in new[]{"Regular","Heavy"})
  {
   bool heavy=kind=="Heavy";string name=kind+"Combo";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips+"/EnemyUAL2"+kind+"Combo.anim");
   var settings=AnimationUtility.GetAnimationClipSettings(clip);
   settings.loopTime=false;settings.loopBlendOrientation=true;settings.keepOriginalOrientation=true;settings.orientationOffsetY=180;
   settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=false;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=false;
   AnimationUtility.SetAnimationClipSettings(clip,settings);
   float[][] windows=heavy ? new[]{new[]{.28f,.50f},new[]{1.05f,1.30f},new[]{1.72f,1.95f},new[]{2.43f,2.73f}} : new[]{new[]{.17f,.32f},new[]{.65f,.80f},new[]{1.45f,1.67f}};
   var events=new List<AnimationEvent>();
   for(int i=0;i<windows.Length;i++)
   {
    events.Add(E(windows[i][0],"AnimEvent_ComboEnableWeaponHitbox",i+1));
    events.Add(E(windows[i][0],"AnimEvent_ComboSound",i+1,heavy?"Slash2":"NormalSlash"));
    events.Add(E(windows[i][1],"AnimEvent_ComboDisableWeaponHitbox",i+1));
   }
   events.Add(E(clip.length-.05f,"AnimEvent_ComboFinished",0));AnimationUtility.SetAnimationEvents(clip,events.ToArray());
   EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
   var state=heavy?heavyState:regularState;state.motion=clip;state.tag="EnemyCombo";state.speed=1;state.speedParameterActive=true;state.speedParameter="EnemyComboPlaybackSpeed";
   if(!state.behaviours.Any(b=>b is AttackRootMotionState))state.AddStateMachineBehaviour<AttackRootMotionState>();
   var tr=root.AddAnyStateTransition(state);tr.AddCondition(AnimatorConditionMode.If,0,name);tr.hasExitTime=false;tr.hasFixedDuration=true;tr.duration=.08f;tr.canTransitionToSelf=false;tr.interruptionSource=TransitionInterruptionSource.None;
   string dataPath=Data+"/"+name+"Attack.asset";var attack=AssetDatabase.LoadAssetAtPath<EnemyAttackData>(dataPath);
   if(attack==null){attack=ScriptableObject.CreateInstance<EnemyAttackData>();AssetDatabase.CreateAsset(attack,dataPath);}
   var so=new SerializedObject(attack);so.FindProperty("_strength").enumValueIndex=heavy?1:0;so.FindProperty("_actionType").enumValueIndex=3;
   so.FindProperty("_animatorTrigger").stringValue=name;so.FindProperty("_damage").floatValue=heavy?6:5;so.FindProperty("_range").floatValue=1.2f;so.FindProperty("_hitboxIndex").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
   EditorUtility.SetDirty(attack);AssetDatabase.SaveAssetIfDirty(attack);
   string patternPath=Data+"/"+name+".asset";var pattern=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(patternPath);
   if(pattern==null){pattern=ScriptableObject.CreateInstance<EnemyAttackPattern>();AssetDatabase.CreateAsset(pattern,patternPath);}
   pattern.PatternName=name;pattern.Attack=attack;pattern.AnimatorState="Base Layer.Attacks."+name;pattern.Windup=heavy?.15f:.05f;pattern.RecoveryDuration=heavy?.8f:.4f;pattern.Timeout=heavy?7f:5f;pattern.PlaybackSpeed=1;pattern.RootMotionScale=heavy?.10f:.18f;
   EditorUtility.SetDirty(pattern);AssetDatabase.SaveAssetIfDirty(pattern);patterns.Add(pattern);
  }
  var old=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/TwoHitCombo.asset");
  foreach(string guid in AssetDatabase.FindAssets("t:EnemyDecisionConfig"))
  {
   var config=AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>(AssetDatabase.GUIDToAssetPath(guid));bool changed=false;
   Func<EnemyDecisionConfig.ActionWeight[],EnemyDecisionConfig.ActionWeight[]> migrate=rows=>
   {
    if(rows==null)return null;var result=new List<EnemyDecisionConfig.ActionWeight>();
    foreach(var row in rows)
    {
     if(old!=null && row.Pattern==old)
     {
      bool test=AssetDatabase.GetAssetPath(config).Contains("ComboTest");float ratio=test?.5f:2f/3f;
      result.Add(new EnemyDecisionConfig.ActionWeight{Action=row.Action,Pattern=patterns[0],Weight=row.Weight*ratio});
      result.Add(new EnemyDecisionConfig.ActionWeight{Action=row.Action,Pattern=patterns[1],Weight=row.Weight*(1-ratio)});changed=true;
     }
     else result.Add(row);
    }
    return result.ToArray();
   };
   config.NearCandidates=migrate(config.NearCandidates);config.MidCandidates=migrate(config.MidCandidates);config.FarCandidates=migrate(config.FarCandidates);
   if(changed){EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);}
  }
  // 参照がなくなった旧TwoHit専用アセットのみ削除。開始時の完全コピーをTempへ退避済み。
  string[] obsolete={"Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/TwoHitCombo.asset","Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/ComboStep1.asset","Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo/ComboStep2.asset","Assets/Mock/Animation/Enemy/TwoHitCombo/EnemyCombo1.anim","Assets/Mock/Animation/Enemy/TwoHitCombo/EnemyCombo2.anim"};
  foreach(var pair in Machines(root).ToArray())foreach(var state in pair.states.ToArray())if(state.state.name=="EnemyCombo1"||state.state.name=="EnemyCombo2")pair.RemoveState(state.state);
  EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
  foreach(string path in obsolete)
  {
   var dependents=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Mock/")&&(p.EndsWith(".asset")||p.EndsWith(".prefab")||p.EndsWith(".controller")||p.EndsWith(".unity"))&&!obsolete.Contains(p)).Where(p=>AssetDatabase.GetDependencies(p,false).Contains(path)).ToArray();
   if(dependents.Length>0)throw new Exception("Still referenced: "+path+" by "+string.Join(",",dependents));
  }
  foreach(string path in obsolete)if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
  foreach(string dir in new[]{"Assets/Mock/ScriptableObjects/Enemy/TwoHitCombo","Assets/Mock/Animation/Enemy/TwoHitCombo"})
   if(AssetDatabase.IsValidFolder(dir)&&Directory.GetFiles(dir).Length==0)AssetDatabase.DeleteAsset(dir);
 }
 static AnimationEvent E(float time,string name,int number,string sound="")=>new AnimationEvent{time=time,functionName=name,intParameter=number,stringParameter=sound};
 static System.Collections.Generic.IEnumerable<AnimatorState> States(AnimatorStateMachine root)=>Machines(root).SelectMany(m=>m.states.Select(s=>s.state));
 static System.Collections.Generic.IEnumerable<AnimatorStateMachine> Machines(AnimatorStateMachine root){yield return root;foreach(var m in root.stateMachines)foreach(var child in Machines(m.stateMachine))yield return child;}
}
