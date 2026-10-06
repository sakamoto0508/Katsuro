using UnityEngine;
using UnityEditor;
using System;
public static class SpreadHitVFX
{
    const string Path="Assets/Mock/Effects/SwordHitVFX.prefab";
    public static string Stage()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Idle Edit Mode required");
        if(SessionState.GetInt("KatsuroSpreadRoot",0)!=0)throw new Exception("Already staged");
        var root=PrefabUtility.LoadPrefabContents(Path);
        SessionState.SetInt("KatsuroSpreadRoot",root.GetInstanceID());
        foreach(var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var m=p.main;
            if(p.name=="BloodStreaks")
            {
                m.startSize=new ParticleSystem.MinMaxCurve(.085f,.125f);
                m.startLifetime=new ParticleSystem.MinMaxCurve(.20f,.28f);
                m.startSpeed=new ParticleSystem.MinMaxCurve(3.8f,5.8f);
                var shape=p.shape;shape.angle=18;shape.radius=.035f;shape.radiusThickness=0;
                // Burst内で方位を均等に分け、同じ筋への重なりを減らす。
                shape.arc=360;shape.arcMode=ParticleSystemShapeMultiModeValue.BurstSpread;shape.arcSpread=0;
                shape.randomDirectionAmount=0;shape.sphericalDirectionAmount=0;
                var size=p.sizeOverLifetime;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(.3f,1),new Keyframe(1,.25f)));
                var r=p.GetComponent<ParticleSystemRenderer>();r.lengthScale=3.2f;r.velocityScale=.045f;
            }
            else if(p.name=="BloodDroplets")
            {
                m.startSize=new ParticleSystem.MinMaxCurve(.022f,.036f);
                var shape=p.shape;shape.angle=22;shape.radius=.025f;
            }
        }
        return "Staged: main size .085-.125, Cone 18 degrees, radius .035, BurstSpread; size held for initial 30%; droplets size .022-.036 and Cone 22; no added particles/materials.";
    }
    public static string Save()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Not idle");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroSpreadRoot",0)) as GameObject;
        if(root==null)throw new Exception("Staged prefab missing");
        PrefabUtility.SaveAsPrefabAsset(root,Path,out bool ok);if(!ok)throw new Exception("Save failed");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt("KatsuroSpreadRoot");
        return "Saved existing SwordHitVFX prefab; reference and GUID preserved.";
    }
}
