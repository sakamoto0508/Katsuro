using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEditor;
public class CounterPlayProbe5 : MonoBehaviour
{
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
 System.Collections.Generic.List<Tuple<Texture2D,string>> frames=new System.Collections.Generic.List<Tuple<Texture2D,string>>(); PlayerController p; EnemyController e; PlayerStateContext ctx; PlayerStateMachine sm; Animator pa; int hits; string root="Artifacts/JustAvoidCounter/PlayMode/Probe5"; float start;
 static void Button(UnityEngine.InputSystem.InputControl<float> c,bool down){using(UnityEngine.InputSystem.LowLevel.StateEvent.From(c.device,out var ev)){c.WriteValueIntoEvent(down?1f:0f,ev);InputSystem.QueueEvent(ev);}InputSystem.Update();}
 void Log(string s){File.AppendAllText(root+".log",Time.frameCount+" "+Time.realtimeSinceStartup.ToString("F3")+" "+s+"\n");}
 public static string Main(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");var go=new GameObject("Counter Play Mode Probe");DontDestroyOnLoad(go);go.AddComponent<CounterPlayProbe5>().StartCoroutine(go.GetComponent<CounterPlayProbe5>().Run());return "Play Mode probe started; log and actual Game View frames will be written under Artifacts/JustAvoidCounter/PlayMode.";}
 IEnumerator Run(){
  SceneManager.sceneLoaded+=OnScene; SceneManager.LoadScene("GameScene");yield return null;yield return null;
  p=GameManager.Instance.Player;e=UnityEngine.Object.FindFirstObjectByType<EnemyController>();ctx=(PlayerStateContext)Get(p,"_stateContext");sm=(PlayerStateMachine)Get(p,"_stateMachine");pa=p.GetComponent<Animator>();
  Log("playing="+EditorApplication.isPlaying+" tutorial="+GameManager.Instance.IsTutorial+" combat="+GameManager.Instance.IsCombatActive);
  e.HitReceived+=OnHit;p.JustAvoidSucceeded+=info=>Log("JUST AVOID succeeded");
  var origin=p.transform.position;var dir=Vector3.ProjectOnPlane(e.transform.position-origin,Vector3.up).normalized;
  e.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(origin+dir*1.4f);e.transform.rotation=Quaternion.LookRotation(-dir);p.transform.rotation=Quaternion.LookRotation(dir);
  ctx.LockOnCamera.LockOn();ctx.Attacker.DrawSword();yield return new WaitForSeconds(1.5f);
  Log("sword="+ctx.Attacker.IsSwordReady+" hp="+p.Hp);
  ctx.Ghost.TryBegin();sm.HandleGhostStarted();
  Log("ghost="+ctx.IsGhostMode+" justWindow="+ctx.IsInJustAvoidWindow);
  p.ApplyDamage(new DamageInfo(1,p.transform.position+Vector3.up, -dir,e.gameObject,p.GetComponent<Collider>()));
  yield return null;ctx.Ghost.End();sm.HandleGhostCanceled();
  yield return new WaitForSeconds(.08f);
  sm.ChangeState(PlayerStateId.Locomotion);sm.HandleLightAttack();
  start=Time.realtimeSinceStartup; bool queued=false;int old=0;
  while(Time.realtimeSinceStartup-start<4f){var state=pa.GetCurrentAnimatorStateInfo(0);if(state.fullPathHash!=old){Log("state="+state.fullPathHash+" tagCounter="+state.IsTag("JustAvoidCounter"));old=state.fullPathHash;}
   if(!queued&&state.IsTag("JustAvoidCounter")&&state.normalizedTime>.25f){sm.HandleLightAttack();queued=true;Log("queued next combo");}yield return null;}
  foreach(var frame in frames){File.WriteAllBytes(frame.Item2,frame.Item1.EncodeToPNG());Destroy(frame.Item1);}frames.Clear();Log("DONE hits="+hits);SceneManager.sceneLoaded-=OnScene;e.HitReceived-=OnHit;
 }
 void OnScene(Scene s,LoadSceneMode m){var g=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(g!=null)Set(g,"_isTutorial",true);}
 void OnHit(DamageInfo info){hits++;Log("HIT strike="+info.CounterStrike+" counter="+info.IsJustAvoidCounter+" damage="+info.DamageAmount+" point="+info.HitPoint+" sweep="+info.SlashDirection+" collider="+info.TargetCollider+"\n"+Environment.StackTrace);StartCoroutine(Capture(hits));}
 IEnumerator Capture(int hit){float began=Time.realtimeSinceStartup;for(int i=0;i<18;i++){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();frames.Add(Tuple.Create(tex,root+"-Hit"+hit+"-"+i.ToString("D2")+".png"));Log("FRAME hit="+hit+" index="+i+" age="+(Time.realtimeSinceStartup-began).ToString("F3")+" animatorSpeed="+pa.speed);}}
}



