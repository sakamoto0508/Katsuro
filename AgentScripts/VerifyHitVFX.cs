using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
public static class VerifyHitVFX
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static string Main()
    {
        Check(!EditorApplication.isPlaying,"Unexpected Play Mode");
        foreach(var path in new[]{"Assets/Mock/Effects/HitBlood.shader","Assets/Mock/Effects/HitBlood.mat","Assets/Mock/Effects/HitBloodMist.mat","Assets/Mock/Effects/SwordHitVFX.prefab","Assets/Mock/ScriptableObjects/VFXConfig.asset"})
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var c=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        Check(AssetDatabase.GetAssetPath(c.HitVFX)=="Assets/Mock/Effects/SwordHitVFX.prefab","Wrong HitVFX");
        Check(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c.HitVFX))=="0606e9ce5afba844d8f73096112372ce","Prefab GUID changed");
        var particles=c.HitVFX.GetComponentsInChildren<ParticleSystem>(true);
        Check(particles.Length==3,"Wrong layer count");
        foreach(var p in particles)
        {
            Check(p.main.simulationSpace==ParticleSystemSimulationSpace.Local && !p.main.loop && !p.main.playOnAwake,"Particle main incorrect");
            Check(p.emission.burstCount==1 && p.emission.GetBurst(0).time==0 && p.emission.GetBurst(0).cycleCount==1,"Wrong burst");
            Check(p.shape.rotation==Vector3.zero && p.shape.shapeType==ParticleSystemShapeType.Cone,"Wrong shape");
            Check(p.main.startLifetime.constantMax<=c.HitVFXDuration,"Pool cuts off particle lifetime");
            var mat=p.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            Check(mat!=null && mat.shader.isSupported && !ShaderUtil.ShaderHasError(mat.shader),"Material/shader error");
            Check(mat.GetFloat("_Mist")== (p.name=="FineSlivers"?1f:0f),"Wrong material layer");
        }
        // 実際の倍率関数が入力のPrefab基準値を変更しないことを確認する。
        var scale=typeof(CombatFeedback).GetMethod("ScaleHitCurve",BindingFlags.Static|BindingFlags.NonPublic);
        var source=particles[0].main.startSpeed;
        var heavy=(ParticleSystem.MinMaxCurve)scale.Invoke(null,new object[]{source,c.HeavyHitVFXSpeed});
        var light=(ParticleSystem.MinMaxCurve)scale.Invoke(null,new object[]{source,1f});
        Check(Mathf.Approximately(heavy.constantMax,source.constantMax*1.2f),"Heavy speed incorrect");
        Check(Mathf.Approximately(light.constantMax,source.constantMax),"Light speed accumulates heavy multiplier");
        var oldInfo=new DamageInfo(1,Vector3.zero,Vector3.forward,null,null);
        var sweepInfo=new DamageInfo(1,Vector3.zero,Vector3.forward,null,null,true,Vector3.left);
        Check(oldInfo.SlashDirection==Vector3.zero && sweepInfo.SlashDirection==Vector3.left && sweepInfo.HitNormal==Vector3.forward,"Direction metadata broke HitNormal");
        return "PASS: forced reimport; original GUID/reference; 3 Local burst layers; lifetime fits pool; material/shader valid; Heavy speed scaling preserves Light baseline; slash metadata preserves HitNormal. Play Mode=False.";
    }
}
