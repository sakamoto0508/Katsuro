using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectBloodPlacementBalance
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
        var normal=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Effects/SwordHitVFX.prefab");
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var effects=new[]{normal,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab")}.Select(root=>new{
            path=AssetDatabase.GetAssetPath(root),rootActiveSelf=root.activeSelf,particles=root.GetComponentsInChildren<ParticleSystem>(true).Select(ps=>{var m=ps.main;var r=ps.GetComponent<ParticleSystemRenderer>();return new{
                ps.name,activeSelf=ps.gameObject.activeSelf,parent=ps.transform.parent.name,localPosition=ps.transform.localPosition.ToString("R"),localScale=ps.transform.localScale.ToString("R"),
                speed=new[]{m.startSpeed.constantMin,m.startSpeed.constantMax},size=new[]{m.startSize.constantMin,m.startSize.constantMax},life=new[]{m.startLifetime.constantMin,m.startLifetime.constantMax},
                delay=new[]{m.startDelay.constantMin,m.startDelay.constantMax},space=m.simulationSpace.ToString(),m.playOnAwake,stop=m.stopAction.ToString(),
                angle=ps.shape.angle,r.lengthScale,r.velocityScale,pivot=r.pivot.ToString("R"),r.freeformStretching,
                material=AssetDatabase.GetAssetPath(r.sharedMaterial),shader=r.sharedMaterial!=null?r.sharedMaterial.shader.name:"NULL",
                count=Enumerable.Range(0,ps.emission.burstCount).Select(i=>ps.emission.GetBurst(i).count.constant).ToArray()};}).ToArray()}).ToArray();
        var result=new{playing=EditorApplication.isPlaying,effects,colliders=enemy.GetComponentsInChildren<CapsuleCollider>(true).Select(c=>new{c.name,c.center,c.radius,c.height,c.direction}).Select(x=>new{x.name,center=x.center.ToString("R"),x.radius,x.height,x.direction}).ToArray()};
        File.WriteAllText("Artifacts/BloodPlacementBalance/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
