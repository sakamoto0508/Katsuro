using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;
public static class SilverSheetAssets {
 public static object Main(){ AssetDatabase.ImportAsset("Assets/Mock/Resources/JustAvoidCounterCut.shader",ImportAssetOptions.ForceSynchronousImport);
  foreach(bool second in new[]{false,true}){
   var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/"+(second?"JustAvoidCounterCut":"JustAvoidCounterFirstCut")+".mat");
   var names=new[]{"_CoreWidth","_CoreLength","_CoreGain","_ImpactBoost","_SheetWidth","_SheetIntensity","_SweepDuration","_LineHold","_FadeDuration","_ResidualGlowDuration","_FlashStrength","_FlashHold","_FlashDuration","_GlowWidth","_GlowIntensity","_TaperStrength"};
   var values=second?new[]{.28f,.98f,2.7f,1.3f,.48f,1.2f,.026f,.16f,.12f,.11f,2.7f,.012f,.08f,.48f,.2f,.85f}:new[]{.165f,.9f,1.75f,.6f,.25f,.5f,.018f,.03f,.095f,.075f,1.6f,.01f,.055f,.3f,.15f,.85f};
   for(int i=0;i<names.Length;i++)mat.SetFloat(names[i],values[i]);
   mat.SetColor("_SheetTint",second?new Color(1.15f,1.2f,1.3f,1):new Color(.92f,.97f,1.05f,1));EditorUtility.SetDirty(mat);
   // Remove obsolete absolute-end keys; the Inspector now exposes true durations.
   var so=new SerializedObject(mat);var floats=so.FindProperty("m_SavedProperties.m_Floats");for(int i=floats.arraySize-1;i>=0;i--){string n=floats.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue;if(n=="_LineDuration"||n=="_GlowDuration")floats.DeleteArrayElementAtIndex(i);}so.ApplyModifiedPropertiesWithoutUndo();
   string path="Assets/Mock/Effects/JustAvoidCounter"+(second?"Finisher":"FirstCut")+"CFX.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{var fx=new SerializedObject(root.GetComponent<JustAvoidCounterCutVFX>());fx.FindProperty("_cutSize").vector2Value=second?new Vector2(3.2f,1.12f):new Vector2(2.1f,.72f);fx.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();AssetDatabase.ImportAsset("Assets/Mock/Resources/JustAvoidCounterCut.shader",ImportAssetOptions.ForceSynchronousImport);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Mock/Resources/JustAvoidCounterCut.shader");
  var result=new{playing=EditorApplication.isPlaying,errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray(),compileMethods=typeof(ShaderUtil).GetMethods().Where(m=>m.Name=="CompilePass"||m.Name=="IsPassCompiled").Select(m=>m.ToString()).ToArray()};
  File.WriteAllText("Artifacts/CutSilverSheet/AssetUpdate.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }
}

