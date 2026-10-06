using UnityEngine;
using UnityEditor;
using System;
public static class ImproveHitVisibility
{
    const string Path="Assets/Mock/Effects/SwordHitVFX.prefab";
    public static string Stage()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new Exception("Idle Edit Mode required");
        AssetDatabase.ImportAsset("Assets/Mock/Effects/HitBlood.shader",ImportAssetOptions.ForceUpdate);
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/HitBlood.mat");
        if(ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Shader compile failed");
        mat.SetColor("_EdgeTint",new Color(.55f,.045f,.025f,1));mat.SetFloat("_EdgeStrength",.35f);EditorUtility.SetDirty(mat);
        var mist=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/HitBloodMist.mat");
        mist.SetFloat("_EdgeStrength",0);EditorUtility.SetDirty(mist);
        var root=PrefabUtility.LoadPrefabContents(Path);
        SessionState.SetInt("KatsuroVisibilityRoot",root.GetInstanceID());
        foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var m=p.main; m.duration=.42f;
            if(p.name=="BloodStreaks")
            {
                m.startSize=new ParticleSystem.MinMaxCurve(.045f,.075f);
                m.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.27f);
                m.startSpeed=new ParticleSystem.MinMaxCurve(3.2f,4.8f);
                m.startColor=new ParticleSystem.MinMaxGradient(new Color(.25f,.01f,.008f,.98f),new Color(.48f,.032f,.018f,.96f));
                var e=p.emission;e.SetBursts(new[]{new ParticleSystem.Burst(0,new ParticleSystem.MinMaxCurve(4,5))});
                var r=p.GetComponent<ParticleSystemRenderer>();r.lengthScale=4.5f;r.velocityScale=.065f;
            }
            else if(p.name=="BloodDroplets")
            {
                m.startSize=new ParticleSystem.MinMaxCurve(.017f,.028f);
                m.startLifetime=new ParticleSystem.MinMaxCurve(.22f,.36f);
                m.startColor=new ParticleSystem.MinMaxGradient(new Color(.36f,.02f,.012f,.94f),new Color(.60f,.055f,.03f,.9f));
            }
            else if(p.name=="FineSlivers")
            {
                m.startSize=new ParticleSystem.MinMaxCurve(.14f,.22f);
                m.startLifetime=new ParticleSystem.MinMaxCurve(.10f,.17f);
                m.startColor=new ParticleSystem.MinMaxGradient(new Color(.27f,.016f,.009f,.12f),new Color(.36f,.025f,.014f,.19f));
            }
            else throw new Exception("Unexpected layer");
            var color=p.colorOverLifetime;
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,p.name=="FineSlivers"?.15f:.45f),new GradientAlphaKey(0,1)});
            color.color=new ParticleSystem.MinMaxGradient(gradient);
        }
        var c=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        if(AssetDatabase.GetAssetPath(c.HitVFX)!=Path)throw new Exception("Unexpected reference");
        var so=new SerializedObject(c);so.FindProperty("_hitVFXDuration").floatValue=.42f;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(c);
        return "Staged: main thickness 2.5x, length 4.5, 4-5 streaks, deep-red edge contrast, longer opaque phase; droplets enlarged; mist restrained.";
    }
    public static string Save()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new Exception("Not idle");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroVisibilityRoot",0)) as GameObject;
        if(root==null)throw new Exception("Missing staged prefab");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Mock/Effects/HitBlood.shader");
        if(ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compile failed");
        PrefabUtility.SaveAsPrefabAsset(root,Path,out bool ok);if(!ok)throw new Exception("Save failed");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt("KatsuroVisibilityRoot");
        foreach(var path in new[]{"Assets/Mock/Effects/HitBlood.mat","Assets/Mock/Effects/HitBloodMist.mat","Assets/Mock/ScriptableObjects/VFXConfig.asset"})
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(path));
        return "Saved existing prefab, materials and VFXConfig; shader errors=false.";
    }
}
