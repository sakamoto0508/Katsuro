using UnityEngine;
using UnityEditor;
using System;
using System.IO;
public static class CounterVisibilityPlayTuning {
 public static string Main(){
  foreach(string path in new[]{"Assets/Mock/Effects/JustAvoidCounterFirstCut.mat","Assets/Mock/Effects/JustAvoidCounterCut.mat","Assets/Mock/Effects/JustAvoidCounterBlood.mat"}){var m=AssetDatabase.LoadAssetAtPath<Material>(path);m.SetFloat("_ZTest",8f);EditorUtility.SetDirty(m);}
  string report="";
  foreach(bool second in new[]{false,true}){string path="Assets/Mock/Effects/JustAvoidCounter"+(second?"Finisher":"FirstCut")+"CFX.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{
   foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;var shape=ps.shape;var r=ps.GetComponent<ParticleSystemRenderer>();report+=root.name+"/"+ps.name+" before size="+main.startSize.constantMin+".."+main.startSize.constantMax+" life="+main.startLifetime.constantMin+".."+main.startLifetime.constantMax+" space="+main.simulationSpace+" renderer="+r.renderMode+"\n";
    if(ps.name.Contains("Crimson")){main.startSize=new ParticleSystem.MinMaxCurve(second?.10f:.085f,second?.15f:.12f);main.startLifetime=new ParticleSystem.MinMaxCurve(second?.34f:.28f,second?.45f:.36f);main.startSpeed=new ParticleSystem.MinMaxCurve(second?6.3f:5f,second?7.8f:6.2f);shape.angle=second?18f:14f;}
    else if(ps.name.Contains("Dark")){main.startSize=new ParticleSystem.MinMaxCurve(second?.075f:.06f,second?.12f:.09f);}
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}}
  AssetDatabase.SaveAssets();File.WriteAllText("Artifacts/JustAvoidCounter/PlayMode/AssetTuning.txt",report);return report;
 }
}
