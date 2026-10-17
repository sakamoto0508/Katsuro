using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ConfigureCounterSlashFinalPolish
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required; Play Mode was not changed.");
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterImpactStroke.shader",ImportAssetOptions.ForceSynchronousImport);
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterSlashFinalPolish/ConfigBefore.json",EditorJsonUtility.ToJson(config,true));
        var first=config.First.StrokeMaterial;var second=config.Second.StrokeMaterial;
        first.SetFloat("_CoreWidth",.028f);
        first.SetFloat("_MinCorePixels",.55f);
        first.SetFloat("_CoreIntensity",2.9f);
        first.SetFloat("_FrontIntensity",1f);
        first.SetFloat("_ExtensionDuration",.01f);
        second.SetFloat("_CoreWidth",.035f);
        EditorUtility.SetDirty(first);AssetDatabase.SaveAssetIfDirty(first);
        EditorUtility.SetDirty(second);AssetDatabase.SaveAssetIfDirty(second);
        config.First.StrokeWidth=.068f;
        config.First.StrokeCoreDuration=.065f;
        config.First.StrokeDuration=.13f;
        EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
        File.WriteAllText("Artifacts/CounterSlashFinalPolish/ConfigAfter.json",EditorJsonUtility.ToJson(config,true));
        return new{playing=EditorApplication.isPlaying,hitStop=new[]{config.First.HitStop,config.Second.HitStop}};
    }
}
