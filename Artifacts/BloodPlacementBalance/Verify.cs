using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class VerifyBloodPlacementBalance
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");
        var cfg=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        var light=cfg.GetNormalBloodStyle(false);var heavy=cfg.GetNormalBloodStyle(true);
        Check(light!=null&&heavy!=null&&!ReferenceEquals(light,heavy),"Normal Blood settings not independent");
        Check(light.StreakCount==3&&heavy.StreakCount==4&&light.DropletCount==4&&heavy.DropletCount==6,"Normal Blood counts incorrect");
        Check(light.SpawnOffset==Vector3.zero&&heavy.SpawnOffset==Vector3.zero,"Unconfirmed position offset applied");
        var normal=cfg.HitVFX;var normalParticles=normal.GetComponentsInChildren<ParticleSystem>(true);
        Check(normalParticles.All(p=>p.gameObject.activeSelf&&p.main.simulationSpace==ParticleSystemSimulationSpace.World),"Normal prefab child inactive or wrong space");
        var second=cfg.CounterPresentation.FinisherCFX.GetComponentsInChildren<ParticleSystem>(true).Single(p=>p.name=="Crimson Threads");
        Check(Mathf.Abs(second.main.startSize.constantMin-.145f*.75f)<.000001f&&Mathf.Abs(second.main.startSize.constantMax-.195f*.75f)<.000001f,"Second width not 75%");
        Check(Mathf.Abs(second.main.startSize.constantMin*second.GetComponent<ParticleSystemRenderer>().lengthScale-.145f*8)<.00001f,"Size-driven streak length lost");
        Check(Mathf.Approximately(second.main.startSpeed.constantMin,8.2f)&&Mathf.Approximately(second.main.startSpeed.constantMax,9.6f)&&Mathf.Approximately(second.main.startLifetime.constantMax,.38f)&&second.emission.GetBurst(0).count.constant==7,"Second speed/life/count changed");
        int positionChecks=0;
        var point=new Vector3(2,1,-3);var slash=new Vector3(1,.2f,.1f);var away=new Vector3(.2f,.1f,1f);
        Check(JustAvoidCounterCutVFX.ResolveBloodSpawnPoint(point,slash,away,Vector3.zero)==point,"Zero offset moves hit origin");
        foreach(var offset in new[]{new Vector3(.01f,.02f,.03f),new Vector3(.5f,.2f,-.6f)})foreach(float angle in new[]{0f,37f,90f,180f})
        {
            var q=Quaternion.Euler(0,angle,0);var a=JustAvoidCounterCutVFX.ResolveBloodSpawnPoint(point,slash,away,offset);
            var b=JustAvoidCounterCutVFX.ResolveBloodSpawnPoint(q*point,q*slash,q*away,offset);
            Check((b-q*a).magnitude<.00001f,"Spawn axes use a fixed horizontal world direction");
            Check((a-point).magnitude<=.100001f,"Spawn offset exceeds 10cm");positionChecks++;
        }
        Check(JustAvoidCounterCutVFX.ResolveBloodSpawnPoint(point,Vector3.up,Vector3.up,Vector3.one)==point,"Unmeasured horizontal direction invented");
        var mats=new[]{"Assets/Mock/Effects/HitBlood.mat","Assets/Mock/Effects/HitBloodMist.mat","Assets/Mock/Effects/HitSlash.mat","Assets/Mock/Effects/JustAvoidCounterBlood.mat","Assets/Mock/Effects/JustAvoidCounterFirstSlash.mat","Assets/Mock/Effects/JustAvoidCounterFinisherSlash.mat","Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat","Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat"}.Select(p=>{
            var m=AssetDatabase.LoadAssetAtPath<Material>(p);Check(m!=null,"Missing material "+p);ShaderUtil.CompilePass(m,0,true);var messages=ShaderUtil.GetShaderMessages(m.shader);
            Check(ShaderUtil.IsPassCompiled(m,0)&&!messages.Any(x=>x.severity.ToString()=="Error"),"Shader compile failed "+p);return new{path=p,shader=m.shader.name,m.renderQueue,messages=messages.Select(x=>x.message).ToArray()};}).ToArray();
        var settings=new[]{light,heavy}.Select((s,i)=>new{attack=i==0?"Light":"Heavy",s.StreakCount,s.DropletCount,s.Width,s.Speed,s.Length,s.Lifetime,s.DirectionBias,offset=s.SpawnOffset.ToString("R")}).ToArray();
        var result=new{playing=EditorApplication.isPlaying,positionChecks,settings,second=new{size=new[]{second.main.startSize.constantMin,second.main.startSize.constantMax},second.GetComponent<ParticleSystemRenderer>().lengthScale},mats,
            scope="Asset APIs, shader compiler and pure position function only. No Scene, Play Mode, particle playback or visual validation."};
        File.WriteAllText("Artifacts/BloodPlacementBalance/After.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
