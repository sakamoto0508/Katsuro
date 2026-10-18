using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// One-shot Edit Mode asset adjustment. Does not activate or simulate particles.
public static class ConfigureCounterSecondBloodPolish
{
    const string Folder = "Artifacts/CounterSecondBloodPolish/";
    const string Path = "Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab";
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Save(string name, object value) => File.WriteAllText(Folder + name + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented));
    static object Curve(ParticleSystem.MinMaxCurve c) => new { mode = c.mode.ToString(), c.constantMin, c.constantMax, c.curveMultiplier, keys = c.curveMax == null ? null : c.curveMax.keys.Select(k => new { k.time, k.value, k.inTangent, k.outTangent }).ToArray() };
    static object Snapshot(ParticleSystem p)
    {
        var m = p.main; var r = p.GetComponent<ParticleSystemRenderer>(); var s = p.shape; var e = p.emission;
        return new { p.name, activeSelf = p.gameObject.activeSelf, position = p.transform.localPosition.ToString("R"), rotation = p.transform.localRotation.ToString("R"), size = Curve(m.startSize), speed = Curve(m.startSpeed), lifetime = Curve(m.startLifetime), delay = Curve(m.startDelay), m.simulationSpace, m.useUnscaledTime, color = EditorJsonUtility.ToJson(p), sizeOverLifetime = Curve(p.sizeOverLifetime.size), bursts = Enumerable.Range(0,e.burstCount).Select(i => new { e.GetBurst(i).time, count = Curve(e.GetBurst(i).count) }).ToArray(), shape = new { s.angle, s.radius, s.length }, renderer = new { r.renderMode, r.lengthScale, r.velocityScale, r.cameraVelocityScale, pivot = r.pivot.ToString("R"), r.enabled, material = AssetDatabase.GetAssetPath(r.sharedMaterial), shader = r.sharedMaterial.shader.name, r.sharedMaterial.renderQueue } };
    }
    public static object Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode only");
        var root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var all = root.GetComponentsInChildren<ParticleSystem>(true);
            var main = all.Single(p => p.name == "Crimson Threads");
            var originalSize = main.main.startSize;
            Check(Mathf.Abs(originalSize.constantMin - .10875f) < .000001f && Mathf.Abs(originalSize.constantMax - .14625f) < .000001f, "Unexpected baseline; do not apply twice");
            var others = all.Where(p => p != main).Select(p => EditorJsonUtility.ToJson(p)).ToArray();
            var renderers = all.Select(p => EditorJsonUtility.ToJson(p.GetComponent<ParticleSystemRenderer>())).ToArray();
            var rootSettings = EditorJsonUtility.ToJson(root.GetComponent<JustAvoidCounterCutVFX>());
            Save("Before", all.Select(Snapshot).ToArray());

            // A: reduce geometric size without increasing stretch again. Speed and lifetime stay intact.
            var m = main.main;
            m.startSize = new ParticleSystem.MinMaxCurve(originalSize.constantMin * .75f, originalSize.constantMax * .75f);
            Save("StepAWidthOnly", Snapshot(main));

            // B: retain existing shader's two-ended taper, wet contour and randomized start sizes.
            // C: eliminate the early 1.12 bulge and thin the body earlier, preserving particle flight.
            var curve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.12f, 1), new Keyframe(.35f, .72f), new Keyframe(.6f, .36f), new Keyframe(1, .06f));
            for (int i = 0; i < curve.length; ++i)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            var size = main.sizeOverLifetime;
            Check(size.enabled && !size.separateAxes, "Unexpected size module");
            size.size = new ParticleSystem.MinMaxCurve(1, curve);
            for (int i = 0; i <= 100; ++i) Check(curve.Evaluate(i / 100f) >= .0599f && curve.Evaluate(i / 100f) <= 1.0001f, "Size curve overshoot");
            Check(others.SequenceEqual(all.Where(p => p != main).Select(p => EditorJsonUtility.ToJson(p))), "Droplet settings changed");
            Check(renderers.SequenceEqual(all.Select(p => EditorJsonUtility.ToJson(p.GetComponent<ParticleSystemRenderer>()))), "Renderer settings changed");
            Check(rootSettings == EditorJsonUtility.ToJson(root.GetComponent<JustAvoidCounterCutVFX>()), "CFX placement/lifecycle changed");
            Check(Mathf.Approximately(m.startSpeed.constantMin, 8.2f) && Mathf.Approximately(m.startSpeed.constantMax, 9.6f) && Mathf.Approximately(m.startLifetime.constantMin,.30f) && Mathf.Approximately(m.startLifetime.constantMax,.38f) && main.emission.GetBurst(0).count.constant == 7, "Speed/lifetime/count changed");
            Save("After", all.Select(Snapshot).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        var savedMain = saved.GetComponentsInChildren<ParticleSystem>(true).Single(p => p.name == "Crimson Threads");
        Check(Mathf.Abs(savedMain.main.startSize.constantMin - .0815625f) < .000001f && Mathf.Abs(savedMain.main.startSize.constantMax - .1096875f) < .000001f, "Saved width mismatch");
        var shaders = new[] { "Assets/Mock/Effects/JustAvoidCounterBlood.mat", "Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat", "Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat" }.Select(path =>
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Check(mat != null && mat.shader != null && mat.shader.isSupported, "Material/shader unavailable " + path);
            ShaderUtil.CompilePass(mat, 0, true);
            var messages = ShaderUtil.GetShaderMessages(mat.shader);
            Check(ShaderUtil.IsPassCompiled(mat,0) && !messages.Any(x => x.severity.ToString() == "Error"), "Shader error " + path);
            return new { path, shader = mat.shader.name, mat.renderQueue, messages = messages.Select(x => x.message).ToArray() };
        }).ToArray();
        var result = new { playing = EditorApplication.isPlaying, saved = Path, widthFactor = .75f, checks = "Other two particle systems, all renderers, CFX placement/lifecycle unchanged; size curve bounded; saved size verified", shaders, scope = "Edit Mode asset inspection and shader compile only; no Play Mode, simulation or visual verification." };
        Save("Verification", result);
        return result;
    }
}
