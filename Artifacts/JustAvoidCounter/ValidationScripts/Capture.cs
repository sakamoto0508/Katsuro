using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class CounterCapture
{
 static BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
 public static string Main()
 {
  string version="Recapture";
  string folder="Temp/CounterV2/"+version;Directory.CreateDirectory(folder);
  var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
  var vfx=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
  int count=0;
  for(int stage=0;stage<3;stage++)
  {
   var preview=new PreviewRenderUtility();
   CombatFeedback feedback=null;object pulse=null;
   try {
    var holder=new GameObject("Counter capture");preview.AddSingleGO(holder);
    JustAvoidCounterCutVFX cut=null;
    var firstField=typeof(JustAvoidCounterConfig).GetField("FirstCFX");
    var cutPrefab=stage==1?config.FinisherCFX:stage==0&&firstField!=null?(JustAvoidCounterCutVFX)firstField.GetValue(config):null;
    GameObject[] roots=null;
    if(cutPrefab!=null){cut=UnityEngine.Object.Instantiate(cutPrefab);preview.AddSingleGO(cut.gameObject);}
    else {
     feedback=holder.AddComponent<CombatFeedback>();feedback.enabled=false;Set(feedback,"_vfxConfig",vfx);
     typeof(CombatFeedback).GetMethod("PrepareContactFeedback",Flags).Invoke(feedback,null);
     roots=Get<GameObject[]>(feedback,"_hitRoots");foreach(var root in roots)preview.AddSingleGO(root);
     var cuts=Get<JustAvoidCounterCutVFX[]>(feedback,"_counterCuts");if(cuts!=null)foreach(var c in cuts)preview.AddSingleGO(c.gameObject);
     var firstCuts=Get<JustAvoidCounterCutVFX[]>(feedback,"_counterFirstCuts");if(firstCuts!=null)foreach(var c in firstCuts)preview.AddSingleGO(c.gameObject); pulse=Get<object>(feedback,"_contactPulse");
     var objects=Get<GameObject[]>(pulse,"objects");if(objects!=null)foreach(var go in objects)preview.AddSingleGO(go);
    }
    var camera=preview.camera;camera.fieldOfView=50;camera.nearClipPlane=.05f;camera.farClipPlane=20;
    camera.clearFlags=CameraClearFlags.SolidColor;
    Vector3 point=new Vector3(0,1.15f,0),slash=new Vector3(1,.28f,.03f).normalized;
    for(int bg=0;bg<3;bg++)for(int distance=0;distance<2;distance++)for(int fpsIndex=0;fpsIndex<2;fpsIndex++)for(int mode=0;mode<3;mode++)
    {
     camera.backgroundColor=bg==0?new Color(.55f,.6f,.62f,1):bg==1?new Color(.012f,.018f,.022f,1):new Color(.24f,.035f,.048f,1);
     camera.transform.position=new Vector3(distance==0?.2f:.45f,1.35f,distance==0?-2.7f:-4.6f);camera.transform.LookAt(point);
     int fps=fpsIndex==0?30:60;
     foreach(int frame in new[]{1,3,6,10}) {
      float age=frame/(float)fps;
      if(cut!=null){cut.Play(point,slash,Vector3.back,camera);var renderer=cut.GetComponentInChildren<MeshRenderer>(true);var block=new MaterialPropertyBlock();block.SetFloat("_Age",age);renderer.SetPropertyBlock(block);renderer.enabled=mode!=1;foreach(var ps in cut.GetComponentsInChildren<ParticleSystem>(true)){ps.Simulate(age,false,true,true);ps.GetComponent<ParticleSystemRenderer>().enabled=mode!=0;}}
      else {
       var info=new DamageInfo(1,point,Vector3.back,null,null,isHeavy:stage==2,slashDirection:slash,isJustAvoidCounter:stage==0,counterStrike:stage==0?JustAvoidCounterStrike.First:JustAvoidCounterStrike.None);
       typeof(CombatFeedback).GetMethod("PlayHitVFX",Flags).Invoke(feedback,new object[]{info});
       foreach(var root in roots)foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true)) {ps.Simulate(age,false,true,true);ps.GetComponent<ParticleSystemRenderer>().enabled=mode==2||(mode==0?ps.name=="SlashLine":ps.name!="SlashLine");}
      }
      preview.BeginStaticPreview(new Rect(0,0,640,360));preview.Render(true,true);var texture=preview.EndStaticPreview();
      File.WriteAllBytes(folder+"/S"+stage+"-B"+bg+"-D"+distance+"-F"+fps+"-M"+mode+"-T"+frame+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);count++;
      if(cut!=null)cut.Stop();
     }
    }
   } finally {
    if(feedback!=null){Set(feedback,"_hitRoots",null);Set(feedback,"_counterCuts",null);Set(feedback,"_counterFirstCuts",null);Set(feedback,"_contactPulse",null);}
    if(pulse!=null){var mat=Get<Material>(pulse,"material");var mesh=Get<Mesh>(pulse,"mesh");if(mat!=null)UnityEngine.Object.DestroyImmediate(mat);if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
    preview.Cleanup();
   }
  }
  return version+" controlled CFX frames="+count+"; backgrounds are visibility proxies, not live gameplay.";
 }
}


