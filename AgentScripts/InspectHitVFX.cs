using UnityEngine;
using UnityEditor;
using System.Text;
public static class InspectHitVFX
{
    static string Curve(ParticleSystem.MinMaxCurve c) => c.mode + ":" + c.constantMin + ".." + c.constantMax + " x" + c.curveMultiplier;
    public static string Main()
    {
        var b = new StringBuilder();
        b.AppendLine("Playing=" + EditorApplication.isPlaying + "; compiling=" + EditorApplication.isCompiling + "; pipeline=" + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline + "; colorSpace=" + QualitySettings.activeColorSpace);
        var config = AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        if (config == null || config.HitVFX == null) throw new System.Exception("VFXConfig/HitVFX missing");
        b.AppendLine("Config=" + EditorJsonUtility.ToJson(config));
        var root = config.HitVFX;
        b.AppendLine("Prefab=" + AssetDatabase.GetAssetPath(root) + "; root=" + root.transform.localEulerAngles + "; scale=" + root.transform.localScale);
        foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var m = p.main; var e = p.emission; var s = p.shape; var r = p.GetComponent<ParticleSystemRenderer>();
            b.AppendLine(p.name + ": rotation=" + p.transform.localEulerAngles + "; position=" + p.transform.localPosition + "; duration=" + m.duration + "; loop=" + m.loop + "; awake=" + m.playOnAwake + "; life=" + Curve(m.startLifetime) + "; speed=" + Curve(m.startSpeed) + "; size=" + Curve(m.startSize) + "; size3D=" + m.startSize3D + "; gravity=" + Curve(m.gravityModifier) + "; space=" + m.simulationSpace + "; max=" + m.maxParticles + "; color=" + m.startColor.mode + ":" + m.startColor.colorMin + ".." + m.startColor.colorMax);
            for (int i=0;i<e.burstCount;i++) { var burst=e.GetBurst(i); b.AppendLine(" Burst=" + Curve(burst.count) + "; time=" + burst.time + "; cycles=" + burst.cycleCount); }
            b.AppendLine("Emission=" + e.enabled + "; rate=" + Curve(e.rateOverTime) + "; shape=" + s.enabled + ":" + s.shapeType + "; angle=" + s.angle + "; radius=" + s.radius + "; shapeRotation=" + s.rotation + "; velocity=" + p.velocityOverLifetime.enabled + "; colorLife=" + p.colorOverLifetime.enabled + "; sizeLife=" + p.sizeOverLifetime.enabled + "; noise=" + p.noise.enabled + "; renderer=" + r.renderMode + "; stretch=" + r.lengthScale + "; velocityScale=" + r.velocityScale + "; sort=" + r.sortingOrder + "; material=" + AssetDatabase.GetAssetPath(r.sharedMaterial));
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Effects/HitBlood.mat");
        b.AppendLine("Material=" + EditorJsonUtility.ToJson(material) + "; shader=" + material.shader.name + "; supported=" + material.shader.isSupported + "; shaderErrors=" + ShaderUtil.ShaderHasError(material.shader));
        foreach (var path in new[]{"Assets/Mock/Prefabs/Enemy.prefab", "Assets/Mock/Prefabs/Player.prefab"})
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            b.AppendLine(path + ": CombatFeedback=" + EditorJsonUtility.ToJson(go.GetComponent<CombatFeedback>()));
            b.AppendLine("EnemyController=" + (go.GetComponent<EnemyController>() != null));
        }
        foreach(var name in new[]{"DamageInfo","EnemyController","CombatFeedback"})
            b.AppendLine("Script " + name + "=" + string.Join(",", AssetDatabase.FindAssets(name+" t:MonoScript")));
        foreach(var guid in AssetDatabase.FindAssets("t:Texture", new[]{"Assets/Mock"}))
        { var path=AssetDatabase.GUIDToAssetPath(guid); if(path.ToLowerInvariant().Contains("blood")) b.AppendLine("BloodTexture="+path); }
        return b.ToString();
    }
}
