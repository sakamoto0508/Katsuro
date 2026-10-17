using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class VerifyCounterImpactCohesion
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        Check(Mathf.Approximately(c.First.HitStop,.045f)&&Mathf.Approximately(c.Second.HitStop,.155f),"HitStop changed");
        Check(c.First.StrokeHitPeak>1&&c.Second.StrokeHitPeak>c.First.StrokeHitPeak,"Impact peak settings missing");
        var envelope=typeof(SwordTrail).Assembly.GetType("CounterHitPose",true).GetMethod("EvaluateEnvelope");
        int poseChecks=0;
        foreach(var s in new[]{c.First,c.Second})foreach(float delay in new[]{0f,s.FollowThroughPeak*.12f,s.FollowThroughPeak*.2f})
        {
            Func<float,Vector2> at=t=>(Vector2)envelope.Invoke(null,new object[]{t,s.PoseDuration,s.FollowThroughPeak,s.FollowThroughHold,delay});
            Check(at(0)==new Vector2(0,1),"Impact pose not present at contact");
            Check(at(s.FollowThroughPeak+delay).x>.999f&&at(s.FollowThroughPeak+delay).y>.999f,"Collapse peak not held");
            Check(at(s.PoseDuration).y<.00001f&&at(s.PoseDuration+.1f).y<.00001f,"Bone offset survives visible reaction");
            float previous=1f;
            for(int i=0;i<=100;i++)
            {
                var v=at(s.PoseDuration*i/100f);Check(v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1,"Envelope outside range");
                Check(v.y<=previous+.00001f,"Recovery reverses or overshoots");previous=v.y;poseChecks++;
            }
            Check(s.StrokePeakDuration>s.StrokeCoreHold||s==c.Second,"First impact duration shorter than core hold");
            Check(Mathf.Min(1f/30,s.StrokePeakDuration*.65f)>=1f/30-.00001f,"30fps peak plateau too short");
        }
        int bloodChecks=0;
        foreach(var slash in new[]{Vector3.right,Vector3.up,Vector3.forward,-Vector3.forward})foreach(var away in new[]{Vector3.forward,Vector3.right,Vector3.up})
        {
            var d=JustAvoidCounterCutVFX.ResolveBloodDirection(slash,away,.3f);
            Check(Vector3.Dot(d,slash)>.95f,"Blood reversed or over-rotated");
            Check(Mathf.Abs(d.magnitude-1)<.0001f,"Blood axis not normalized");bloodChecks++;
        }
        var effects=new[]{c.FirstCFX,c.FinisherCFX}.Select((fx,index)=>{
            var ps=fx.GetComponentsInChildren<ParticleSystem>(true);Check(ps.Length==3,"Particle emitter count changed");
            Check(fx.UsesLocalContactFlash,"Old large Cut quad enabled");
            var main=ps.Single(p=>p.name=="Crimson Threads");var late=ps.Single(p=>p.name=="Late Fine Drops");
            foreach(var p in ps)
            {
                Check(p.main.simulationSpace==ParticleSystemSimulationSpace.World&&p.main.useUnscaledTime&&!p.main.loop,"Particle clock or space changed");
                Check(p.transform.localPosition==Vector3.zero&&p.shape.position==Vector3.zero,"Blood origin moved");
                var duration=new SerializedObject(fx).FindProperty("_duration").floatValue;
                Check(p.main.startDelay.constantMax+p.main.startLifetime.constantMax<=duration,"Pool cuts particle life short");
            }
            Check(main.main.startDelay.constantMax==0,"Main streak delayed from hit");
            Check(main.emission.GetBurst(0).count.constant==(index==0?5:7),"Main streak count incorrect");
            Check(late.main.startDelay.constantMin>0,"Droplets not separated in time");
            return new{path=AssetDatabase.GetAssetPath(fx),streakCount=main.emission.GetBurst(0).count.constant,
                speed=new[]{main.main.startSpeed.constantMin,main.main.startSpeed.constantMax},size=new[]{main.main.startSize.constantMin,main.main.startSize.constantMax},
                spread=main.shape.angle,lateDelay=new[]{late.main.startDelay.constantMin,late.main.startDelay.constantMax}};
        }).ToArray();
        var mats=new[]{c.First.SlashMaterial,c.Second.SlashMaterial,c.First.StrokeMaterial,c.Second.StrokeMaterial,
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat")}.Select(m=>{
            Check(m!=null,"Missing Material");ShaderUtil.CompilePass(m,0,true);var messages=ShaderUtil.GetShaderMessages(m.shader);
            Check(ShaderUtil.IsPassCompiled(m,0)&&!messages.Any(x=>x.severity.ToString()=="Error"),"Shader compile failed");
            return new{path=AssetDatabase.GetAssetPath(m),m.renderQueue,compiled=true,messages=messages.Select(x=>new{x.message,x.line,severity=x.severity.ToString()}).ToArray()};}).ToArray();
        Check(c.First.StrokeMaterial.HasProperty("_HitPeak")&&c.Second.StrokeMaterial.HasProperty("_HitPeak"),"Shader peak binding missing");
        var result=new{playing=EditorApplication.isPlaying,poseChecks,bloodChecks,effects,mats,scope="Asset APIs and pure functions only. No Scene, Play Mode, visual evaluation or runtime Animator validation."};
        File.WriteAllText("Artifacts/CounterImpactCohesion/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
