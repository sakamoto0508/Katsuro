using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ConfigureBloodPlacementBalance
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        var light=config.GetNormalBloodStyle(false);var heavy=config.GetNormalBloodStyle(true);
        light.StreakCount=3;light.DropletCount=4;light.Width=1.25f;light.Speed=1f;light.Length=1f;light.Lifetime=1f;light.DirectionBias=.15f;light.SpawnOffset=Vector3.zero;
        heavy.StreakCount=4;heavy.DropletCount=6;heavy.Width=1.5f;heavy.Speed=1.15f;heavy.Length=1.25f;heavy.Lifetime=1.1f;heavy.DirectionBias=.2f;heavy.SpawnOffset=Vector3.zero;
        EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
        File.WriteAllText("Artifacts/BloodPlacementBalance/ConfigAfter.json",EditorJsonUtility.ToJson(config,true));
        var path="Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var main=root.GetComponentsInChildren<ParticleSystem>(true).Single(p=>p.name=="Crimson Threads");
            var m=main.main;m.startSize=new ParticleSystem.MinMaxCurve(.145f*.75f,.195f*.75f);
            main.GetComponent<ParticleSystemRenderer>().lengthScale=8f/.75f;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return new{playing=EditorApplication.isPlaying,configured=true,secondWidthFactor=.75f,secondLengthCompensation=8f/.75f};
    }
}
