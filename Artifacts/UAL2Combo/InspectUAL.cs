using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectUAL
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var log=new StringBuilder();
  foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("Universal Animation Library 2") && (p.EndsWith(".fbx") || p.EndsWith("Combo.anim"))))
  {
   log.AppendLine("ASSET "+path);
   if(AssetImporter.GetAtPath(path) is ModelImporter importer)log.AppendLine("rig="+importer.animationType+" motionNode="+importer.motionNodeName+" avatarSetup="+importer.avatarSetup);
   foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__") && c.name.Contains("Combo")))
   {
    var settings=AnimationUtility.GetAnimationClipSettings(clip);
    log.AppendLine("CLIP "+clip.name+" length="+clip.length+" human="+clip.humanMotion+" root="+clip.hasRootCurves+" speed="+clip.averageSpeed+" bakeXZ="+settings.loopBlendPositionXZ+" keepXZ="+settings.keepOriginalPositionXZ+" orientation="+settings.orientationOffsetY);
    foreach(var e in AnimationUtility.GetAnimationEvents(clip))log.AppendLine(" event "+e.time+" "+e.functionName+" int="+e.intParameter);
    foreach(var b in AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("RootT.") || (b.path.EndsWith("root")&&b.propertyName.Contains("Position"))))
    {
     var curve=AnimationUtility.GetEditorCurve(clip,b);if(curve==null||curve.length==0)continue;
     log.AppendLine(" curve "+b.path+" "+b.propertyName+" "+curve.keys[0].value+" -> "+curve.keys.Last().value);
    }
   }
  }
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");var a=prefab.GetComponent<Animator>();
  log.AppendLine("EnemyAvatar "+AssetDatabase.GetAssetPath(a.avatar)+" valid="+a.avatar.isValid+" human="+a.avatar.isHuman);
  var ctrl=(AnimatorController)a.runtimeAnimatorController;
  foreach(var layer in ctrl.layers) Walk(layer.stateMachine,layer.name,log);
  File.WriteAllText("Artifacts/UAL2Combo/InitialInspection.txt",log.ToString());
 }
 static void Walk(AnimatorStateMachine sm,string prefix,StringBuilder log)
 {
  foreach(var s in sm.states)log.AppendLine("STATE "+prefix+"."+s.state.name+" motion="+AssetDatabase.GetAssetPath(s.state.motion)+" behaviours="+string.Join(",",s.state.behaviours.Select(b=>b.GetType().Name)));
  foreach(var child in sm.stateMachines)Walk(child.stateMachine,prefix+"."+child.stateMachine.name,log);
 }
}
