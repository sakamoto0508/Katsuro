using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ConfigureCounterSwordSlash
{
    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Asset configuration requires Edit Mode; Play Mode was not changed.");
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterSwordSlash.shader", ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Mock/Resources/CounterSwordSlash.shader");
        if (shader == null) throw new Exception("Slash shader is missing");
        var config = AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterSwordSlash/ConfigBefore.json", EditorJsonUtility.ToJson(config, true));
        for (int index = 0; index < 2; index++)
        {
            bool second = index == 1;
            string path = "Assets/Mock/Effects/JustAvoidCounter" + (second ? "Finisher" : "First") + "Slash.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) throw new Exception("An unrelated asset occupies " + path);
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.shader != shader) throw new Exception("Unexpected existing material shader: " + path);
            mat.SetColor("_CoreTint", second ? new Color(2f,2.15f,2.4f,1f) : new Color(1.65f,1.8f,2.05f,1f));
            mat.SetColor("_SheetTint", second ? new Color(.65f,.85f,1.15f,1f) : new Color(.48f,.68f,.95f,1f));
            mat.SetFloat("_CoreWidth", second ? .16f : .11f);
            mat.SetFloat("_CorePosition", .66f);
            mat.SetFloat("_SheetIntensity", second ? 1.15f : .7f);
            mat.SetFloat("_SheetSoftness", second ? .1f : .14f);
            mat.SetFloat("_EdgeIntensity", second ? 3.1f : 2.4f);
            mat.SetFloat("_EdgeWidth", second ? .075f : .05f);
            mat.SetFloat("_AfterglowIntensity", second ? .95f : .7f);
            mat.SetFloat("_ImpactGain", second ? 1f : .7f);
            mat.SetFloat("_ContactRadius", second ? .38f : .28f);
            mat.SetFloat("_ExposureWeight", 0f);
            mat.SetFloat("_Intensity", second ? 1.15f : 1f);
            EditorUtility.SetDirty(mat); AssetDatabase.SaveAssetIfDirty(mat);
            var strike = second ? config.Second : config.First;
            strike.SlashMaterial = mat;
            strike.BladeStart = second ? .12f : .28f;
            strike.TipExtension = second ? .14f : .06f;
            strike.TrailWidth = second ? 1.15f : .95f;
            strike.TrailDuration = second ? .36f : .23f;
            strike.TrailBrightness = second ? 1.6f : 1.3f;
            strike.CoreIntensity = second ? 3.6f : 2.2f;
            strike.CoreDuration = second ? .18f : .095f;
            strike.Afterglow = second ? .6f : .4f;
        }
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
        foreach (var path in new[] { "Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab", "Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<JustAvoidCounterCutVFX>());
                so.FindProperty("_useLocalContactFlash").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText("Artifacts/CounterSwordSlash/ConfigAfter.json", EditorJsonUtility.ToJson(config, true));
        return new { playing = EditorApplication.isPlaying, first = AssetDatabase.GetAssetPath(config.First.SlashMaterial), second = AssetDatabase.GetAssetPath(config.Second.SlashMaterial), hitStop = new[] { config.First.HitStop, config.Second.HitStop } };
    }
}
