using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class VerifyAKI112Blood
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        Check(Mathf.Approximately(c.First.HitStop,.045f)&&Mathf.Approximately(c.Second.HitStop,.155f),"HitStop changed");
        Check(typeof(SwordTrail).GetMethod("NotifyCounterHit").ReturnType==typeof(Vector3),"Measured direction return not loaded");
        var effects=new[]{c.FirstCFX,c.FinisherCFX}.Select((fx,index)=>{
            var ps=fx.GetComponentsInChildren<ParticleSystem>(true);Check(ps.Length==3,"Emitter count changed");
            var refs=new SerializedObject(fx).FindProperty("_blood");Check(refs.arraySize==3,"Pool emitter references missing");
            for(int i=0;i<refs.arraySize;i++)Check(refs.GetArrayElementAtIndex(i).objectReferenceValue!=null,"Null emitter reference");
            var duration=new SerializedObject(fx).FindProperty("_duration").floatValue;
            Check(fx.UsesLocalContactFlash,"Old Quad enabled");
            var particles=ps.Select(p=>{var m=p.main;var size=p.sizeOverLifetime.size;var r=p.GetComponent<ParticleSystemRenderer>();
                Check(m.simulationSpace==ParticleSystemSimulationSpace.World&&m.useUnscaledTime&&!m.loop,"Clock or space changed");
                Check(p.transform.localPosition==Vector3.zero&&p.shape.position==Vector3.zero,"Hit origin moved");
                Check(m.startDelay.constantMax+m.startLifetime.constantMax<duration,"Pool truncates particle lifetime");
                Check(r.sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat"),"Blood material reference changed");
                Check(p.colorOverLifetime.color.Evaluate(1).a==0,"Particle alpha does not vanish");
                if(p.name=="Crimson Threads")
                {
                    Check(m.startDelay.constantMax==0,"Main streak delayed");
                    Check(p.emission.GetBurst(0).count.constant==(index==0?5:7),"Main particle count changed");
                    Check(size.Evaluate(.15f)>1.1f&&size.Evaluate(.48f)>.99f&&size.Evaluate(1)<.061f,"Main envelope invalid");
                    for(int i=0;i<=100;i++)Check(size.Evaluate(i/100f)>=0&&size.Evaluate(i/100f)<=1.121f,"Size overshoot");
                }
                return new{p.name,speed=new[]{m.startSpeed.constantMin,m.startSpeed.constantMax},size=new[]{m.startSize.constantMin,m.startSize.constantMax},
                    lifetime=new[]{m.startLifetime.constantMin,m.startLifetime.constantMax},delay=new[]{m.startDelay.constantMin,m.startDelay.constantMax},
                    sizeAtLife=new[]{0f,.15f,.48f,.8f,1f}.Select(t=>new{t,value=size.Evaluate(t)}).ToArray(),count=p.emission.GetBurst(0).count.constant,p.shape.angle,r.lengthScale,r.velocityScale};
            }).ToArray();return new{path=AssetDatabase.GetAssetPath(fx),duration,particles};
        }).ToArray();
        var blood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat");
        Check(Mathf.Approximately(blood.GetFloat("_BloodGain"),.4f)&&blood.GetFloat("_ZTest")==8,"Blood brightness or depth changed");
        Check(blood.GetFloat("_Opacity")==1&&Mathf.Approximately(blood.GetFloat("_ContourVariation"),.65f),"Blood shape controls missing");
        var mats=new[]{blood,c.First.SlashMaterial,c.Second.SlashMaterial,c.First.StrokeMaterial,c.Second.StrokeMaterial}.Select(m=>{
            ShaderUtil.CompilePass(m,0,true);var messages=ShaderUtil.GetShaderMessages(m.shader);Check(ShaderUtil.IsPassCompiled(m,0)&&!messages.Any(x=>x.severity.ToString()=="Error"),"Shader compile failed");
            return new{path=AssetDatabase.GetAssetPath(m),m.renderQueue,compiled=true,messages=messages.Select(x=>new{x.message,x.line,severity=x.severity.ToString()}).ToArray()};}).ToArray();
        var result=new{playing=EditorApplication.isPlaying,effects,mats,scope="Asset APIs, shader compiler and numeric curve inspection only. No Play Mode or visual test."};
        File.WriteAllText("Artifacts/AKI112Blood/After.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
