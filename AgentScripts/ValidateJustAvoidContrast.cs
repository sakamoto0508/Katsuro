using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public static class ValidateJustAvoidContrast {
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Near(float a,float b,string message){Check(Mathf.Abs(a-b)<.0001f,message+" "+a+" != "+b);}
 public static string Run(){
 Check(!EditorApplication.isPlaying && !EditorUtility.scriptCompilationFailed,"Compiled Edit Mode");
 var e=new JustAvoidEnvelope();e.Begin(10);Near(e.Evaluate(10.02f,.02f,.08f,.20f),1,"Monochrome peak");Near(e.Evaluate(10.1f,.02f,.08f,.20f),1,"Hold end");Near(e.Evaluate(10.2f,.02f,.08f,.20f),.5f,"Restore midpoint");Near(e.Evaluate(10.31f,.02f,.08f,.20f),0,"Auto restore");
 e.Begin(20);float current=e.Evaluate(20.2f,.02f,.08f,.20f);e.Begin(20.2f);Near(e.Evaluate(20.2f,.02f,.08f,.20f),current,"Repeat continuity");for(int i=0;i<1000;i++){e.Begin(21+i*.001f);float v=e.Evaluate(21+i*.001f,.02f,.08f,.20f);Check(v>=0&&v<=1,"No accumulation");}e.Reset();Near(e.Value,0,"Cancel");
 var audioGo=new GameObject("Duck validation");var clip=AudioClip.Create("silent test",100,1,44100,false);
 try{
 var audio=audioGo.AddComponent<AudioManager>();var sources=new List<AudioSource>();foreach(float v in new[]{.6f,.3f,.8f}){var src=audioGo.AddComponent<AudioSource>();src.volume=v;sources.Add(src);}
 typeof(AudioManager).GetField("bgmSources",flags).SetValue(audio,sources);
 var sfx=audioGo.AddComponent<AudioSource>();sfx.volume=.65f;typeof(AudioManager).GetField("_sfxPool",flags).SetValue(audio,new List<AudioSource>{sfx});float listenerVolume=AudioListener.volume;
 var duck=typeof(AudioManager).GetField("_duckEnvelope",flags);var update=typeof(AudioManager).GetMethod("UpdateDuck",flags);
 var env=new JustAvoidEnvelope();env.Begin(0);duck.SetValue(audio,env);update.Invoke(audio,new object[]{.04f});Near(sources[0].volume,.15f,"Channel 0 baseline x duck");Near(sources[1].volume,.075f,"Channel 1 baseline x duck");
 audio.SetBGMVolume(.4f,1);Near(sources[1].volume,.1f,"Set volume during duck");
 // PlayBGM performs the normal source selection/clip swap while preserving the multiplier.
 audio.PlayBGM(clip,2,.7f);Near(sources[2].volume,.175f,"Switch BGM during duck");sources[2].Stop();sources[2].clip=null;
 update.Invoke(audio,new object[]{.32f});Near(sources[0].volume,.6f,"Channel 0 restored");Near(sources[1].volume,.4f,"Changed baseline restored");Near(sources[2].volume,.7f,"Channel 2 restored");Check(sources[2].clip==null&&!sources[2].isPlaying,"Stopped BGM stays stopped");
 audio.PlayJustAvoidDuck();audio.CancelJustAvoidDuck();Near(sources[0].volume,.6f,"Explicit duck cancel");
 Near(sfx.volume,.65f,"SE pool untouched");Near(AudioListener.volume,listenerVolume,"Listener untouched");
 }finally{UnityEngine.Object.DestroyImmediate(audioGo);UnityEngine.Object.DestroyImmediate(clip);}
 var camera=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Manager/CameraManager.prefab");var contrast=camera.GetComponent<JustAvoidContrast>();Check(contrast!=null,"Contrast prefab component");var so=new SerializedObject(contrast);var volume=(Volume)so.FindProperty("_volume").objectReferenceValue;Check(volume!=null&&volume.isGlobal&&volume.gameObject.layer==0,"Dedicated global volume/mask");Near(volume.weight,0,"Normal weight");Near(volume.priority,100,"Priority");Check(volume.sharedProfile.components.Count==1,"Only ColorAdjustments");Check(volume.sharedProfile.TryGet<ColorAdjustments>(out var color),"Saturation component");Near(color.saturation.value,-100,"Full monochrome");Check(color.saturation.overrideState&&!color.postExposure.overrideState&&!color.contrast.overrideState&&!color.colorFilter.overrideState,"Only saturation override");
 var scene=EditorSceneManager.OpenPreviewScene("Assets/Mock/Scenes/GameScene.unity");int connected=0;try{foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<JustAvoidContrast>(true)){connected++;var v=(Volume)new SerializedObject(c).FindProperty("_volume").objectReferenceValue;Check(v!=null&&v.sharedProfile==volume.sharedProfile,"GameScene inherited correct profile");Near(v.weight,0,"GameScene default weight");}}finally{EditorSceneManager.ClosePreviewScene(scene);}Check(connected==1,"GameScene exactly one contrast owner");
 var instance=(GameObject)PrefabUtility.InstantiatePrefab(camera);try{var c=instance.GetComponent<JustAvoidContrast>();var v=instance.GetComponentInChildren<Volume>();c.Play(null);v.weight=.7f;c.Cancel();Near(v.weight,0,"Explicit volume cancel");v.weight=.5f;typeof(JustAvoidContrast).GetMethod("OnDisable",flags).Invoke(c,null);Near(v.weight,0,"Disable handler volume cancel");typeof(JustAvoidContrast).GetMethod("GameStateChanged",flags).Invoke(c,new object[]{GameManager.GameState.Victory});c.Play(null);var env=(JustAvoidEnvelope)typeof(JustAvoidContrast).GetField("_envelope",flags).GetValue(c);Check(!env.Playing,"Victory blocks restart");}finally{UnityEngine.Object.DestroyImmediate(instance);}
 var savedAudio=new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Manager/AudioManager.prefab").GetComponent<AudioManager>());Near(savedAudio.FindProperty("_justAvoidDuckVolume").floatValue,.25f,"Saved duck minimum");Near(savedAudio.FindProperty("_justAvoidDuckEnter").floatValue,.03f,"Saved duck enter");Near(savedAudio.FindProperty("_justAvoidDuckHold").floatValue,.10f,"Saved duck hold");Near(savedAudio.FindProperty("_justAvoidDuckRestore").floatValue,.18f,"Saved duck restore");
 AssetDatabase.SaveAssets();var result="PASS: unscaled envelope timing, 1000 repeats/no stacking, 3 BGM baselines, SetBGMVolume/PlayBGM during duck, exact recovery, stopped BGM not restarted, cancellation/disable, saved prefab/profile re-read, GameScene inherited settings. Compile errors: none. Play Mode: not run.";File.WriteAllText("AgentScripts/JustAvoidContrastTests.txt",result);return result;
 }
}
