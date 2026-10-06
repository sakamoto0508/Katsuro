using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class ConfigureSwordTrail
{
    const string PrefabPath="Assets/Mock/Prefabs/Player.prefab";
    const string MaterialPath="Assets/Mock/Resources/SwordTrailSteel.mat";
    const string Key="Katsuro.SwordTrail.Prefab";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode and compiled scripts required.");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Mock/Resources/BladeTrail.shader");
        Check(shader!=null && !ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Trail Shader must compile.");
        var existing=AssetDatabase.LoadMainAssetAtPath(MaterialPath);
        Check(existing==null || existing is Material,"Do not replace another asset.");
        Check(!AssetDatabase.FindAssets("SwordTrailSteel t:Material").Any(g=>AssetDatabase.GUIDToAssetPath(g)!=MaterialPath),"Same named material exists elsewhere; inspect before creating.");
        var material=existing as Material;
        if(material==null){material=new Material(shader){name="SwordTrailSteel"};AssetDatabase.CreateAsset(material,MaterialPath);}
        Check(material.shader==shader,"Do not replace existing material shader.");
        material.SetColor("_Tint",new Color(.93f,.97f,1f,1f));
        material.SetFloat("_CoreWidth",.035f);material.SetFloat("_CoreBrightness",2.8f);
        material.SetFloat("_BodyAlpha",.24f);material.SetFloat("_BodyWidth",.22f);
        material.SetFloat("_AfterGlowAlpha",.055f);material.SetFloat("_EdgeFade",.12f);
        material.SetFloat("_TailFade",.5f);material.SetFloat("_TipBrightness",1.2f);
        material.SetFloat("_Brightness",1.05f);material.SetFloat("_FadePower",1.2f);
        material.renderQueue=3000;EditorUtility.SetDirty(material);
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        var trails=root.GetComponentsInChildren<SwordTrail>(true);Check(trails.Length==5,"Expected existing five blade collider trails.");
        foreach(var trail in trails)
        {
            var so=new SerializedObject(trail);
            so.FindProperty("_trailMaterial").objectReferenceValue=material;
            so.FindProperty("_duration").floatValue=.11f;
            so.FindProperty("_heavyDuration").floatValue=.16f;so.FindProperty("_counterDuration").floatValue=.18f;
            so.FindProperty("_minimumSpeed").floatValue=2;so.FindProperty("_fullSpeed").floatValue=14;
            so.FindProperty("_heavyBrightness").floatValue=1.18f;so.FindProperty("_counterBrightness").floatValue=1.35f;
            so.FindProperty("_brightness").floatValue=1.05f;so.FindProperty("_fade").floatValue=1.15f;
            so.FindProperty("_color").colorValue=new Color(.92f,.96f,1f,.72f);
            so.ApplyModifiedPropertiesWithoutUndo();
            var legacy=trail.GetComponent<TrailRenderer>();if(legacy!=null){legacy.emitting=false;legacy.enabled=false;}
        }
        SessionState.SetInt(Key,root.GetInstanceID());
        return "Staged existing Player trails: shared base/tip unchanged, assigned SwordTrailSteel, lifetimes .11/.16/.18, speed 2..14m/s, brightness 1/1.18/1.35. Legacy TrailRenderers retained and disabled. Animation events untouched.";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compile before Save.");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key,0))as GameObject;Check(root!=null,"Staged prefab missing.");
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath));
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool saved);Check(saved,"Prefab save failed.");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt(Key);
        return "Saved Player.prefab and SwordTrailSteel.mat; shader source imported and validated.";
    }
}
