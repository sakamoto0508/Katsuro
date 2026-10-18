using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InspectComboMotion
{
 [Serializable] public class Pose { public string clip; public float time; public string[] bones; public Vector3[] positions; public Vector3[] blade; }
 [Serializable] public class Result { public List<Pose> poses = new List<Pose>(); }
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode only");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
  try
  {
   var animator=go.GetComponent<Animator>(); animator.Rebind(); animator.Update(0);
   var controller=new SerializedObject(go.GetComponent<EnemyController>());
   var array=controller.FindProperty("_enemyWeaponColliders");
   var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
   var result=new Result();
   foreach(int n in new[]{1,2,3})
   {
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo"+n+".anim");
    for(int frame=0;frame<=Mathf.RoundToInt(clip.length*60);frame+=3)
    {
     float t=Mathf.Min(clip.length-.0001f,frame/60f);
     clip.SampleAnimation(go,t);
     var pose=new Pose{clip=clip.name,time=t,bones=Array.ConvertAll(bones,b=>b.ToString()),positions=new Vector3[bones.Length],blade=new Vector3[array.arraySize]};
     for(int i=0;i<bones.Length;i++) pose.positions[i]=go.transform.InverseTransformPoint(animator.GetBoneTransform(bones[i]).position);
     for(int i=0;i<array.arraySize;i++) { var c=(Collider)array.GetArrayElementAtIndex(i).objectReferenceValue; pose.blade[i]=go.transform.InverseTransformPoint(c.transform.position); }
     result.poses.Add(pose);
    }
   }
   File.WriteAllText("Artifacts/AKI123/ComboPoses.json",Newtonsoft.Json.JsonConvert.SerializeObject(new { poses = result.poses.Select(p => new { p.clip, p.time, p.bones, positions = p.positions.Select(v => new { v.x, v.y, v.z }), blade = p.blade.Select(v => new { v.x, v.y, v.z }) }) }, Newtonsoft.Json.Formatting.Indented));
  }
  finally { UnityEngine.Object.DestroyImmediate(go); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
 }
}
