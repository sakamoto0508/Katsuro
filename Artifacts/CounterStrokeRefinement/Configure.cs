using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ConfigureCounterStrokeRefinement
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required. Play Mode was not changed.");
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterImpactStroke.shader",ImportAssetOptions.ForceSynchronousImport);
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterStrokeRefinement/ConfigBefore.json",EditorJsonUtility.ToJson(config,true));
        for(int i=0;i<2;i++)
        {
            bool second=i==1;var s=second?config.Second:config.First;var m=s.StrokeMaterial;
            if(m==null||m.shader.name!="Katsuro/CounterImpactStroke")throw new Exception("Unexpected Stroke material");
            m.SetColor("_WhiteTint",new Color(.95f,1.08f,1.24f,1));
            m.SetColor("_SteelTint",second?new Color(.17f,.36f,.53f,1):new Color(.16f,.34f,.5f,1));
            m.SetFloat("_CoreWidth",second?.045f:.04f);
            m.SetFloat("_MinCorePixels",second?.65f:.6f);
            m.SetFloat("_CoreIntensity",second?2.4f:2.6f);
            m.SetFloat("_FrontIntensity",second?1.15f:.85f);
            m.SetFloat("_SurfaceIntensity",second?.65f:.55f);
            m.SetFloat("_FacetContrast",.6f);
            m.SetFloat("_EdgeSoftness",second?.38f:.4f);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
            s.StrokeLength=second?2.05f:1.2f;
            s.StrokeWidth=second?.135f:.072f;
            s.StrokeCoreDuration=second?.09f:.07f;
            s.StrokeDuration=second?.21f:.15f;
            s.StrokeIntensity=1.1f;
        }
        EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
        File.WriteAllText("Artifacts/CounterStrokeRefinement/ConfigAfter.json",EditorJsonUtility.ToJson(config,true));
        return new{playing=EditorApplication.isPlaying,hitStop=new[]{config.First.HitStop,config.Second.HitStop}};
    }
}
