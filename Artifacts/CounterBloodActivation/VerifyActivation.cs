using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class VerifyCounterBloodActivation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"No Play Mode verification authorized");
        var cfg=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        var activation=typeof(JustAvoidCounterCutVFX).GetMethod("ActivateBloodObjects",BindingFlags.NonPublic|BindingFlags.Instance);
        Check(activation!=null,"Latest activation method not loaded");
        var rows=new List<object>();
        var temporary=ScriptableObject.CreateInstance<JustAvoidCounterConfig>();
        try
        {
            int index=0;
            foreach(var source in new[]{cfg.FirstCFX,cfg.FinisherCFX})
            {
                var path=AssetDatabase.GetAssetPath(source);var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var fx=root.GetComponent<JustAvoidCounterCutVFX>();var blood=root.GetComponentsInChildren<ParticleSystem>(true);
                    Check(blood.Length==3&&blood.All(p=>p.transform.parent==root.transform),"Expected direct Blood children");
                    Check(blood.All(p=>!p.gameObject.activeSelf&&!p.main.playOnAwake&&p.main.stopAction==ParticleSystemStopAction.None),"Baseline hierarchy or lifecycle changed");
                    // Reproduce just the old activation sequence; never call ParticleSystem.Play/Emit/Simulate.
                    root.SetActive(true);
                    Check(root.activeInHierarchy&&blood.All(p=>!p.gameObject.activeInHierarchy),"Old root-only activation did not reproduce inactive Blood");
                    foreach(var mode in new[]{CounterVFXDebugMode.Normal,CounterVFXDebugMode.BloodOnly})
                    {
                        temporary.VFXDebugMode=mode;fx.ConfigureDebug(temporary,index==0?JustAvoidCounterStrike.First:JustAvoidCounterStrike.Second);
                        foreach(var p in blood)p.gameObject.SetActive(false);
                        activation.Invoke(fx,null);
                        Check(blood.All(p=>p.gameObject.activeSelf&&p.gameObject.activeInHierarchy),"Blood activation failed "+mode);
                        Check(blood.All(p=>!p.isPlaying&&p.particleCount==0),"Verification unexpectedly played particles");
                        root.SetActive(false);
                        Check(blood.All(p=>p.gameObject.activeSelf&&!p.gameObject.activeInHierarchy),"Pooled root activation state invalid");
                        root.SetActive(true);activation.Invoke(fx,null);
                        Check(blood.All(p=>p.gameObject.activeInHierarchy),"Reused hierarchy failed "+mode);
                        rows.Add(new{stage=index==0?"First":"Second",mode=mode.ToString(),rootActiveSelf=root.activeSelf,rootActiveInHierarchy=root.activeInHierarchy,
                            blood=blood.Select(p=>new{p.name,activeSelf=p.gameObject.activeSelf,activeInHierarchy=p.gameObject.activeInHierarchy,p.isPlaying,p.particleCount,p.emission.enabled}).ToArray()});
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                index++;
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(temporary);}
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat");ShaderUtil.CompilePass(mat,0,true);
        Check(ShaderUtil.IsPassCompiled(mat,0)&&!ShaderUtil.GetShaderMessages(mat.shader).Any(x=>x.severity.ToString()=="Error"),"Blood shader compilation failed");
        var result=new{playing=EditorApplication.isPlaying,rows,scope="Unsaved Prefab contents activation test only. No ParticleSystem.Play/Emit/Simulate, no Scene asset changes or Play Mode/visual test."};
        File.WriteAllText("Artifacts/CounterBloodActivation/ActivationVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
