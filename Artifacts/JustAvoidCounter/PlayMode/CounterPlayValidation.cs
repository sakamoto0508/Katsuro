using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
// Diagnostic only: compiled through the Editor Pipeline, never included in the player.
public sealed class CounterPlayValidation : MonoBehaviour
{
 const string Root="Artifacts/JustAvoidCounter/PlayMode/Comparison";
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 PlayerController player;EnemyController enemy;PlayerStateContext context;PlayerStateMachine machine;Animator animator;
 readonly List<Tuple<Texture2D,string>> frames=new List<Tuple<Texture2D,string>>();
 string condition;string mode;int hitCount;int strike;int imageIndex;float hitAt;float captureUntil;bool recording;
 public static string Main(){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");Directory.CreateDirectory(Root);var go=new GameObject("Counter Play Mode Validation");DontDestroyOnLoad(go);var runner=go.AddComponent<CounterPlayValidation>();runner.StartCoroutine(runner.Run());return "Started actual Play Mode Game View comparison (existing weapon trigger contacts). See Comparison/Validation.log.";}
 void Log(string s)=>File.AppendAllText(Root+"/Validation.log",Time.frameCount+","+Time.realtimeSinceStartup.ToString("F4")+","+condition+","+s+"\n");
 IEnumerator Run(){
  SceneManager.sceneLoaded+=PrepareScene;
  StartCoroutine(CaptureFrames());
  foreach(string test in new[]{"Before-All","After-CutOnly","After-BloodOnly","After-All"}){
   condition=test;mode=test.Substring(test.IndexOf('-')+1);hitCount=0;recording=false;SceneManager.LoadScene("GameScene");yield return null;yield return null;
   player=GameManager.Instance.Player;enemy=UnityEngine.Object.FindFirstObjectByType<EnemyController>();context=(PlayerStateContext)Get(player,"_stateContext");machine=(PlayerStateMachine)Get(player,"_stateMachine");animator=player.GetComponent<Animator>();enemy.HitReceived+=OnHit;player.JustAvoidSucceeded+=OnAvoid;
   // Only in-memory CFX material instances are used for the depth comparison.
   foreach(var fx in UnityEngine.Object.FindObjectsByType<JustAvoidCounterCutVFX>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var r in fx.GetComponentsInChildren<Renderer>(true)){var m=r.material;if(m.HasProperty("_ZTest"))m.SetFloat("_ZTest",test.StartsWith("Before")?4:8);}
   Vector3 origin=player.transform.position;Vector3 dir=Vector3.ProjectOnPlane(enemy.transform.position-origin,Vector3.up).normalized;
   enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(origin+dir*1.2f);enemy.transform.rotation=Quaternion.LookRotation(-dir);player.transform.rotation=Quaternion.LookRotation(dir);context.LockOnCamera.LockOn();context.Attacker.DrawSword();yield return new WaitForSeconds(1.5f);
   Log("BEGIN playing="+EditorApplication.isPlaying+" camera="+Camera.main.transform.position+" player="+player.transform.position+" enemy="+enemy.transform.position+" screen="+Screen.width+"x"+Screen.height);
   // Schedule the incoming hit inside the existing Ghost window. Enemy hits are not used
   // as evidence of manual evasion; outgoing hits MUST come from WeaponHitboxRelay.
   context.Ghost.TryBegin();machine.HandleGhostStarted();player.ApplyDamage(new DamageInfo(1,player.transform.position+Vector3.up,-dir,enemy.gameObject,player.GetComponent<Collider>()));
   yield return null;context.Ghost.End();machine.HandleGhostCanceled();yield return new WaitForSeconds(.08f);machine.ChangeState(PlayerStateId.Locomotion);machine.HandleLightAttack();
   float began=Time.realtimeSinceStartup;bool queued=false;
   while(Time.realtimeSinceStartup-began<3.6f){var state=animator.GetCurrentAnimatorStateInfo(0);if(!queued&&hitCount>0&&state.IsTag(PlayerAttacker.JustAvoidCounterTag)&&state.normalizedTime>.25f){machine.HandleLightAttack();queued=true;Log("COMBO queued through existing state machine");}yield return null;}
   recording=false;enemy.HitReceived-=OnHit;player.JustAvoidSucceeded-=OnAvoid;
   Log("RESULT hits="+hitCount+" frames="+frames.Count);
   foreach(var f in frames){File.WriteAllBytes(f.Item2,f.Item1.EncodeToPNG());Destroy(f.Item1);}frames.Clear();yield return null;
  }
  SceneManager.sceneLoaded-=PrepareScene;Log("COMPLETE");
 }
 void PrepareScene(Scene s,LoadSceneMode m){var g=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(g!=null)Set(g,"_isTutorial",true);}
 void OnAvoid(DamageInfo d)=>Log("JUST_AVOID_SUCCESS (scheduled incoming diagnostic hit)");
 void OnHit(DamageInfo d){hitCount++;strike=(int)d.CounterStrike;imageIndex=0;hitAt=Time.realtimeSinceStartup;captureUntil=hitAt+.4f;recording=true;Log("HIT strike="+d.CounterStrike+" counter="+d.IsJustAvoidCounter+" amount="+d.DamageAmount+" point="+d.HitPoint+" sweep="+d.SlashDirection+" collider="+d.TargetCollider+"\n"+Environment.StackTrace);}
 void LateUpdate(){if(enemy==null)return;foreach(var fx in UnityEngine.Object.FindObjectsByType<JustAvoidCounterCutVFX>(FindObjectsSortMode.None)){foreach(var r in fx.GetComponentsInChildren<MeshRenderer>(true))r.forceRenderingOff=mode=="BloodOnly";foreach(var r in fx.GetComponentsInChildren<ParticleSystemRenderer>(true))r.forceRenderingOff=mode=="CutOnly";}
  foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.name=="Blade Trail Surface")r.forceRenderingOff=mode!="All";
 }
 IEnumerator CaptureFrames(){while(true){yield return new WaitForEndOfFrame();if(recording&&Time.realtimeSinceStartup<=captureUntil){var t=ScreenCapture.CaptureScreenshotAsTexture();string path=Root+"/"+condition+"-Strike"+strike+"-"+imageIndex.ToString("D2")+".png";frames.Add(Tuple.Create(t,path));Log("FRAME strike="+strike+" index="+imageIndex+" age="+(Time.realtimeSinceStartup-hitAt).ToString("F4")+" animatorSpeed="+animator.speed);imageIndex++;}}}
 void OnDestroy(){SceneManager.sceneLoaded-=PrepareScene;foreach(var f in frames)if(f.Item1!=null)Destroy(f.Item1);}
}
