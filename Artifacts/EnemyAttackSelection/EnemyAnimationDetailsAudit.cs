using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
public static class EnemyAnimationDetailsAudit
{
 public static void Run()
 {
  if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode only");
  var log = new StringBuilder();
  var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  try
  {
   foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.Contains("UAL2_Standard") && p.EndsWith(".fbx")))
   {
    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    var instance = UnityEngine.Object.Instantiate(model);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
    try
    {
     var root = instance.transform.Find("Armature/root");
     log.AppendLine(path + " Generic=" + ((ModelImporter)AssetImporter.GetAtPath(path)).animationType + " root=" + root);
     foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => c.name.Contains("Sword_Dash") || c.name.Contains("Sword_Regular_Combo") || c.name.Contains("Sword_Heavy_Combo") || c.name.Contains("Hit_Knockback")))
     {
      clip.SampleAnimation(instance, 0);
      var start = instance.transform.InverseTransformPoint(root.position);
      clip.SampleAnimation(instance, clip.length);
      var end = instance.transform.InverseTransformPoint(root.position);
      log.AppendLine(clip.name + " model-space root start=" + start.ToString("F5") + " end=" + end.ToString("F5") + " delta=" + (end-start).ToString("F5"));
     }
    }
    finally { UnityEngine.Object.DestroyImmediate(instance); }
   }
  }
  finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
  var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
  foreach (var layer in controller.layers) Walk(layer.stateMachine, layer.name, log);
  File.WriteAllText("Artifacts/EnemyAttackSelection/AnimationDetails.txt", log.ToString());
 }
 static void Walk(UnityEditor.Animations.AnimatorStateMachine machine, string path, StringBuilder log)
 {
  foreach(var entry in machine.states)
  {
   var state = entry.state;
   log.AppendLine(path+"/"+state.name+" tag="+state.tag+" motion="+(state.motion != null ? AssetDatabase.GetAssetPath(state.motion) : "none"));
   if(state.motion is AnimationClip clip) foreach(var e in AnimationUtility.GetAnimationEvents(clip)) log.AppendLine("  event " + e.time + " " + e.functionName);
  }
  foreach(var entry in machine.stateMachines) Walk(entry.stateMachine,path+"/"+entry.stateMachine.name,log);
 }
}
