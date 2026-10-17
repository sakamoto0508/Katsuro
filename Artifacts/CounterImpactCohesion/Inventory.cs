using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InventoryCounterImpactCohesion
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode inspection only");
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterImpactCohesion/Baseline/Config.json",EditorJsonUtility.ToJson(config,true));
        var effects=new[]{config.FirstCFX,config.FinisherCFX}.Select(fx=>new{path=AssetDatabase.GetAssetPath(fx),duration=new SerializedObject(fx).FindProperty("_duration").floatValue,
            particles=fx.GetComponentsInChildren<ParticleSystem>(true).Select(ps=>{var m=ps.main;var r=ps.GetComponent<ParticleSystemRenderer>();var sh=ps.shape;return new{
                ps.name,space=m.simulationSpace.ToString(),m.useUnscaledTime,m.duration,m.maxParticles,
                speed=new[]{m.startSpeed.constantMin,m.startSpeed.constantMax},size=new[]{m.startSize.constantMin,m.startSize.constantMax},
                life=new[]{m.startLifetime.constantMin,m.startLifetime.constantMax},delay=new[]{m.startDelay.constantMin,m.startDelay.constantMax},
                shape=sh.shapeType.ToString(),sh.angle,sh.radius,sh.randomDirectionAmount,sh.sphericalDirectionAmount,
                renderer=r.renderMode.ToString(),r.lengthScale,r.velocityScale,r.cameraVelocityScale,
                bursts=Enumerable.Range(0,ps.emission.burstCount).Select(i=>{var b=ps.emission.GetBurst(i);return new{b.time,b.minCount,b.maxCount};}).ToArray(),
                material=AssetDatabase.GetAssetPath(r.sharedMaterial),localPosition=ps.transform.localPosition.ToString("R")};}).ToArray()}).ToArray();
        var controllers=AssetDatabase.FindAssets("t:AnimatorController",new[]{"Assets/Mock"}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.IndexOf("Enemy",StringComparison.OrdinalIgnoreCase)>=0).Select(p=>{var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(p);
            return new{path=p,layers=c.layers.Select(l=>new{l.name,l.defaultWeight,l.blendingMode,mask=AssetDatabase.GetAssetPath(l.avatarMask),
                states=l.stateMachine.states.Select(s=>new{s.state.name,s.state.speed,s.state.speedParameter,s.state.speedParameterActive,motion=AssetDatabase.GetAssetPath(s.state.motion),length=(s.state.motion as AnimationClip)?.length}).ToArray()}).ToArray()};}).ToArray();
        var enemies=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Mock/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p,obj=AssetDatabase.LoadAssetAtPath<GameObject>(p)})
            .Where(x=>x.obj.GetComponentInChildren<EnemyAnimationController>(true)!=null).Select(x=>{var c=x.obj.GetComponentInChildren<EnemyAnimationController>(true);var a=c.GetComponent<Animator>();var so=new SerializedObject(c);return new{x.path,controller=AssetDatabase.GetAssetPath(a.runtimeAnimatorController),a.isHuman,
                counter=AssetDatabase.GetAssetPath(so.FindProperty("_counterPresentation").objectReferenceValue),heavyHitDuration=so.FindProperty("_heavyHitDuration").floatValue,blendOut=so.FindProperty("_reactionBlendOut").floatValue};}).ToArray();
        var result=new{playing=EditorApplication.isPlaying,effects,controllers,enemies};
        File.WriteAllText("Artifacts/CounterImpactCohesion/Inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
