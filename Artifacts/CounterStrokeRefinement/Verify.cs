using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class VerifyCounterBladeStroke
{
    private static void Check(bool ok,string message) {if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"No Play Mode test authorized");
        foreach(var path in new[]{"Assets/Mock/Resources/CounterSwordSlash.shader","Assets/Mock/Resources/CounterImpactStroke.shader"})
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        Check(Mathf.Approximately(config.First.HitStop,.045f)&&Mathf.Approximately(config.Second.HitStop,.155f),"HitStop modified");
        var mats=new[]{config.First.SlashMaterial,config.Second.SlashMaterial,config.First.StrokeMaterial,config.Second.StrokeMaterial}.Select(mat=>{
            Check(mat!=null,"Missing material"); ShaderUtil.CompilePass(mat,0,true);
            var messages=ShaderUtil.GetShaderMessages(mat.shader).Select(m=>new{m.message,m.line,severity=m.severity.ToString()}).ToArray();
            Check(ShaderUtil.IsPassCompiled(mat,0)&&!messages.Any(m=>m.severity=="Error"),"Shader compile failed");
            return new{path=AssetDatabase.GetAssetPath(mat),shader=AssetDatabase.GetAssetPath(mat.shader),compiled=ShaderUtil.IsPassCompiled(mat,0),mat.renderQueue,messages};
        }).ToArray();
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        var trails=player.GetComponentsInChildren<SwordTrail>(true);
        int samplers=trails.Count(t=>new SerializedObject(t).FindProperty("_sharedTrail").objectReferenceValue==null);
        Check(samplers==1,"Shared sampler reference changed");
        Check(config.FirstCFX.UsesLocalContactFlash&&config.FinisherCFX.UsesLocalContactFlash,"Old Cut quad is not suppressed");
        // Actual pure helper methods, with no GameObject/Scene or rendering instantiated.
        var type=typeof(SwordTrail).Assembly.GetType("CounterImpactStroke",true);
        var resolve=type.GetMethod("ResolveDirection"); var vertex=type.GetMethod("BladeVertex");
        Func<Vector3,Vector3,Vector3> direction=(s,h)=>(Vector3)resolve.Invoke(null,new object[]{s,h});
        Check(Vector3.Dot(direction(Vector3.right,Vector3.left),Vector3.right)>.999f,"Opposing history reversed the sweep");
        Check(Vector3.Dot(direction(Vector3.zero,Vector3.up),Vector3.up)>.999f,"History fallback failed");
        Check(direction(Vector3.zero,Vector3.zero)==Vector3.zero,"Missing direction invented a stroke");
        Check(Mathf.Abs(direction(Vector3.right,Vector3.up*.5f+Vector3.right).magnitude-1)<.0001f,"Averaged direction not normalized");
        int geometryChecks=0;
        foreach(var settings in new[]{config.First,config.Second})
        {
            Vector3 point=new Vector3(3,1,-2), forward=direction(new Vector3(1,.2f,.4f),new Vector3(1,.1f,.3f));
            Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
            for(int section=0;section<9;section++) for(int row=0;row<3;row++)
            {
                float u=section/8f;
                var p=(Vector3)vertex.Invoke(null,new object[]{point,forward,side,settings.StrokeLength,settings.StrokeWidth,u,row-1f});
                Check(!float.IsNaN(p.x)&&!float.IsInfinity(p.x),"Invalid stroke vertex");
                Vector3 center=point+forward*((u-.5f)*settings.StrokeLength);
                Check(Mathf.Abs(Vector3.Dot(p-point,forward)-(u-.5f)*settings.StrokeLength)<.0001f,"Centerline is not straight");
                Check((p-center).magnitude<=settings.StrokeWidth*.5f+.0001f,"Stroke width escaped its bounds");
                if(section==0||section==8)Check((p-center).magnitude<.0001f,"Stroke endpoint not tapered");
                geometryChecks++;
            }
            Check(settings.StrokeCoreDuration>=.033f&&settings.StrokeDuration>settings.StrokeCoreDuration,"Stroke duration invalid");
        }
        Check(config.Second.StrokeLength>config.First.StrokeLength&&config.Second.StrokeWidth>config.First.StrokeWidth,"Strike differentiation missing");
        var result=new{playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,graphicsAPI=SystemInfo.graphicsDeviceType.ToString(),mats,samplers,
            geometryChecks,directionTests=true,scope="Asset API, shader compilation, pure math only; no Scene or Play Mode test."};
        File.WriteAllText("Artifacts/CounterStrokeRefinement/EditorVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}

