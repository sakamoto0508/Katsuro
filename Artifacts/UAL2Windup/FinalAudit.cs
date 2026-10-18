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
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Editor must be compiled in Edit Mode");
  var log=new StringBuilder("isPlaying=false isCompiling=false scriptCompilationFailed=false\n");
  foreach(var p in new[]{"Assets/Mock/Scenes/GameScene.unity","Assets/Mock/UI/KatsuroUIFont.asset","Assets/Mock/AnimationController/Enemy.controller","Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset","Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig_ComboTest.asset","Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2RegularCombo.anim","Assets/Mock/Animation/Enemy/UAL2Combos/EnemyUAL2HeavyCombo.anim"})
  {
   if(Hash(p)!=Hash("Temp/UAL2Windup-StartSnapshot/"+p))throw new Exception("Unexpected change "+p);
   log.AppendLine("PASS unchanged from task start: "+p);
  }
  // Player・JustAvoid・移動物理は今回の変更対象外。Pattern参照と新規Clip時刻設定は両方有効。
  foreach(var kind in new[]{"Regular","Heavy"})
  {
   var p=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/"+kind+"Combo.asset");
   if(!p.IsValid)throw new Exception("Invalid pattern "+kind);
   log.AppendLine("PASS "+kind+" duration="+p.WindupDuration+" prefix="+p.WindupEndTime+" windupSpeed="+p.WindupSpeed+" bodySpeed="+p.PlaybackSpeed+" rootScale="+p.RootMotionScale);
  }
  File.WriteAllText("Artifacts/UAL2Windup/FinalAudit.txt",log.ToString());
 }
}
