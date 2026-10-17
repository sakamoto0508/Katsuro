using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ConfigureCounterImpactCohesion
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required; Play Mode not changed");
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        c.First.StrokeHitPeak=1.65f;c.First.StrokePeakDuration=.06f;
        c.Second.StrokeHitPeak=2.05f;c.Second.StrokePeakDuration=.08f;
        c.FirstReactionWeight=.58f;c.First.PoseDuration=.22f;c.First.TorsoFollowThrough=7f;
        c.First.FollowThroughPeak=.055f;c.First.FollowThroughHold=.018f;
        c.Second.FollowThroughPeak=.095f;c.Second.FollowThroughHold=.035f;
        EditorUtility.SetDirty(c);AssetDatabase.SaveAssetIfDirty(c);
        File.WriteAllText("Artifacts/CounterImpactCohesion/ConfigAfter.json",EditorJsonUtility.ToJson(c,true));
        ConfigurePrefab(AssetDatabase.GetAssetPath(c.FirstCFX),false);
        ConfigurePrefab(AssetDatabase.GetAssetPath(c.FinisherCFX),true);
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterImpactStroke.shader",ImportAssetOptions.ForceSynchronousImport);
        return new{playing=EditorApplication.isPlaying,configured=true};
    }
    private static void ConfigurePrefab(string path,bool second)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var fx=root.GetComponent<JustAvoidCounterCutVFX>();var so=new SerializedObject(fx);
            so.FindProperty("_bloodOutwardBias").floatValue=second?.3f:.22f;so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var m=ps.main;
                if(ps.name=="Crimson Threads")
                {
                    m.startDelay=0f;
                    m.startSpeed=second?new ParticleSystem.MinMaxCurve(8.2f,9.6f):new ParticleSystem.MinMaxCurve(6.4f,7.6f);
                    m.startSize=second?new ParticleSystem.MinMaxCurve(.10f,.135f):new ParticleSystem.MinMaxCurve(.07f,.095f);
                    m.startLifetime=second?new ParticleSystem.MinMaxCurve(.30f,.38f):new ParticleSystem.MinMaxCurve(.23f,.30f);
                    var shape=ps.shape;shape.angle=second?14f:10f;
                    var emission=ps.emission;var burst=emission.GetBurst(0);burst.count=second?7f:5f;emission.SetBurst(0,burst);
                    var r=ps.GetComponent<ParticleSystemRenderer>();r.lengthScale=second?9f:7.5f;
                }
                else if(ps.name=="Late Fine Drops")
                    m.startDelay=second?new ParticleSystem.MinMaxCurve(.065f,.09f):new ParticleSystem.MinMaxCurve(.035f,.05f);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
