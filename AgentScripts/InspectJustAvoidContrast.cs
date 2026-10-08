using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Text;
using System.IO;
public static class InspectJustAvoidContrast {
 public static string Run() {
 var b=new StringBuilder();
 b.AppendLine("Scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
 foreach(var v in Resources.FindObjectsOfTypeAll<Volume>()) { if(EditorUtility.IsPersistent(v)) continue; b.AppendLine("Volume "+v.name+" layer="+v.gameObject.layer+" priority="+v.priority+" weight="+v.weight+" profile="+AssetDatabase.GetAssetPath(v.sharedProfile)); }
 foreach(var c in Resources.FindObjectsOfTypeAll<Canvas>()) if(!EditorUtility.IsPersistent(c)) b.AppendLine("Canvas "+c.name+" mode="+c.renderMode);
 foreach(var c in Resources.FindObjectsOfTypeAll<HDAdditionalCameraData>()) if(!EditorUtility.IsPersistent(c)) b.AppendLine("Camera "+c.name+" volumeMask="+c.volumeLayerMask.value);
 foreach(var p in new[]{"Assets/Mock/Prefabs/Manager/CameraManager.prefab","Assets/Mock/Prefabs/Manager/AudioManager.prefab"}) { var g=AssetDatabase.LoadAssetAtPath<GameObject>(p); b.AppendLine(p); foreach(var c in g.GetComponentsInChildren<Component>(true)) if(c!=null) b.AppendLine(c.GetType().Name+" "+c.name); }
 var a=AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Mock/ScriptableObjects/AudioConfig.asset"); b.AppendLine("SuccessSE="+a.JustAvoidSound);
 File.WriteAllText("AgentScripts/JustAvoidContrastBefore.txt",b.ToString()); return b.ToString(); }
}
