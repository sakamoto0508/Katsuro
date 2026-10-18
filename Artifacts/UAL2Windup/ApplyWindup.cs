using System;
using UnityEngine;
using UnityEditor;
public static class ApplyWindup
{
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  foreach(string kind in new[]{"Regular","Heavy"})
  {
   var p=AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>("Assets/Mock/ScriptableObjects/Enemy/UAL2Combos/"+kind+"Combo.asset");
   p.PlaybackSpeed=.85f;p.WindupDuration=kind=="Regular"?.35f:.5f;p.WindupEndTime=kind=="Regular"?.15f:.25f;
   EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);
  }
 }
}
