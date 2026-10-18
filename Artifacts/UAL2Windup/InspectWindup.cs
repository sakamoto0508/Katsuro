using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectWindup
{
 static HumanBodyBones[] bones={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
 static object Vec(Vector3 v)=>new{v.x,v.y,v.z};
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var log=new StringBuilder();var poses=new List<object>();var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
  var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);var a=go.GetComponent<Animator>();go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var candidates=new List<AnimationClip>();
  foreach(string name in new[]{"Attack_Heavy1_Start","Guard_Start","Guard_B_Start","Idle1_to_Idle2"})
  {var p="Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_"+name+".anim";var c=AssetDatabase.LoadAssetAtPath<AnimationClip>(p);if(c!=null)candidates.Add(c);}
  string source="Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";
  var ual=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&c.name.Contains("Sword")).ToArray();
  log.AppendLine("UAL Sword names: "+string.Join(", ",ual.Select(c=>c.name)));
  candidates.AddRange(ual.Where(c=>c.name.Contains("Idle")||c.name.Contains("Draw")||c.name.Contains("Ready")||c.name.EndsWith("Sword_Block")));
  candidates.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/InportAssets/ARPGPack/ARPGHalberd/Animations/Humanoid/ARPG_Halberd_Attack_Heavy1_Start.anim"));
  var controller=new AnimatorController();controller.AddLayer("Base Layer");var state=controller.layers[0].stateMachine.AddState("Probe");controller.layers[0].stateMachine.defaultState=state;state.speedParameter="EnemyComboPlaybackSpeed";state.speedParameterActive=true;controller.AddParameter("EnemyComboPlaybackSpeed",AnimatorControllerParameterType.Float);a.runtimeAnimatorController=controller;
  try
  {
   foreach(string kind in new[]{"Regular","Heavy"})
   {
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2"+kind+"Combo.anim");state.motion=clip;
    foreach(float speed in new[]{1f,.85f})
    {a.Rebind();a.SetFloat("EnemyComboPlaybackSpeed",speed);a.Play("Base Layer.Probe",0,0);a.Update(0);a.Update(.5f);log.AppendLine(kind+" speed="+speed+" clipTimeAfter0.5s="+a.GetCurrentAnimatorStateInfo(0).normalizedTime*clip.length+" globalSpeed="+a.speed);}
    a.Rebind();a.Play("Base Layer.Probe",0,0);a.SetFloat("EnemyComboPlaybackSpeed",1);a.Update(0);
    var target=bones.Select(b=>a.GetBoneTransform(b).localRotation).ToArray();
    foreach(var c in candidates)
    {
     state.motion=c;a.Rebind();a.SetFloat("EnemyComboPlaybackSpeed",1);a.Play("Base Layer.Probe",0,0);a.Update(0);a.Update(c.length);
     float mean=bones.Select((b,i)=>Quaternion.Angle(a.GetBoneTransform(b).localRotation,target[i])).Average();log.AppendLine(kind+" candidate="+c.name+" human="+c.humanMotion+" length="+c.length+" endToComboStartMeanBoneAngle="+mean);
    }
    state.motion=clip;
    foreach(float t in new[]{0,.05f,.10f,kind=="Regular"?.15f:.25f})
    {a.Rebind();a.Play("Base Layer.Probe",0,0);a.SetFloat("EnemyComboPlaybackSpeed",1);a.Update(0);a.Update(t);var ec=new SerializedObject(go.GetComponent<EnemyController>()).FindProperty("_enemyWeaponColliders");poses.Add(new{kind,t,bones=bones.Select(b=>Vec(go.transform.InverseTransformPoint(a.GetBoneTransform(b).position))).ToArray(),blade=Enumerable.Range(0,ec.arraySize).Select(i=>Vec(go.transform.InverseTransformPoint(((Collider)ec.GetArrayElementAtIndex(i).objectReferenceValue).transform.position))).ToArray()});}
   }
  }
  finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(controller);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  File.WriteAllText("Artifacts/UAL2Windup/Inspection.txt",log.ToString());File.WriteAllText("Artifacts/UAL2Windup/LeadInPoses.json",Newtonsoft.Json.JsonConvert.SerializeObject(poses));
 }
}
