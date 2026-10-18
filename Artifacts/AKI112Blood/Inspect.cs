using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectAKI112Blood
{
    static readonly float[] Times={0f,.05f,.15f,.3f,.5f,.75f,1f};
    static object Curve(ParticleSystem.MinMaxCurve c)=>new{mode=c.mode.ToString(),c.constantMin,c.constantMax,samples=Times.Select(t=>new{t,min=c.Evaluate(t,0f),max=c.Evaluate(t,1f)}).ToArray()};
    static float[] RGBA(Color c)=>new[]{c.r,c.g,c.b,c.a};
    static object Gradient(ParticleSystem.MinMaxGradient c)=>new{mode=c.mode.ToString(),samples=Times.Select(t=>new{t,min=RGBA(c.Evaluate(t,0f)),max=RGBA(c.Evaluate(t,1f))}).ToArray()};
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode inspection only");
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        var effects=new[]{c.FirstCFX,c.FinisherCFX}.Select(fx=>new{path=AssetDatabase.GetAssetPath(fx),duration=new SerializedObject(fx).FindProperty("_duration").floatValue,
            bias=new SerializedObject(fx).FindProperty("_bloodOutwardBias").floatValue,fx.UsesLocalContactFlash,
            particles=fx.GetComponentsInChildren<ParticleSystem>(true).Select(ps=>{var m=ps.main;var sh=ps.shape;var r=ps.GetComponent<ParticleSystemRenderer>();return new{
                ps.name,space=m.simulationSpace.ToString(),m.useUnscaledTime,m.loop,m.maxParticles,m.startSize3D,m.scalingMode,
                speed=Curve(m.startSpeed),size=Curve(m.startSize),life=Curve(m.startLifetime),delay=Curve(m.startDelay),color=Gradient(m.startColor),gravity=Curve(m.gravityModifier),
                sizeLife=new{ps.sizeOverLifetime.enabled,size=Curve(ps.sizeOverLifetime.size)},colorLife=new{ps.colorOverLifetime.enabled,color=Gradient(ps.colorOverLifetime.color)},
                velocity=ps.velocityOverLifetime.enabled,force=ps.forceOverLifetime.enabled,noise=ps.noise.enabled,limitVelocity=ps.limitVelocityOverLifetime.enabled,
                shape=sh.shapeType.ToString(),sh.angle,sh.radius,sh.length,sh.randomDirectionAmount,shapePosition=sh.position.ToString("R"),rotation=ps.transform.localEulerAngles.ToString("R"),localPosition=ps.transform.localPosition.ToString("R"),
                render=r.renderMode.ToString(),r.lengthScale,r.velocityScale,r.cameraVelocityScale,r.freeformStretching,r.rotateWithStretchDirection,r.minParticleSize,r.maxParticleSize,
                r.sortMode,material=AssetDatabase.GetAssetPath(r.sharedMaterial),
                bursts=Enumerable.Range(0,ps.emission.burstCount).Select(i=>{var b=ps.emission.GetBurst(i);return new{b.time,b.probability,b.cycleCount,b.repeatInterval,count=Curve(b.count)};}).ToArray()};}).ToArray()}).ToArray();
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat");
        var result=new{playing=EditorApplication.isPlaying,effects,material=new{shader=mat.shader.name,mat.renderQueue,ztest=mat.GetFloat("_ZTest"),gain=mat.GetFloat("_BloodGain"),tint=RGBA(mat.GetColor("_Tint"))}};
        File.WriteAllText("Artifacts/AKI112Blood/Before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        foreach(var fx in new[]{c.FirstCFX,c.FinisherCFX})foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            File.WriteAllText("Artifacts/AKI112Blood/Baseline/"+fx.name+"_"+ps.name+".json",EditorJsonUtility.ToJson(ps,true));
        return new{playing=EditorApplication.isPlaying,recorded="Artifacts/AKI112Blood/Before.json",material=result.material};
    }
}

