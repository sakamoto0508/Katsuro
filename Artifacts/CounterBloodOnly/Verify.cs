using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class VerifyCounterBloodOnly
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"No Play Mode verification authorized");
        var temporary=ScriptableObject.CreateInstance<JustAvoidCounterConfig>();
        try
        {
            Check(!temporary.IsBloodOnlyDebug,"Default mode is not Normal");
            temporary.VFXDebugMode=CounterVFXDebugMode.BloodOnly;Check(temporary.IsBloodOnlyDebug,"BloodOnly gate failed");
            temporary.VFXDebugMode=CounterVFXDebugMode.Normal;Check(!temporary.IsBloodOnlyDebug,"Normal restore failed");
        }
        finally{UnityEngine.Object.DestroyImmediate(temporary);}
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        Check(new SerializedObject(c).FindProperty("VFXDebugMode")!=null,"Inspector mode field missing");
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        var configs=player.GetComponentsInChildren<SwordTrail>(true).Select(t=>new SerializedObject(t).FindProperty("_counterPresentation").objectReferenceValue).Where(x=>x!=null).ToArray();
        Check(configs.Length>0&&configs.All(x=>x==c),"SwordTrail uses a different Counter Config");
        var vfx=AssetDatabase.FindAssets("t:VFXConfig",new[]{"Assets/Mock"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p,cfg=AssetDatabase.LoadAssetAtPath<VFXConfig>(p)}).ToArray();
        Check(vfx.Any(x=>x.cfg.CounterPresentation==c),"Combat VFX Config reference missing");
        var effects=new[]{c.FirstCFX,c.FinisherCFX}.Select(fx=>{
            Check(fx!=null,"CFX prefab missing");var so=new SerializedObject(fx);var blood=so.FindProperty("_blood");Check(blood.arraySize==3,"Expected three Blood emitters");
            var cut=so.FindProperty("_cutRenderer").objectReferenceValue;Check(cut!=null,"Cut renderer missing");
            var emitters=fx.GetComponentsInChildren<ParticleSystem>(true).Select(ps=>{var r=ps.GetComponent<ParticleSystemRenderer>();Check(r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.shader!=null,"Broken Blood renderer reference");
                var m=ps.main;return new{ps.name,m.useUnscaledTime,space=m.simulationSpace.ToString(),renderer=r.enabled,material=r.sharedMaterial.name,shader=r.sharedMaterial.shader.name,
                    r.lengthScale,r.velocityScale,r.cameraVelocityScale,mode=r.renderMode.ToString(),startAlpha=new[]{m.startColor.colorMin.a,m.startColor.colorMax.a},
                    sizeLife=ps.sizeOverLifetime.size.Evaluate(.5f),endAlpha=ps.colorOverLifetime.color.Evaluate(1f).a};}).ToArray();
            return new{path=AssetDatabase.GetAssetPath(fx),fx.UsesLocalContactFlash,emitters};
        }).ToArray();
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat");ShaderUtil.CompilePass(mat,0,true);
        var messages=ShaderUtil.GetShaderMessages(mat.shader);Check(ShaderUtil.IsPassCompiled(mat,0)&&!messages.Any(m=>m.severity.ToString()=="Error"),"Blood shader compilation failed");
        var result=new{playing=EditorApplication.isPlaying,mode=c.VFXDebugMode.ToString(),effects,configs=vfx.Select(x=>new{x.path,counter=AssetDatabase.GetAssetPath(x.cfg.CounterPresentation)}).ToArray(),
            renderQueue=mat.renderQueue,ztest=mat.GetFloat("_ZTest"),compiled=true,shaderMessages=messages.Select(m=>m.message).ToArray(),
            scope="Edit Mode Asset API and temporary ScriptableObject mode gate only. No GameObjects instantiated, no particle playback, no Scene/Play Mode/render validation."};
        File.WriteAllText("Artifacts/CounterBloodOnly/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
