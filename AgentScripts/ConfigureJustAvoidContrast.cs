using System;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public static class ConfigureJustAvoidContrast {
 public static string Run() {
 if(EditorApplication.isPlaying || EditorUtility.scriptCompilationFailed) throw new Exception("Compiled Edit Mode required");
 var b=new StringBuilder(); var scene=EditorSceneManager.OpenPreviewScene("Assets/Mock/Scenes/GameScene.unity");
 float priority=100;
 try { foreach(var root in scene.GetRootGameObjects()) {
 foreach(var v in root.GetComponentsInChildren<Volume>(true)) { b.AppendLine("ExistingVolume="+v.name+" priority="+v.priority+" profile="+AssetDatabase.GetAssetPath(v.sharedProfile)); priority=Mathf.Max(priority,v.priority+1); }
 foreach(var c in root.GetComponentsInChildren<Canvas>(true)) b.AppendLine("Canvas="+c.name+" mode="+c.renderMode);
 foreach(var c in root.GetComponentsInChildren<HDAdditionalCameraData>(true)) b.AppendLine("Camera="+c.name+" mask="+c.volumeLayerMask.value);
 }}finally {EditorSceneManager.ClosePreviewScene(scene);}
 const string profilePath="Assets/Mock/ScriptableObjects/JustAvoidMonochrome.asset";
 var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
 if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,profilePath);}
 if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>(false);AssetDatabase.AddObjectToAsset(color,profile);}
 color.saturation.overrideState=true; color.saturation.value=-100; EditorUtility.SetDirty(color);EditorUtility.SetDirty(profile);
 const string path="Assets/Mock/Prefabs/Manager/CameraManager.prefab"; var g=PrefabUtility.LoadPrefabContents(path);
 try {
 var contrast=g.GetComponent<JustAvoidContrast>() ?? g.AddComponent<JustAvoidContrast>();
 var child=g.transform.Find("Just Avoid Monochrome Volume");
 if(child==null){var go=new GameObject("Just Avoid Monochrome Volume"); go.transform.SetParent(g.transform,false); child=go.transform;}
 child.gameObject.layer=0;
 var v=child.GetComponent<Volume>() ?? child.gameObject.AddComponent<Volume>();v.isGlobal=true;v.priority=priority;v.weight=0;v.sharedProfile=profile;
 var so=new SerializedObject(contrast);so.FindProperty("_volume").objectReferenceValue=v;so.FindProperty("_enter").floatValue=.02f;so.FindProperty("_hold").floatValue=.08f;so.FindProperty("_restore").floatValue=.20f;so.ApplyModifiedPropertiesWithoutUndo();
 PrefabUtility.SaveAsPrefabAsset(g,path,out bool saved);if(!saved)throw new Exception("Camera save failed");
 }finally{PrefabUtility.UnloadPrefabContents(g);}
 const string audioPath="Assets/Mock/Prefabs/Manager/AudioManager.prefab";g=PrefabUtility.LoadPrefabContents(audioPath);
 try{var so=new SerializedObject(g.GetComponent<AudioManager>());so.FindProperty("_justAvoidDuckVolume").floatValue=.25f;so.FindProperty("_justAvoidDuckEnter").floatValue=.03f;so.FindProperty("_justAvoidDuckHold").floatValue=.10f;so.FindProperty("_justAvoidDuckRestore").floatValue=.18f;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(g,audioPath,out bool saved);if(!saved)throw new Exception("Audio save failed");}finally{PrefabUtility.UnloadPrefabContents(g);}
 AssetDatabase.SaveAssets(); b.AppendLine("Saved dedicated profile, CameraManager and AudioManager prefabs. Priority="+priority);
 File.WriteAllText("AgentScripts/JustAvoidContrastConfiguration.txt",b.ToString()); return b.ToString();
 }
}
