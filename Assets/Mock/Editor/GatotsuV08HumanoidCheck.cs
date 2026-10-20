using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Gatotsu V08 の Humanoid クリップを実際の Enemy.prefab に適用し、全フレームの関節位置・刀の向き・足裏高さを
/// Artifacts/GatotsuV08/Stage3/UnitySample.json へ書き出す。開いているシーンや Animator Controller は変更しない
/// （非表示の Preview Scene で評価する）。初回はコンパイル後に自動実行、以降は Katsuro/Gatotsu メニューから。
/// </summary>
internal static class GatotsuV08HumanoidCheck
{
    private const string Fbx = "Assets/Art/Animation/Katsuro_Gatotsu_RM_v08.fbx";
    private const string ClipName = "Katsuro_Gatotsu_RM_v08";
    private const string EnemyPrefab = "Assets/Mock/Prefabs/Enemy.prefab";
    private const string OutDir = "Artifacts/GatotsuV08/Stage3";
    private const string OutFile = OutDir + "/UnitySample.json";
    // V07 で計測した RightHand 基準の刀 Grip（V08 も同じ Grip を使う）
    private static readonly Vector3 GripPosition = new Vector3(0.03806717f, 0.241248623f, -0.00632778648f);
    private static readonly Quaternion GripRotation = new Quaternion(0.764940143f, 0.122554682f, 0.288627982f, -0.5626199f);
    private static readonly string[] Bones =
    {
        "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
        "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
        "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    };

    [InitializeOnLoadMethod]
    private static void AutoRunOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(OutFile)) return;
            Run();
        };
    }

    private const string AnimOut = "Assets/Art/Animation/Katsuro_Gatotsu_RM_v08.anim";
    private const string KatanaPath = "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand/Katana_Rust";

    [InitializeOnLoadMethod]
    private static void AutoCreateAnimOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Re-sync whenever the FBX is newer than the .anim (re-exported from Blender).
            if (File.Exists(AnimOut) && File.GetLastWriteTimeUtc(AnimOut) >= File.GetLastWriteTimeUtc(Fbx)) return;
            CreateAnimWithGrip();
        };
    }

    /// <summary>
    /// FBX の Humanoid クリップ（Root Motion・Event 込み）を .anim に複製し、V07 と同じく Katana_Rust の握り
    /// （位置3・回転4の一定カーブ）を加える。Enemy の刀は RightHand 直下なので、これで実機でも刀が刺突方向を向く。
    /// </summary>
    [MenuItem("Katsuro/Gatotsu/Create V08 .anim (Humanoid + Katana grip)")]
    private static void CreateAnimWithGrip()
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == ClipName);
        if (clip == null) { Debug.LogWarning("[GatotsuV08] FBX clip not found."); return; }
        var copy = Object.Instantiate(clip);
        copy.name = ClipName;
        float end = clip.length;
        void Constant(string property, float value) =>
            AnimationUtility.SetEditorCurve(copy, EditorCurveBinding.FloatCurve(KatanaPath, typeof(Transform), property),
                AnimationCurve.Constant(0f, end, value));
        Constant("m_LocalPosition.x", GripPosition.x); Constant("m_LocalPosition.y", GripPosition.y); Constant("m_LocalPosition.z", GripPosition.z);
        Constant("m_LocalRotation.x", GripRotation.x); Constant("m_LocalRotation.y", GripRotation.y);
        Constant("m_LocalRotation.z", GripRotation.z); Constant("m_LocalRotation.w", GripRotation.w);
        AnimationUtility.SetAnimationEvents(copy, clip.events);
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimOut);
        if (existing != null)
        {
            // Overwrite in place so the GUID (and the Enemy.controller RushAttack reference) is kept.
            EditorUtility.CopySerialized(copy, existing);
            existing.name = ClipName;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(copy);
            copy = existing;
        }
        else AssetDatabase.CreateAsset(copy, AnimOut);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GatotsuV08] Synced {AnimOut} (humanMotion={copy.isHumanMotion}, length={copy.length:F3}s, events={copy.events.Length}).");
        MeasureDash();
    }

    private const string MeasureOut = OutDir + "/DashMeasure.json";
    private const string V07Anim = "Assets/Mock/Animation/Enemy/SpecialAttacks/Gatotsu/Katsuro_Gatotsu_RM_v07.anim";

    [InitializeOnLoadMethod]
    private static void AutoMeasureOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(MeasureOut) || !File.Exists(AnimOut)) return;
            MeasureDash();
        };
    }

    /// <summary>
    /// 刺突区間（WindupEndTime〜DashEndTime）で実Enemyが進む水平距離を、Humanoid の RootT カーブ × Enemy の humanScale ×
    /// オブジェクトスケールで求める。V07（0.30〜0.55秒、既知値 3.1772707m）も同じ式で計算して式の妥当性を確かめる。
    /// </summary>
    [MenuItem("Katsuro/Gatotsu/Measure V08 dash distance")]
    private static void MeasureDash()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var animator = go.GetComponentInChildren<Animator>(true);
            float scale = animator.humanScale * animator.transform.lossyScale.y;
            float Horizontal(AnimationClip clip, float a, float b)
            {
                AnimationCurve x = null, z = null;
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.propertyName == "RootT.x") x = AnimationUtility.GetEditorCurve(clip, binding);
                    if (binding.propertyName == "RootT.z") z = AnimationUtility.GetEditorCurve(clip, binding);
                }
                if (x == null || z == null) return -1f;
                var d = new Vector2(x.Evaluate(b) - x.Evaluate(a), z.Evaluate(b) - z.Evaluate(a));
                return d.magnitude * scale;
            }
            var v07 = AssetDatabase.LoadAssetAtPath<AnimationClip>(V07Anim);
            var v08 = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimOut);
            float d07 = v07 != null ? Horizontal(v07, .30f, .55f) : -1f;
            float d08 = Horizontal(v08, .45f, .70f);
            float d08All = Horizontal(v08, 0f, v08.length);
            var ci = CultureInfo.InvariantCulture;
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(MeasureOut, string.Format(ci,
                "{{\"humanScale\":{0},\"objectScale\":{1},\"v07_dash_0.30_0.55\":{2},\"v07_known\":3.1772707,\"v08_dash_0.45_0.70\":{3},\"v08_whole_clip\":{4}}}",
                animator.humanScale, animator.transform.lossyScale.y, d07, d08, d08All));
            Debug.Log($"[GatotsuV08] Dash distance V07={d07:F4}m (known 3.1773), V08={d08:F4}m, V08 whole clip={d08All:F4}m");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [MenuItem("Katsuro/Gatotsu/Check V08 on Enemy (Humanoid)")]
    private static void Run()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(Fbx);
        var clip = assets.OfType<AnimationClip>().FirstOrDefault(c => c.name == ClipName);
        var sourceAvatar = assets.OfType<Avatar>().FirstOrDefault();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (clip == null || prefab == null)
        {
            Debug.LogWarning($"[GatotsuV08] clip or Enemy prefab not found (clip={(clip != null)}, prefab={(prefab != null)}).");
            return;
        }
        var ci = CultureInfo.InvariantCulture;
        var json = new StringBuilder();
        json.Append("{\"clip\":{");
        json.AppendFormat(ci, "\"length\":{0},\"frameRate\":{1},\"isHumanMotion\":{2},\"events\":{3},\"sourceAvatarValid\":{4},\"sourceAvatarHuman\":{5}",
            clip.length, clip.frameRate, clip.isHumanMotion ? "true" : "false", clip.events.Length,
            sourceAvatar != null && sourceAvatar.isValid ? "true" : "false", sourceAvatar != null && sourceAvatar.isHuman ? "true" : "false");
        // Root motion distance from the humanoid root curves (body center travel).
        float rootZ = 0f, rootX = 0f;
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.propertyName != "RootT.z" && binding.propertyName != "RootT.x") continue;
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            float delta = curve.Evaluate(clip.length) - curve.Evaluate(0f);
            if (binding.propertyName == "RootT.z") rootZ = delta; else rootX = delta;
        }
        json.AppendFormat(ci, ",\"rootT_delta_z\":{0},\"rootT_delta_x\":{1}}}", rootZ, rootX);

        var scene = EditorSceneManager.NewPreviewScene();
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Mesh baked = new Mesh();
        try
        {
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            var animator = go.GetComponentInChildren<Animator>(true);
            json.AppendFormat(ci, ",\"enemy\":{{\"avatar\":\"{0}\",\"avatarHuman\":{1},\"scale\":{2}}}",
                animator.avatar != null ? animator.avatar.name : "", animator.avatar != null && animator.avatar.isHuman ? "true" : "false",
                go.transform.lossyScale.y);
            animator.applyRootMotion = false;
            // Playables are not evaluated inside a preview scene, so sample the clip directly onto the
            // Animator's object (Humanoid retargeting through the Enemy avatar). Positions are read relative
            // to the Animator's transform each frame, so any root travel cancels out.
            var root = animator.transform;
            var all = go.GetComponentsInChildren<Transform>(true);
            Transform Find(string n) => all.FirstOrDefault(t => t.name == n);
            var katana = Find("Katana_Rust");
            var katanaMesh = katana != null ? katana.GetComponentInChildren<MeshFilter>(true) : null;
            var skin = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(s => s.sharedMesh != null ? s.sharedMesh.vertexCount : 0).FirstOrDefault();

            int frames = Mathf.RoundToInt(clip.length * clip.frameRate) + 1;
            json.Append(",\"frames\":[");
            for (int f = 0; f < frames; f++)
            {
                float t = Mathf.Min(clip.length, f / clip.frameRate);
                clip.SampleAnimation(animator.gameObject, t);
                if (katana != null) { katana.localPosition = GripPosition; katana.localRotation = GripRotation; }
                if (f > 0) json.Append(',');
                json.AppendFormat(ci, "{{\"f\":{0},\"t\":{1},\"bones\":{{", f + 1, t);
                bool first = true;
                foreach (var name in Bones)
                {
                    var bone = Find(name);
                    if (bone == null) continue;
                    var p = root.InverseTransformPoint(bone.position);
                    json.AppendFormat(ci, "{0}\"{1}\":[{2},{3},{4}]", first ? "" : ",", name, p.x, p.y, p.z);
                    first = false;
                }
                json.Append('}');
                if (katanaMesh != null && katanaMesh.sharedMesh != null)
                {
                    // Blade tip = end of the long axis farther from the grip.
                    var b = katanaMesh.sharedMesh.bounds; var e = b.extents;
                    Vector3 axis = e.x >= e.y && e.x >= e.z ? Vector3.right : e.y >= e.z ? Vector3.up : Vector3.forward;
                    float half = Vector3.Dot(e, axis);
                    var a0 = root.InverseTransformPoint(katanaMesh.transform.TransformPoint(b.center - axis * half));
                    var a1 = root.InverseTransformPoint(katanaMesh.transform.TransformPoint(b.center + axis * half));
                    var hand = root.InverseTransformPoint(Find("RightHand").position);
                    var dir = ((a1 - hand).sqrMagnitude > (a0 - hand).sqrMagnitude ? a1 - a0 : a0 - a1).normalized;
                    json.AppendFormat(ci, ",\"blade\":[{0},{1},{2}]", dir.x, dir.y, dir.z);
                }
                if (skin != null)
                {
                    skin.BakeMesh(baked, true);
                    var m = skin.transform.localToWorldMatrix;
                    var lf = Find("LeftFoot").position; var rf = Find("RightFoot").position;
                    float lMin = float.MaxValue, rMin = float.MaxValue;
                    foreach (var v in baked.vertices)
                    {
                        var w = m.MultiplyPoint3x4(v);
                        if ((w - lf).sqrMagnitude < .35f * .35f) lMin = Mathf.Min(lMin, root.InverseTransformPoint(w).y);
                        if ((w - rf).sqrMagnitude < .35f * .35f) rMin = Mathf.Min(rMin, root.InverseTransformPoint(w).y);
                    }
                    json.AppendFormat(ci, ",\"sole\":[{0},{1}]", lMin, rMin);
                }
                json.Append('}');
            }
            json.Append("]}");
        }
        finally
        {
            Object.DestroyImmediate(baked);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(OutFile, json.ToString());
        Debug.Log($"[GatotsuV08] Sampled {ClipName} on Enemy.prefab -> {OutFile}");
    }
}
