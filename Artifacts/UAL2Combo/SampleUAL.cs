using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class SampleUAL
{
 static object Vec(Vector3 v)=>new{v.x,v.y,v.z};
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var results=new List<object>();var log=new System.Text.StringBuilder();
  foreach(string kind in new[]{"Regular","Heavy"}) foreach(bool apply in new[]{false,true})
  {
   var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   var controller=new AnimatorController();controller.AddLayer("Base Layer");
   try
   {
    string path="Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2"+kind+"Combo.anim";
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    var state=controller.layers[0].stateMachine.AddState("Probe");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;
    var a=go.GetComponent<Animator>();a.runtimeAnimatorController=controller;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=apply;a.Rebind();a.Play("Base Layer.Probe",0,0);a.Update(0);
    var so=new SerializedObject(go.GetComponent<EnemyController>());var cols=so.FindProperty("_enemyWeaponColliders");
    var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    var frames=new List<object>();Vector3 sum=Vector3.zero;float maxY=0;
    for(int i=0;i<Mathf.RoundToInt(clip.length*60);i++)
    {
     a.Update(1f/60f);var delta=a.deltaPosition;sum+=delta;maxY=Mathf.Max(maxY,Mathf.Abs(delta.y));
     frames.Add(new{t=(i+1)/60f,delta=Vec(delta),bones=bones.Select(b=>Vec(go.transform.InverseTransformPoint(a.GetBoneTransform(b).position))).ToArray(),blade=Enumerable.Range(0,cols.arraySize).Select(n=>Vec(go.transform.InverseTransformPoint(((Collider)cols.GetArrayElementAtIndex(n).objectReferenceValue).transform.position))).ToArray()});
    }
    log.AppendLine(kind+" applyRootMotion="+apply+" human="+clip.humanMotion+" hasRoot="+clip.hasRootCurves+" sum="+sum.ToString("F6")+" maxY="+maxY+" avatar="+a.avatar.isValid);
    results.Add(new{kind,apply,path,length=clip.length,sum=Vec(sum),maxY,frames});
   }
   finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(controller);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  }
  File.WriteAllText("Artifacts/UAL2Combo/MotionSamples.json",Newtonsoft.Json.JsonConvert.SerializeObject(results));
  File.WriteAllText("Artifacts/UAL2Combo/DeltaExtraction.txt",log.ToString());
 }
}
