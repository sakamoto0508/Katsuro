using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class CheckUALCollision
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
  GameObject bodyObject=null,obstacle=null;
  var log=new System.Text.StringBuilder();
  try
  {
   bodyObject=new GameObject("enemy capsule");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bodyObject,scene);
   var body=bodyObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
   var capsule=bodyObject.AddComponent<CapsuleCollider>();capsule.radius=.38f;capsule.height=1.63f;capsule.center=new Vector3(0,.91f,0);
   foreach(bool player in new[]{false,true})
   {
    obstacle=new GameObject(player?"player body":"wall");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obstacle,scene);
    obstacle.transform.position=new Vector3(0,.9f,1.5f);
    if(player){var c=obstacle.AddComponent<CapsuleCollider>();c.radius=.3f;c.height=1.8f;}else{var c=obstacle.AddComponent<BoxCollider>();c.size=new Vector3(4,2,.2f);}
    Physics.SyncTransforms();
    var input=new Vector3(0,1,3);var output=AttackRootMotionPhysics.LimitDisplacement(body,input);
    log.AppendLine((player?"player":"wall")+" requested="+input+" limited="+output.ToString("F5"));
    if(output.z<=0||output.z>=1.2f||output.y!=0)throw new Exception("Sweep did not block "+(player?"player":"wall"));
    UnityEngine.Object.DestroyImmediate(obstacle);obstacle=null;
   }
  }
  finally
  {
   if(obstacle!=null)UnityEngine.Object.DestroyImmediate(obstacle);if(bodyObject!=null)UnityEngine.Object.DestroyImmediate(bodyObject);
   UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
   File.WriteAllText("Artifacts/UAL2Combo/CollisionAudit.txt",log.ToString());
  }
 }
}
