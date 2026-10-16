using System;using System.Linq;using System.IO;using UnityEditor;using UnityEngine;
public static class CutPlateAssets {
 public static object Main(){
  AssetDatabase.ImportAsset("Assets/Mock/Resources/JustAvoidCounterCut.shader",ImportAssetOptions.ForceSynchronousImport);
  foreach(bool second in new[]{false,true}){
   var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/"+(second?"JustAvoidCounterCut":"JustAvoidCounterFirstCut")+".mat");
   string[] names={"_StrikeMultiplier","_CoreWidth","_CoreLength","_CoreGain","_ImpactBoost","_SheetWidth","_SheetLength","_SheetIntensity","_SheetSoftness","_ForwardExtension","_SweepDuration","_LineHold","_FadeDuration","_ResidualGlowDuration","_FlashStrength","_FlashHold","_FlashDuration","_TaperStrength","_BladeCurve","_GlowIntensity"};
   float[] values=second?new[]{1.35f,.32f,.92f,3.1f,1.25f,.64f,.99f,1.7f,.12f,.45f,.026f,.16f,.095f,.11f,2.5f,.008f,.06f,.7f,.2f,.16f}:new[]{1f,.18f,.88f,1.9f,.65f,.32f,.95f,.65f,.24f,.24f,.016f,.025f,.08f,.06f,1.7f,.006f,.045f,.95f,.09f,.12f};
   for(int i=0;i<names.Length;i++){if(!m.HasProperty(names[i]))throw new Exception("Missing shader property "+names[i]);m.SetFloat(names[i],values[i]);}
   m.SetVector("_FlashSize",second?new Vector4(.1f,.22f,0,0):new Vector4(.09f,.2f,0,0));
   m.SetColor("_Tint",second?new Color(2.1f,2.25f,2.45f,1):new Color(1.9f,2f,2.2f,1));
   m.SetColor("_SheetTint",second?new Color(1.15f,1.35f,1.7f,1):new Color(.85f,1.05f,1.4f,1));
   m.SetColor("_GlowTint",new Color(.45f,.7f,1f,1));EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
   string path="Assets/Mock/Effects/JustAvoidCounter"+(second?"Finisher":"FirstCut")+"CFX.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{var so=new SerializedObject(root.GetComponent<JustAvoidCounterCutVFX>());so.FindProperty("_cutSize").vector2Value=second?new Vector2(3.4f,1.2f):new Vector2(2.15f,.76f);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  var paths=new[]{"Assets/Mock/Effects/JustAvoidCounterFirstCut.mat","Assets/Mock/Effects/JustAvoidCounterCut.mat"};
  var materials=paths.Select(path=>{var m=AssetDatabase.LoadAssetAtPath<Material>(path);ShaderUtil.CompilePass(m,0,true);return new{path,compiled=ShaderUtil.IsPassCompiled(m,0),errors=ShaderUtil.GetShaderMessages(m.shader).Where(x=>x.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(x=>x.message).ToArray(),parameters=new[]{"_StrikeMultiplier","_CoreWidth","_CoreLength","_CoreGain","_SheetWidth","_SheetLength","_SheetIntensity","_SheetSoftness","_ForwardExtension","_FlashStrength","_FlashDuration","_LineHold","_FadeDuration","_ResidualGlowDuration"}.ToDictionary(n=>n,n=>m.GetFloat(n)),flashSize=m.GetVector("_FlashSize").ToString()};}).ToArray();
  var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");var result=new{playing=EditorApplication.isPlaying,materials,hitStop=new[]{config.First.HitStop,config.Second.HitStop}};
  File.WriteAllText("Artifacts/CutPlate/EditorVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}
