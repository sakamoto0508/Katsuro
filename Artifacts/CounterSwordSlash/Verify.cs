using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class VerifyCounterSwordSlash
{
    private static void Require(bool test, string message) { if (!test) throw new Exception(message); }
    public static object Main()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "No Play Mode verification was requested.");
        var config = AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        Require(Mathf.Approximately(config.First.HitStop,.045f) && Mathf.Approximately(config.Second.HitStop,.155f), "HitStop changed");
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        var trails = player.GetComponentsInChildren<SwordTrail>(true).Select(trail => {
            var so = new SerializedObject(trail);
            var shared = so.FindProperty("_sharedTrail").objectReferenceValue;
            var blade = so.FindProperty("_bladeBase").objectReferenceValue as Transform;
            var tip = so.FindProperty("_tip").objectReferenceValue as Transform;
            Require(blade != null && tip != null, "Blade endpoint reference missing");
            Require(so.FindProperty("_counterPresentation").objectReferenceValue == config, "Wrong counter configuration");
            var animator = trail.GetComponentInParent<Animator>(true);
            Require(animator != null && animator.GetComponent<JustAvoidCounterAnimation>() != null, "Counter State owner missing");
            return new { trail.name, shared = shared != null ? shared.name : null, bladeBase = blade.name, tip = tip.name,
                legacyMaterial = AssetDatabase.GetAssetPath(so.FindProperty("_trailMaterial").objectReferenceValue) };
        }).ToArray();
        Require(trails.Count(t => t.shared == null) == 1, "Expected one shared sampler");
        var materials = new[] { config.First.SlashMaterial, config.Second.SlashMaterial }.Select(mat => {
            Require(mat != null && mat.shader.name == "Katsuro/CounterSwordSlash", "Slash material reference missing");
            ShaderUtil.CompilePass(mat,0,true);
            var messages = ShaderUtil.GetShaderMessages(mat.shader).Select(m => new { m.message, m.line, severity = m.severity.ToString() }).ToArray();
            Require(!messages.Any(m => m.severity == "Error"), "Slash shader compile error");
            Require(ShaderUtil.IsPassCompiled(mat,0), "Slash pass not compiled");
            return new { path = AssetDatabase.GetAssetPath(mat), shader = AssetDatabase.GetAssetPath(mat.shader), mat.renderQueue,
                compiled = ShaderUtil.IsPassCompiled(mat,0), messages,
                properties = Enumerable.Range(0,mat.shader.GetPropertyCount()).ToDictionary(i => mat.shader.GetPropertyName(i), i =>
                    mat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color || mat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Vector
                    ? (object)mat.GetVector(mat.shader.GetPropertyName(i)).ToString("R") : mat.GetFloat(mat.shader.GetPropertyName(i))) };
        }).ToArray();
        var cfx = new[] { config.FirstCFX, config.FinisherCFX }.Select(effect => {
            Require(effect != null && effect.UsesLocalContactFlash, "Old large Cut quad is not suppressed");
            var so = new SerializedObject(effect);
            var blood = so.FindProperty("_blood"); Require(blood.arraySize == 3, "Blood references changed");
            for (int i=0;i<blood.arraySize;i++) Require(blood.GetArrayElementAtIndex(i).objectReferenceValue != null,"Missing Blood reference");
            return new { path = AssetDatabase.GetAssetPath(effect), effect.UsesLocalContactFlash, bloodCount = blood.arraySize };
        }).ToArray();

        // Pure C# math/time tests against the actual helper. No GameObjects or Scenes are created.
        var type = typeof(SwordTrail).Assembly.GetType("CounterSlashPresentation", true);
        var helper = Activator.CreateInstance(type, new object[] { null, null });
        type.GetField("<Settings>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(helper,config.Second);
        var hit = type.GetMethod("Hit"); var advance = type.GetMethod("Advance"); var age = type.GetMethod("CoreAge");
        var taper = type.GetMethod("Taper"); var curved = type.GetMethod("CurvedTip");
        float[] times = { .9f,.95f,1f };
        hit.Invoke(helper,new object[] { Vector3.zero,1f });
        object[] step = { 1.1f,times,3,.98f }; advance.Invoke(helper,step);
        Require(Mathf.Abs(times[0]-1f)<.0001f && Mathf.Abs((float)step[3]-1.08f)<.0001f,"History or speed clock did not freeze");
        Require(Mathf.Abs((float)age.Invoke(helper,new object[] { .1f,1.1f })-.012f)<.0001f,"Core not held at impact");
        step = new object[] { 1.2f,times,3,(float)step[3] }; advance.Invoke(helper,step);
        Require(Mathf.Abs(times[0]-1.055f)<.0001f,"Hold exceeded configured .155 seconds");
        Require(Mathf.Abs((float)age.Invoke(helper,new object[] { .2f,1.255f })-.112f)<.0001f,"Core did not fade after hold");
        Require((float)taper.Invoke(null,new object[]{0f})==0f && (float)taper.Invoke(null,new object[]{1f})==0f,"Ribbon tips are not tapered");
        Require((float)taper.Invoke(null,new object[]{.5f})>.99f,"Ribbon center collapsed");
        var midpoint = (Vector3)curved.Invoke(null,new object[]{Vector3.zero,Vector3.right,Vector3.up,.5f});
        Require(Mathf.Abs(midpoint.magnitude-1f)<.0001f && midpoint.x>.7f && midpoint.y>.7f,"Low FPS rotational arc became a straight chord");
        var opposite = (Vector3)curved.Invoke(null,new object[]{Vector3.zero,Vector3.right,Vector3.left,.5f});
        Require(!float.IsNaN(opposite.x) && opposite.sqrMagnitude<.0001f,"Opposing samples produced an unstable arc");
        int geometryChecks=0;
        for(int count=2;count<=40;count++)
        {
            int points=(count-1)*3+1;
            Require(points*2<=40*3*2 && (points-1)*6<=(40*3-1)*6,"Mesh buffer overflow");
            for(int p=0;p<points;p++)
            {
                int i=Mathf.Min(p/3,count-2), j=i+1;
                float t=(p-i*3)/3f;
                Require(i>=0 && j<count && t>=0f && t<=1f,"Interpolation index out of range");
                float value=(float)taper.Invoke(null,new object[]{p/(float)(points-1)});
                Require(value>=0 && value<=1 && !float.IsNaN(value),"Nonfinite taper"); geometryChecks++;
            }
        }
        var result = new { playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling,
            graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), trails, materials, cfx,
            hitStop = new[] {config.First.HitStop,config.Second.HitStop},
            pureMathTests = new { historyFreeze=true, holdDeadline=true, coreRecovery=true, taper=true, rotationalArc=true, opposingArcFallback=true, geometryChecks },
            scope = "Asset API, shader compile and pure CPU math only. No Play Mode or Scene visual test." };
        File.WriteAllText("Artifacts/CounterSwordSlash/EditorVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
