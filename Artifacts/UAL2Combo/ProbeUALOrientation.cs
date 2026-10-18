using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class ProbeUALOrientation
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var log=new StringBuilder();
  foreach(string kind in new[]{"Regular","Heavy"})foreach(bool keep in new[]{false,true})foreach(float yaw in new[]{0f,180f})
  {
   var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   var clip=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2"+kind+"Combo.anim"));
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopBlendOrientation=true;settings.keepOriginalOrientation=keep;settings.orientationOffsetY=yaw;AnimationUtility.SetAnimationClipSettings(clip,settings);
   var controller=new AnimatorController();controller.AddLayer("Base Layer");var state=controller.layers[0].stateMachine.AddState("Probe");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;
   try
   {
    var a=go.GetComponent<Animator>();a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.Play("Base Layer.Probe",0,0);a.Update(0);
    var facing=a.GetBoneTransform(HumanBodyBones.LeftToes).position-a.GetBoneTransform(HumanBodyBones.LeftFoot).position;
    Vector3 sum=Vector3.zero;float rotation=0;
    for(int i=0;i<Mathf.RoundToInt(clip.length*60);i++){a.Update(1f/60f);sum+=a.deltaPosition;rotation+=Quaternion.Angle(Quaternion.identity,a.deltaRotation);}
    log.AppendLine(kind+" keep="+keep+" yaw="+yaw+" sum="+sum.ToString("F5")+" initial toeForward="+facing.ToString("F4")+" rootYawMotion="+rotation);
   }
   finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(controller);UnityEngine.Object.DestroyImmediate(clip);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  }
  File.WriteAllText("Artifacts/UAL2Combo/OrientationProbe.txt",log.ToString());
 }
}
