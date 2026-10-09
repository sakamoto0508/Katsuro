using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.Text;
using System.IO;
public static class InspectVictory {
 static void States(AnimatorStateMachine sm,StringBuilder b){foreach(var s in sm.states){if(s.state.name.ToLower().Contains("dead")||s.state.name.ToLower().Contains("sheath")){b.AppendLine("State "+s.state.name+" speed="+s.state.speed+" clip="+AssetDatabase.GetAssetPath(s.state.motion));foreach(var t in s.state.transitions)b.AppendLine(" Exit="+t.hasExitTime+" time="+t.exitTime+" duration="+t.duration);}foreach(var t in sm.anyStateTransitions)if(t.destinationState==s.state)b.AppendLine("Any->"+s.state.name+" duration="+t.duration);}foreach(var child in sm.stateMachines)States(child.stateMachine,b);}
 public static string Run(){var b=new StringBuilder();var scene=EditorSceneManager.OpenPreviewScene("Assets/Mock/Scenes/GameScene.unity");try{foreach(var root in scene.GetRootGameObjects()){
 foreach(var c in root.GetComponentsInChildren<FinalBlowManager>(true))b.AppendLine("FinalBlow "+EditorJsonUtility.ToJson(c));
 foreach(var c in root.GetComponentsInChildren<FinalBlowPresentation>(true))b.AppendLine("Presentation "+EditorJsonUtility.ToJson(c));
 foreach(var c in root.GetComponentsInChildren<Unity.Cinemachine.CinemachineBrain>(true))b.AppendLine("Brain "+EditorJsonUtility.ToJson(c));
 }}finally{EditorSceneManager.ClosePreviewScene(scene);}
 foreach(var path in new[]{"Assets/Mock/AnimationController/Player.controller","Assets/Mock/AnimationController/Enemy.controller"}){var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);foreach(var layer in controller.layers)States(layer.stateMachine,b);}
 foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip")){var path=AssetDatabase.GUIDToAssetPath(guid);foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path)){var clip=obj as AnimationClip;if(clip==null||!clip.name.ToLower().Contains("sheath"))continue;b.AppendLine("Sheath clip "+clip.name+" length="+clip.length);foreach(var e in AnimationUtility.GetAnimationEvents(clip))b.AppendLine(" Event "+e.time+" "+e.functionName+" "+e.stringParameter);}}
 var config=AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Mock/ScriptableObjects/AudioConfig.asset");b.AppendLine("EnemyDeadSound="+config.EnemyDeadSound);
 var audio=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Manager/AudioManager.prefab");b.AppendLine("Audio="+EditorJsonUtility.ToJson(audio.GetComponent<AudioManager>()));
 File.WriteAllText("AgentScripts/VictoryBefore.txt",b.ToString());return b.ToString();}
}
