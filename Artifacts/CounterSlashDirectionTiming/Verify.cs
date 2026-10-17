using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class VerifyCounterSlashDirectionTiming
{
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"No Play Mode test authorized");
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        var type=typeof(SwordTrail).Assembly.GetType("CounterImpactStroke",true);
        var choose=type.GetMethod("SelectMotionDirection");var project=type.GetMethod("ProjectDirection");var vertex=type.GetMethod("BladeVertex");
        Check(choose!=null&&project!=null,"Latest assembly not loaded");
        Func<Vector3,Vector3,Vector3,Vector3> select=(instant,recent,sweep)=>(Vector3)choose.Invoke(null,new object[]{instant,recent,sweep,3.5f});
        Check(Vector3.Angle(select(Vector3.up*10,Vector3.right*8,Vector3.forward),Vector3.up)<.01f,"Instant tip motion not preferred");
        Check(Vector3.Angle(select(Vector3.zero,Vector3.right*8,Vector3.up),Vector3.right)<.01f,"Fresh measured motion fallback failed");
        Check(Vector3.Angle(select(Vector3.zero,Vector3.one*.1f,Vector3.up),Vector3.up)<.01f,"Slow tip noise replaced sweep");
        Check(select(Vector3.zero,Vector3.zero,Vector3.zero)==Vector3.zero,"Unmeasured direction invented");
        Func<Vector3,Vector3,Vector3,float,float,Vector3> correction=(d,h,v,m,a)=>(Vector3)project.Invoke(null,new object[]{d,h,v,m,a});
        int projectionChecks=0;
        foreach(var v in new[]{Vector3.forward,new Vector3(.3f,.15f,1).normalized})
        foreach(var d in new[]{new Vector3(.03f,.12f,1).normalized,new Vector3(.2f,.08f,-1).normalized,Vector3.up,Vector3.right})
        {
            var r=correction(d,Vector3.zero,v,.55f,20f);
            var p0=Vector3.ProjectOnPlane(d,v);var p1=Vector3.ProjectOnPlane(r,v);
            Check(Vector3.Angle(d,r)<=20.01f,"Projection exceeded 20 degree correction limit");
            Check(p1.magnitude>=p0.magnitude-.0001f,"Projection shortened the stroke");
            if(p0.sqrMagnitude>.00001f)Check(Vector3.Angle(p0,p1)<.05f,"Projection changed measured screen azimuth");
            Check(Mathf.Abs(r.magnitude-1)<.0001f,"Projected axis not normalized");projectionChecks++;
        }
        Check(correction(Vector3.forward,Vector3.forward,Vector3.forward,.55f,20f)==Vector3.forward,"View-aligned motion invented a horizontal axis");
        Check(Vector3.Angle(correction(Vector3.up,Vector3.zero,Vector3.forward,.55f,20f),Vector3.up)<.01f,"Vertical physical swing was flattened");
        Check(correction(Vector3.forward,Vector3.right,Vector3.forward,.55f,0f)==Vector3.forward,"Projection disable failed");
        int geometryChecks=0;
        foreach(var s in new[]{config.First,config.Second})
        {
            Check(s.StrokeCoreHold<s.StrokeCoreDuration&&s.StrokeCoreDuration<s.StrokeDuration,"Invalid temporal ordering");
            Check(s.StrokePeakDuration<s.StrokeCoreDuration,"Peak not separate from core fade");
            for(int i=0;i<9;i++)for(int row=0;row<3;row++)
            {
                float u=i/8f;var center=new Vector3(2,1,-3);
                var p=(Vector3)vertex.Invoke(null,new object[]{center,Vector3.right,Vector3.up,s.StrokeLength,s.StrokeWidth,u,row-1f});
                Check(Mathf.Abs(p.x-center.x-(u-.5f)*s.StrokeLength)<.0001f,"Stroke center moved from HitPoint");
                Check(Mathf.Abs(p.y-center.y)<=s.StrokeWidth*.5f+.0001f,"Stroke width exceeded bounds");
                if(i==4&&row==1)Check((p-center).sqrMagnitude<.0000001f,"Contact vertex shifted");geometryChecks++;
            }
        }
        Check(config.First.StrokeCoreHold>=1f/30f,"First peak hold shorter than one 30fps frame interval");
        Check(config.Second.StrokeCoreHold>=config.Second.HitStop,"Second core hold shorter than existing HitStop");
        Check(Mathf.Approximately(config.First.HitStop,.045f)&&Mathf.Approximately(config.Second.HitStop,.155f),"HitStop changed");
        var mats=new[]{config.First.SlashMaterial,config.Second.SlashMaterial,config.First.StrokeMaterial,config.Second.StrokeMaterial}.Select(mat=>{
            Check(mat!=null,"Missing material");ShaderUtil.CompilePass(mat,0,true);
            var messages=ShaderUtil.GetShaderMessages(mat.shader).Select(m=>new{m.message,m.line,severity=m.severity.ToString()}).ToArray();
            Check(ShaderUtil.IsPassCompiled(mat,0)&&!messages.Any(m=>m.severity=="Error"),"Shader compile failed");
            return new{path=AssetDatabase.GetAssetPath(mat),shader=AssetDatabase.GetAssetPath(mat.shader),mat.renderQueue,compiled=true,messages};
        }).ToArray();
        var cfx=new[]{config.FirstCFX,config.FinisherCFX}.Select(effect=>{
            Check(effect.UsesLocalContactFlash,"Large old Cut quad enabled");
            return new{prefab=AssetDatabase.GetAssetPath(effect),emitters=effect.GetComponentsInChildren<ParticleSystem>(true).Select(ps=>new{ps.name,
                offset=effect.transform.InverseTransformPoint(ps.transform.position).ToString("R"),shapeOffset=ps.shape.position.ToString("R")}).ToArray()};
        }).ToArray();
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        int samplers=player.GetComponentsInChildren<SwordTrail>(true).Count(t=>new SerializedObject(t).FindProperty("_sharedTrail").objectReferenceValue==null);
        Check(samplers==1,"Shared sampler count changed");
        var result=new{playing=EditorApplication.isPlaying,projectionChecks,geometryChecks,samplers,mats,cfx,
            scope="Asset API and pure C# direction/projection/geometry checks only. No Scene or Play Mode test."};
        File.WriteAllText("Artifacts/CounterSlashDirectionTiming/EditorVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
