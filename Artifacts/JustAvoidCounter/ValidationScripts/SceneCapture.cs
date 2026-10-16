using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
public static class CounterSceneCapture
{
 public static string Main()
 {
  var playerScene=UnityEngine.Object.FindFirstObjectByType<PlayerController>();var enemyScene=UnityEngine.Object.FindFirstObjectByType<EnemyController>();
  if(playerScene==null||enemyScene==null)throw new Exception("GameScene characters required");
  var originals=new System.Collections.Generic.Dictionary<Renderer,bool>();
  var owned=new System.Collections.Generic.List<GameObject>();
  var clock=typeof(CombatFeedback).Assembly.GetType("FeedbackVisualTime");var clockElapsed=clock.GetField("_elapsed",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);float savedClock=(float)clockElapsed.GetValue(null); var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGBHalf);target.Create();
  var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
  var cameraConfig=AssetDatabase.LoadAssetAtPath<CameraConfig>("Assets/Mock/ScriptableObjects/GameSceneCameraConfig.asset");
  try {
   foreach(var original in new[]{playerScene.gameObject,enemyScene.gameObject})foreach(var r in original.GetComponentsInChildren<Renderer>(true)){originals[r]=r.forceRenderingOff;r.forceRenderingOff=true;}
   var p=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab"));owned.Add(p);
   var e=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));owned.Add(e);
   foreach(var go in new[]{p,e}){foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;}
   e.transform.SetPositionAndRotation(enemyScene.transform.position,Quaternion.Euler(0,180,0));p.transform.SetPositionAndRotation(e.transform.position-Vector3.forward*1.25f,Quaternion.identity);
   Vector3 attackerPosition=p.transform.position; var pc=new SerializedObject(p.GetComponent<PlayerController>());((GameObject)pc.FindProperty("_playerWeapon").objectReferenceValue).SetActive(true);((GameObject)pc.FindProperty("_playerStartWeapon").objectReferenceValue).SetActive(false);
   var pa=p.GetComponent<Animator>();var ea=e.GetComponent<Animator>();foreach(var a in new[]{pa,ea}){a.fireEvents=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=false;a.Rebind();a.Update(0);}
   var reaction=e.GetComponent<EnemyAnimationController>();reaction.Init();
   var goCamera=new GameObject("Counter validation camera",typeof(Camera),typeof(HDAdditionalCameraData));owned.Add(goCamera);var camera=goCamera.GetComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
   var mainData=Camera.main.GetComponent<HDAdditionalCameraData>();if(mainData!=null)EditorUtility.CopySerialized(mainData,goCamera.GetComponent<HDAdditionalCameraData>());
   camera.targetTexture=target;
   Directory.CreateDirectory("Artifacts/JustAvoidCounter/Scene");
   for(int stage=1;stage<=2;stage++)for(int dist=0;dist<2;dist++){
    ea.Rebind();ea.Update(0);var chest=ea.GetBoneTransform(HumanBodyBones.Chest);Vector3 point=chest.position-Vector3.forward*.18f;
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Mock/Animation/Player/JustVoidAttack/ARPG_Samurai_Attack_Heavy"+stage+".anim");
    clip.SampleAnimation(p,stage==1?.20f:.34f);p.transform.SetPositionAndRotation(attackerPosition,Quaternion.identity);
    var blade=(Collider)new SerializedObject(p.GetComponent<PlayerController>()).FindProperty("_weaponColliders").GetArrayElementAtIndex(0).objectReferenceValue;
    Vector3 before=blade.transform.position;clip.SampleAnimation(p,stage==1?.2333f:.3733f);p.transform.SetPositionAndRotation(attackerPosition,Quaternion.identity);Vector3 slash=(blade.transform.position-before).normalized;
    if(slash.sqrMagnitude<.001f)slash=new Vector3(1,.3f,0).normalized;
    point=enemyScene.GetComponent<Collider>().ClosestPoint(chest.position-Vector3.forward*1.5f);var trail=blade.GetComponent<SwordTrail>();if(trail!=null){trail.Init();trail.SetStyle(SwordTrail.AttackStyle.JustAvoidCounter,(JustAvoidCounterStrike)stage);var shared=new SerializedObject(trail).FindProperty("_sharedTrail").objectReferenceValue as SwordTrail;if(shared!=null)trail=shared;var surface=(GameObject)typeof(SwordTrail).GetField("surface",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(trail);if(surface!=null&&!owned.Contains(surface))owned.Add(surface);trail.SetActive(true);float enabledTime=stage==1?.183333f:.316667f;for(int sample=0;sample<5;sample++){float sampleTime=enabledTime+sample/60f;clockElapsed.SetValue(null,savedClock+sample/60f);clip.SampleAnimation(p,sampleTime);p.transform.SetPositionAndRotation(attackerPosition,Quaternion.identity);typeof(SwordTrail).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(trail,null);}trail.SetActive(false);} var info=new DamageInfo(1,point,Vector3.back,p,null,isJustAvoidCounter:true,counterStrike:(JustAvoidCounterStrike)stage);reaction.TryPlayHitReaction(info);
    var cfx=UnityEngine.Object.Instantiate(config.GetCFX((JustAvoidCounterStrike)stage));owned.Add(cfx.gameObject);
    Vector3 forward=Vector3.forward;float distance=dist==0?cameraConfig.CameraDistance*.7f:cameraConfig.CameraDistance;
    camera.transform.position=p.transform.position-forward*distance+Vector3.up*cameraConfig.CameraHeight+Vector3.right*cameraConfig.LockOnSideOffset;
    camera.transform.LookAt(e.transform.position+Vector3.up*cameraConfig.LookAtHeight);
    float previous=0; float trailClock=(float)clockElapsed.GetValue(null);
    foreach(float age in new[]{.033333f,.1f,.233333f}){
     clockElapsed.SetValue(null,trailClock+age);if(trail!=null)typeof(SwordTrail).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(trail,null);float elapsed=Mathf.Max(0,age-config.GetStrike((JustAvoidCounterStrike)stage).HitStop);float delta=elapsed-previous;
     ea.Update(delta);reaction.TickHitReaction(delta);typeof(EnemyAnimationController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(reaction,null);previous=elapsed;
     cfx.Play(point,slash,Vector3.back,camera);var r=cfx.GetComponentInChildren<MeshRenderer>(true);var block=new MaterialPropertyBlock();block.SetFloat("_Age",age);r.SetPropertyBlock(block);foreach(var ps in cfx.GetComponentsInChildren<ParticleSystem>(true))ps.Simulate(age,false,true,true);
     var request=new RenderPipeline.StandardRequest{destination=target};RenderPipeline.SubmitRenderRequest(camera,request);
     var active=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();RenderTexture.active=active;
     File.WriteAllBytes("Artifacts/JustAvoidCounter/Scene/Strike"+stage+"-"+(dist==0?"Near":"LockOn")+"-"+Mathf.RoundToInt(age*1000)+"ms.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);cfx.Stop();
    }
    reaction.CancelHitReaction();
   }
   return "Rendered 12 GameScene frames using actual environment, character prefabs, sampled attack and reaction poses, and existing lock-on composition. Edit Mode samples, not live combat or full motion recordings.";
  }finally{clockElapsed.SetValue(null,savedClock);foreach(var entry in originals)if(entry.Key!=null)entry.Key.forceRenderingOff=entry.Value;foreach(var go in owned)if(go!=null)UnityEngine.Object.DestroyImmediate(go);target.Release();UnityEngine.Object.DestroyImmediate(target);}
 }
}


