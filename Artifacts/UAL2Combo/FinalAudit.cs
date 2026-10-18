using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class FinalAudit
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  var log=new StringBuilder();
  log.AppendLine("isPlaying="+EditorApplication.isPlaying+" isCompiling="+EditorApplication.isCompiling+" scriptCompilationFailed="+EditorUtility.scriptCompilationFailed);
  if(EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)throw new Exception("Compilation not successful");
  const string work="Assets/InportAssets/UAL2EnemyImportWork";
  var clips=new[]{"Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2RegularCombo.anim","Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2HeavyCombo.anim"};
  foreach(var p in clips)
  {
   var deps=AssetDatabase.GetDependencies(p,true);
   log.AppendLine(p+" dependencies="+string.Join(",",deps));
   if(deps.Any(d=>d.StartsWith(work)))throw new Exception("Standalone clip still depends on working FBX");
  }
  if(AssetDatabase.IsValidFolder(work))
  {
   foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Mock/")&&!AssetDatabase.IsValidFolder(p)))
    if(AssetDatabase.GetDependencies(p,true).Any(d=>d.StartsWith(work)))throw new Exception("Working import referenced by "+p);
   if(!AssetDatabase.DeleteAsset(work))throw new Exception("Cannot remove working import");
   log.AppendLine("Removed unreferenced temporary RM importer copy; exported clips standalone.");
  }
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
   log.AppendLine("Open scene="+scene.path+" dirty="+scene.isDirty);
  }
  File.WriteAllText("Artifacts/UAL2Combo/FinalAudit.txt",log.ToString());
 }
}
