using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ConfigureCounterBladeStroke
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit Mode required; Play Mode was not changed.");
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterSwordSlash.shader",ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterImpactStroke.shader",ImportAssetOptions.ForceSynchronousImport);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Mock/Resources/CounterImpactStroke.shader");
        if(shader==null) throw new Exception("Missing stroke shader");
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterBladeStroke/ConfigBefore.json",EditorJsonUtility.ToJson(config,true));
        for(int i=0;i<2;i++)
        {
            bool second=i==1; var settings=second?config.Second:config.First;
            var arc=settings.SlashMaterial;
            arc.SetColor("_CoreTint",new Color(.9f,1.05f,1.3f,1));
            arc.SetColor("_SheetTint",second?new Color(.12f,.32f,.58f,1):new Color(.1f,.28f,.5f,1));
            arc.SetFloat("_CoreWidth",second?.045f:.035f);
            arc.SetFloat("_SheetIntensity",second?.38f:.25f);
            arc.SetFloat("_SheetSoftness",.22f);
            arc.SetFloat("_EdgeIntensity",second?1.1f:.75f);
            arc.SetFloat("_EdgeWidth",second?.032f:.022f);
            arc.SetFloat("_WhiteFrontSpan",second?.35f:.3f);
            arc.SetFloat("_FacetContrast",.75f);
            arc.SetFloat("_AfterglowIntensity",second?.4f:.28f);
            arc.SetFloat("_ImpactGain",second?.2f:.12f);
            arc.SetFloat("_Intensity",second?.9f:.8f);
            EditorUtility.SetDirty(arc); AssetDatabase.SaveAssetIfDirty(arc);
            string path="Assets/Mock/Effects/JustAvoidCounter"+(second?"Finisher":"First")+"Stroke.mat";
            var stroke=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(stroke==null)
            {
                if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) throw new Exception("Unrelated asset at "+path);
                stroke=new Material(shader); AssetDatabase.CreateAsset(stroke,path);
            }
            if(stroke.shader!=shader) throw new Exception("Unexpected stroke shader");
            stroke.SetColor("_WhiteTint",new Color(1.15f,1.22f,1.32f,1));
            stroke.SetColor("_SteelTint",second?new Color(.18f,.44f,.74f,1):new Color(.16f,.4f,.68f,1));
            stroke.SetFloat("_CoreWidth",second?.065f:.05f);
            stroke.SetFloat("_MinCorePixels",.8f);
            stroke.SetFloat("_CoreIntensity",second?2.8f:2.4f);
            stroke.SetFloat("_FrontIntensity",second?1.8f:1.3f);
            stroke.SetFloat("_SurfaceIntensity",second?1.1f:.8f);
            stroke.SetFloat("_FacetContrast",.8f);
            stroke.SetFloat("_EdgeSoftness",.2f);
            stroke.SetFloat("_ExtensionDuration",second?.022f:.015f);
            EditorUtility.SetDirty(stroke); AssetDatabase.SaveAssetIfDirty(stroke);
            settings.StrokeMaterial=stroke;
            settings.StrokeLength=second?2.1f:1.35f;
            settings.StrokeWidth=second?.15f:.085f;
            settings.StrokeCoreDuration=second?.09f:.055f;
            settings.StrokeDuration=second?.23f:.14f;
            settings.StrokeIntensity=second?1.25f:1f;
            settings.TrailWidth=second?1f:.85f;
            settings.TrailBrightness=second?.9f:.7f;
            settings.CoreIntensity=second?1.1f:.7f;
            settings.CoreDuration=second?.1f:.07f;
            settings.TrailDuration=second?.25f:.18f;
            settings.Afterglow=second?.26f:.18f;
        }
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
        File.WriteAllText("Artifacts/CounterBladeStroke/ConfigAfter.json",EditorJsonUtility.ToJson(config,true));
        return new {playing=EditorApplication.isPlaying,firstStroke=AssetDatabase.GetAssetPath(config.First.StrokeMaterial),secondStroke=AssetDatabase.GetAssetPath(config.Second.StrokeMaterial),hitStop=new[]{config.First.HitStop,config.Second.HitStop}};
    }
}
