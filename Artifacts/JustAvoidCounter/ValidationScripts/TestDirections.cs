using System;
using UnityEngine;
using UnityEditor;
public static class CounterPoseDirections
{
 public static string Main(){var p=new PreviewRenderUtility();try{
 var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));p.AddSingleGO(root);foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
 var a=root.GetComponent<Animator>();a.fireEvents=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.Update(0);var r=root.GetComponent<EnemyAnimationController>();r.Init();
 foreach(var source in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})foreach(var stage in new[]{JustAvoidCounterStrike.First,JustAvoidCounterStrike.Second}){
 var attacker=new GameObject("direction");p.AddSingleGO(attacker);attacker.transform.position=root.transform.position+source;
 var info=new DamageInfo(1,root.transform.position+Vector3.up,source,attacker,null,slashDirection:Vector3.Cross(source,Vector3.up),isJustAvoidCounter:true,counterStrike:stage);
 r.TryPlayHitReaction(info);var transform=root.transform;var position=transform.position;var rotation=transform.rotation;
 for(int i=0;i<100;i++){typeof(EnemyAnimationController).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(r,null);}
 if((position-transform.position).sqrMagnitude>.000001f||Quaternion.Angle(rotation,transform.rotation)>.01f)throw new Exception("Root changed");
 float elapsed=0;bool ended=false;for(int i=0;i<50;i++){a.Update(.01f);elapsed+=.01f;ended=r.TickHitReaction(.01f);if(ended)break;}
 if(!ended||Mathf.Abs(elapsed-.48f)>.015f)throw new Exception("Gameplay hold duration changed: "+elapsed);
 r.CancelHitReaction();if(a.GetLayerWeight(1)!=0)throw new Exception("Cancel layer");
 }
 return "PASS both strikes x four directions; repeated pose application stable; Root unchanged; reaction control completes at original 0.48 seconds; cancel clears layer.";
 }finally{p.Cleanup();}}
}
