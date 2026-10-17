using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class ConfigureCounterSlashMicroTune
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required; Play Mode was not changed.");
        var changes=new List<object>();
        var materials=new List<Material>();
        foreach(bool second in new[]{false,true})
        {
            string path="Assets/Mock/Effects/JustAvoidCounter"+(second?"Finisher":"First")+"Stroke.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null||mat.shader.name!="Katsuro/CounterImpactStroke")throw new Exception("Unexpected material reference: "+path);
            if(EditorUtility.IsDirty(mat))throw new Exception("Unsaved Material edits must be preserved: "+path);
            string[] names=second?new[]{"_SurfaceIntensity","_CoreWidth","_FrontIntensity","_FacetContrast"}:new[]{"_CoreIntensity","_FrontIntensity"};
            float[] values=second?new[]{.56f,.031f,1.2f,.65f}:new[]{3.05f,1.08f};
            File.WriteAllText("Artifacts/CounterSlashMicroTune/Baseline/"+mat.name+".json",EditorJsonUtility.ToJson(mat,true));
            for(int i=0;i<names.Length;i++)
            {
                if(!mat.HasProperty(names[i]))throw new Exception("Missing property "+names[i]);
                changes.Add(new{path,property=names[i],before=mat.GetFloat(names[i]),after=values[i]});
            }
            materials.Add(mat);
        }
        // Record all before-values before performing any mutation.
        File.WriteAllText("Artifacts/CounterSlashMicroTune/Changes.json",Newtonsoft.Json.JsonConvert.SerializeObject(changes,Newtonsoft.Json.Formatting.Indented));
        for(int index=0;index<materials.Count;index++)
        {
            var mat=materials[index];bool second=index==1;
            string[] names=second?new[]{"_SurfaceIntensity","_CoreWidth","_FrontIntensity","_FacetContrast"}:new[]{"_CoreIntensity","_FrontIntensity"};
            float[] values=second?new[]{.56f,.031f,1.2f,.65f}:new[]{3.05f,1.08f};
            for(int i=0;i<names.Length;i++)mat.SetFloat(names[i],values[i]);
            EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        }
        var shaders=materials.Select(mat=>{
            ShaderUtil.CompilePass(mat,0,true);
            var messages=ShaderUtil.GetShaderMessages(mat.shader).Select(m=>new{m.message,m.line,severity=m.severity.ToString()}).ToArray();
            if(!ShaderUtil.IsPassCompiled(mat,0)||messages.Any(m=>m.severity=="Error"))throw new Exception("Shader compile failed");
            return new{path=AssetDatabase.GetAssetPath(mat),shader=AssetDatabase.GetAssetPath(mat.shader),compiled=true,messages};
        }).ToArray();
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        if(config.First.StrokeMaterial!=materials[0]||config.Second.StrokeMaterial!=materials[1])throw new Exception("Counter material references changed");
        var result=new{playing=EditorApplication.isPlaying,changes,shaders,scope="Material API and shader compilation only; no Play Mode test."};
        File.WriteAllText("Artifacts/CounterSlashMicroTune/EditorVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
