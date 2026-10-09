using System;using System.Text;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class InspectVictorySaved {
 public static string Run(){var b=new StringBuilder();var scene=EditorSceneManager.OpenPreviewScene("Assets/Mock/Scenes/GameScene.unity");try{foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<FinalBlowPresentation>(true)){b.AppendLine("Scene="+p.gameObject.scene.path+" source="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p));b.AppendLine(EditorJsonUtility.ToJson(p));}}finally{EditorSceneManager.ClosePreviewScene(scene);}File.WriteAllText("AgentScripts/VictorySavedSettings.txt",b.ToString());return b.ToString();}
}
