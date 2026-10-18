using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectRush
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  const string root="Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/";
  const string work="Assets/InportAssets/RushImportWork";
  if(!AssetDatabase.IsValidFolder(work))AssetDatabase.CreateFolder("Assets/InportAssets","RushImportWork");
  string path=work+"/UAL2_Rush_RM.fbx";
  if(AssetImporter.GetAtPath(path)==null)AssetDatabase.CopyAsset(root+"UAL2_Standard_RM.fbx",path);
  var source=(ModelImporter)AssetImporter.GetAtPath(root+"UAL2_Standard.fbx");var dest=(ModelImporter)AssetImporter.GetAtPath(path);
  dest.animationType=ModelImporterAnimationType.Human;dest.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;dest.humanDescription=source.humanDescription;dest.SaveAndReimport();
  File.Copy(path+".meta","Artifacts/RushWarp/RMImporter.meta.txt",true);
  var dash=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name=="Armature|Sword_Dash");
  var sprint=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Sprint.anim");
  var rows=new List<object>();var log=new System.Text.StringBuilder();
  foreach(var original in new[]{sprint,dash})foreach(float yaw in new[]{0f,180f})
  {
   var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"),scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   var clip=UnityEngine.Object.Instantiate(original);AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
   var s=AnimationUtility.GetAnimationClipSettings(clip);s.loopTime=false;s.loopBlendOrientation=true;s.keepOriginalOrientation=true;s.orientationOffsetY=yaw;s.loopBlendPositionY=true;s.keepOriginalPositionY=true;s.loopBlendPositionXZ=false;AnimationUtility.SetAnimationClipSettings(clip,s);
   var ctrl=new AnimatorController();ctrl.AddLayer("Base Layer");var state=ctrl.layers[0].stateMachine.AddState("Probe");state.motion=clip;ctrl.layers[0].stateMachine.defaultState=state;
   try
   {
    var a=go.GetComponent<Animator>();a.runtimeAnimatorController=ctrl;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.Play("Base Layer.Probe",0,0);a.Update(0);
    var cols=new SerializedObject(go.GetComponent<EnemyController>()).FindProperty("_enemyWeaponColliders");var frames=new List<object>();Vector3 sum=Vector3.zero;
    var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    for(int i=0;i<Mathf.RoundToInt(clip.length*60);i++){a.Update(1f/60);sum+=a.deltaPosition;frames.Add(new{t=(i+1)/60f,delta=V(a.deltaPosition),bones=bones.Select(b=>V(go.transform.InverseTransformPoint(a.GetBoneTransform(b).position))).ToArray(),blade=Enumerable.Range(0,cols.arraySize).Select(n=>V(go.transform.InverseTransformPoint(((Collider)cols.GetArrayElementAtIndex(n).objectReferenceValue).transform.position))).ToArray()});}
    log.AppendLine(original.name+" yaw="+yaw+" human="+clip.humanMotion+" avatar="+a.avatar.isValid+" length="+clip.length+" sum="+sum.ToString("F6"));rows.Add(new{name=original.name,yaw,length=clip.length,frames});
   }
   finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(ctrl);UnityEngine.Object.DestroyImmediate(clip);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  }
  File.WriteAllText("Artifacts/RushWarp/Candidates.txt",log.ToString());File.WriteAllText("Artifacts/RushWarp/Candidates.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));
 }
 static object V(Vector3 v)=>new{v.x,v.y,v.z};
}
