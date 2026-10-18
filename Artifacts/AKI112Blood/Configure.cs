using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ConfigureAKI112Blood
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only; Play Mode not changed");
        AssetDatabase.ImportAsset("Assets/Mock/Effects/JustAvoidCounterBlood.shader",ImportAssetOptions.ForceSynchronousImport);
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/JustAvoidCounterBlood.mat");
        // Keep the current crimson palette, gain and depth mode; these controls change coverage only.
        mat.SetFloat("_Opacity",1f);mat.SetFloat("_ContourVariation",.65f);
        EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        ConfigurePrefab("Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab",false);
        ConfigurePrefab("Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab",true);
        return new{playing=EditorApplication.isPlaying,configured=true};
    }
    static void ConfigurePrefab(string path,bool second)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m=ps.main;
                if(ps.name=="Crimson Threads")
                {
                    m.startSize=second?new ParticleSystem.MinMaxCurve(.145f,.195f):new ParticleSystem.MinMaxCurve(.105f,.145f);
                    var curve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.15f,1.12f),new Keyframe(.48f,1),new Keyframe(.8f,.48f),new Keyframe(1,.06f));
                    for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);}
                    var size=ps.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1f,curve);
                    var sh=ps.shape;sh.angle=second?18f:13f;
                    ps.GetComponent<ParticleSystemRenderer>().lengthScale=second?8f:7f;
                }
                else if(ps.name=="Dark Blood Drops")
                {
                    m.startSize=second?new ParticleSystem.MinMaxCurve(.09f,.14f):new ParticleSystem.MinMaxCurve(.075f,.105f);
                    m.startDelay=second?new ParticleSystem.MinMaxCurve(.035f,.055f):new ParticleSystem.MinMaxCurve(.018f,.032f);
                }
                else if(ps.name=="Late Fine Drops")
                {
                    m.startSize=second?new ParticleSystem.MinMaxCurve(.032f,.055f):new ParticleSystem.MinMaxCurve(.026f,.045f);
                    m.startDelay=second?new ParticleSystem.MinMaxCurve(.095f,.135f):new ParticleSystem.MinMaxCurve(.055f,.08f);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
