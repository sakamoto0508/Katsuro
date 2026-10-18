using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;
public static class FinalAudit
{
 static string Hash(string p){using(var h=SHA256.Create())using(var f=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(f));}
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Requires compiled Edit Mode");
  var log=new StringBuilder("isPlaying=false isCompiling=false scriptCompilationFailed=false\n");
  foreach(string p in new[]{"Assets/Mock/Scenes/GameScene.unity","Assets/Mock/Prefabs/Enemy.prefab","Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/RegularCombo.asset","Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/HeavyCombo.asset","Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2RegularCombo.anim","Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2HeavyCombo.anim","Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig_ComboTest.asset"})
  {if(Hash(p)!=Hash("Temp/RushWarp-StartSnapshot/"+p))throw new Exception("Unexpected edit "+p);log.AppendLine("PASS task-start hash unchanged "+p);}
  foreach(string p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Mock/Animation/Enemy/SpecialAttacks/")&&p.EndsWith(".anim")))
  {var deps=AssetDatabase.GetDependencies(p,true);if(deps.Any(d=>d.StartsWith("Assets/InportAssets/RushImportWork")))throw new Exception("Temporary dependency "+p);log.AppendLine("PASS standalone clip "+p);}
  if(AssetDatabase.IsValidFolder("Assets/InportAssets/RushImportWork"))
  {if(!AssetDatabase.DeleteAsset("Assets/InportAssets/RushImportWork"))throw new Exception("Temporary import cleanup failed");log.AppendLine("PASS unreferenced candidate RM import copy removed");}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
  if(prefab.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Enemy prefab missing scripts");
  log.AppendLine("PASS Enemy prefab scripts resolve; user Scene override retained");
  var warp=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/WarpAttack.asset");
  var effects=warp.WarpEffectPrefab.GetComponentsInChildren<Component>(true);
  if(effects.Any(c=>c==null))throw new Exception("Smoke prefab missing script");
  log.AppendLine("INFO Smoke prefab components="+string.Join(",",effects.Select(c=>c.GetType().Name).Distinct()));
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var smoke=(GameObject)PrefabUtility.InstantiatePrefab(warp.WarpEffectPrefab,scene);
  try
  {
   smoke.transform.position=new Vector3(12,1,23);
   foreach(var binder in smoke.GetComponentsInChildren<INab.CommonVFX.VFXLossyTransformBinder>(true))
   {var v=binder.GetComponent<UnityEngine.VFX.VisualEffect>();if(binder.Target!=smoke.transform||!binder.IsValid(v))throw new Exception("Smoke transform binder invalid");binder.UpdateBinding(v);if(Vector3.Distance(v.GetVector3("Transform_position"),smoke.transform.position)>.001f)throw new Exception("Smoke position not bound");log.AppendLine("PASS smoke VFX bound to spawn transform");}
  }
  finally{UnityEngine.Object.DestroyImmediate(smoke);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/")&&p.EndsWith(".asset")))
  {if(AssetDatabase.LoadMainAssetAtPath(p)==null)throw new Exception("Missing special data "+p);log.AppendLine("PASS data "+p);}
  File.WriteAllText("Artifacts/RushWarp/FinalAudit.txt",log.ToString());
 }
}
