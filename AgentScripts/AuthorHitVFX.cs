using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;
public static class AuthorHitVFX
{
    const string PrefabPath = "Assets/Mock/Effects/SwordHitVFX.prefab";
    const string MistPath = "Assets/Mock/Effects/HitBloodMist.mat";
    static Color Red(float r, float g, float b, float a) => new Color(r,g,b,a);
    public static string Stage()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Editor must be idle in Edit Mode");
        if(SessionState.GetInt("KatsuroHitVFXRoot",0)!=0) throw new Exception("Already staged");
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        if(config==null || AssetDatabase.GetAssetPath(config.HitVFX)!=PrefabPath) throw new Exception("Unexpected HitVFX reference");
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/HitBlood.mat");
        if(mat==null || mat.shader.name!="Katsuro/HitBlood") throw new Exception("Unexpected blood material");
        if(ShaderUtil.ShaderHasError(mat.shader)) throw new Exception("Blood shader compile error");
        mat.SetColor("_Tint",Color.white); mat.SetFloat("_Mist",0); mat.renderQueue=3000; EditorUtility.SetDirty(mat);
        var mist=AssetDatabase.LoadAssetAtPath<Material>(MistPath);
        if(mist==null)
        {
            // 同名の別型Assetがあれば新規作成しない。
            if(AssetDatabase.LoadMainAssetAtPath(MistPath)!=null) throw new Exception("Mist path occupied");
            mist=new Material(mat){name="HitBloodMist"};
            SessionState.SetBool("KatsuroHitVFXNewMist",true);
        }
        mist.SetFloat("_Mist",1); mist.SetColor("_Tint",Color.white); mist.renderQueue=3000;
        SessionState.SetInt("KatsuroHitVFXMist",mist.GetInstanceID());
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        SessionState.SetInt("KatsuroHitVFXRoot",root.GetInstanceID());
        var particles=root.GetComponentsInChildren<ParticleSystem>(true);
        if(particles.Length!=3) throw new Exception("Unexpected particle count");
        foreach(var p in particles)
        {
            int layer=p.name=="BloodStreaks"?0:p.name=="BloodDroplets"?2:p.name=="FineSlivers"?1:-1;
            if(layer<0) throw new Exception("Unexpected particle name");
            p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            p.transform.localPosition=Vector3.zero; p.transform.localRotation=Quaternion.identity; p.transform.localScale=Vector3.one;
            var m=p.main;
            m.duration=.4f; m.loop=false; m.playOnAwake=false; m.prewarm=false;
            m.simulationSpace=ParticleSystemSimulationSpace.Local; m.scalingMode=ParticleSystemScalingMode.Hierarchy;
            m.useUnscaledTime=true; m.stopAction=ParticleSystemStopAction.None; m.startDelay=0;
            m.startSize3D=false; m.startRotation3D=false; m.startRotation=0; m.maxParticles=layer==2?20:12;
            m.startLifetime=layer==0?new ParticleSystem.MinMaxCurve(.14f,.23f):layer==1?new ParticleSystem.MinMaxCurve(.09f,.15f):new ParticleSystem.MinMaxCurve(.19f,.34f);
            m.startSpeed=layer==0?new ParticleSystem.MinMaxCurve(3.8f,5.5f):layer==1?new ParticleSystem.MinMaxCurve(.25f,.6f):new ParticleSystem.MinMaxCurve(1.5f,3.0f);
            m.startSize=layer==0?new ParticleSystem.MinMaxCurve(.018f,.029f):layer==1?new ParticleSystem.MinMaxCurve(.1f,.17f):new ParticleSystem.MinMaxCurve(.009f,.017f);
            m.gravityModifier=layer==2?.22f:0;
            m.startColor=layer==0?new ParticleSystem.MinMaxGradient(Red(.145f,.005f,.004f,.94f),Red(.32f,.021f,.018f,.9f)):
                layer==1?new ParticleSystem.MinMaxGradient(Red(.20f,.011f,.009f,.09f),Red(.27f,.018f,.015f,.14f)):
                new ParticleSystem.MinMaxGradient(Red(.28f,.015f,.012f,.9f),Red(.60f,.063f,.063f,.8f));
            var e=p.emission; e.enabled=true; e.rateOverTime=0; e.rateOverDistance=0;
            e.SetBursts(new[]{new ParticleSystem.Burst(0,layer==0?new ParticleSystem.MinMaxCurve(3,4):layer==1?new ParticleSystem.MinMaxCurve(2):new ParticleSystem.MinMaxCurve(7,10))});
            var s=p.shape; s.enabled=true; s.shapeType=ParticleSystemShapeType.Cone;
            s.angle=layer==0?5:layer==1?14:16; s.radius=layer==1?.025f:.008f; s.radiusThickness=1;
            s.position=Vector3.zero; s.rotation=Vector3.zero; s.scale=Vector3.one;
            s.randomDirectionAmount=0; s.sphericalDirectionAmount=0; s.alignToDirection=false;
            var velocity=p.velocityOverLifetime; velocity.enabled=false;
            var noise=p.noise; noise.enabled=false;
            var limit=p.limitVelocityOverLifetime; limit.enabled=false;
            var force=p.forceOverLifetime; force.enabled=false;
            var rotation=p.rotationOverLifetime; rotation.enabled=false;
            var collision=p.collision; collision.enabled=false;
            var trails=p.trails; trails.enabled=false;
            var color=p.colorOverLifetime; color.enabled=true;
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,layer==1?.12f:.25f),new GradientAlphaKey(0,1)});
            color.color=new ParticleSystem.MinMaxGradient(gradient);
            var size=p.sizeOverLifetime; size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,layer==1?.65f:1),new Keyframe(1,layer==1?1.15f:.2f)));
            var r=p.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial=layer==1?mist:mat;
            r.renderMode=layer==1?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;
            r.lengthScale=layer==0?3.5f:layer==2?1.3f:1;
            r.velocityScale=layer==0?.065f:layer==2?.008f:0;
            r.cameraVelocityScale=0; r.sortMode=ParticleSystemSortMode.Distance;
            r.sortingOrder=layer==1?0:1; r.sortingFudge=0;
            r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=false;
            r.lightProbeUsage=LightProbeUsage.Off; r.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        var so=new SerializedObject(config);
        so.FindProperty("_hitVFXDuration").floatValue=.4f;
        so.FindProperty("_heavyHitVFXAmount").floatValue=1.4f;
        so.FindProperty("_heavyHitVFXSpread").floatValue=1.05f;
        so.FindProperty("_heavyHitVFXSize").floatValue=1;
        so.FindProperty("_heavyHitVFXScale").floatValue=1.3f;
        so.FindProperty("_heavyHitVFXSpeed").floatValue=1.2f;
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config);
        return "Staged existing prefab with 3 layers; material/config dirty; names, references and GUID preserved. Compile before Save.";
    }
    public static string Save()
    {
        if(EditorApplication.isCompiling || EditorApplication.isPlaying) throw new Exception("Editor not ready");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroHitVFXRoot",0)) as GameObject;
        var mist=EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroHitVFXMist",0)) as Material;
        if(root==null || mist==null) throw new Exception("Staged objects missing");
        if(ShaderUtil.ShaderHasError(mist.shader)) throw new Exception("Shader error");
        if(SessionState.GetBool("KatsuroHitVFXNewMist",false))
        {
            if(AssetDatabase.LoadMainAssetAtPath(MistPath)!=null) throw new Exception("Mist path occupied before save");
            AssetDatabase.CreateAsset(mist,MistPath);
        }
        EditorUtility.SetDirty(mist);
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool success);
        if(!success) throw new Exception("Prefab save failed");
        PrefabUtility.UnloadPrefabContents(root);
        SessionState.EraseInt("KatsuroHitVFXRoot"); SessionState.EraseInt("KatsuroHitVFXMist"); SessionState.EraseBool("KatsuroHitVFXNewMist");
        // 今回変更したAssetのみを保存する。
        AssetDatabase.SaveAssetIfDirty(mist);
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/HitBlood.mat"));
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset"));
        return "Prefab, Blood/Mist materials and VFXConfig saved successfully.";
    }
}
