using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Assets/Art の一閃・ワープ素材から、Material / Prefab を作り、VFXConfig と WarpAttack パターンへ接続する。
/// 初回はコンパイル後に自動実行。作り直しは Katsuro/VFX/Rebuild Art VFX から。
/// 既にInspectorで別のVFXが割り当てられている参照は上書きしない。
/// </summary>
[InitializeOnLoad]
internal static class KatsuroArtVFXSetup
{
    private const string Root = "Assets/Art";
    private const string IssenFbx = Root + "/IssenSlash/IssenSlash.fbx";
    private const string WarpFbx = Root + "/WarpAttackVFX/WarpAttackVFX.fbx";
    private const string MaterialDir = Root + "/Materials";
    private const string PrefabDir = Root + "/Prefabs";
    private const string IssenPrefab = PrefabDir + "/VFX_IssenSlash.prefab";
    private const string VanishPrefab = PrefabDir + "/VFX_WarpVanish.prefab";
    private const string AppearPrefab = PrefabDir + "/VFX_WarpAppear.prefab";

    static KatsuroArtVFXSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(IssenPrefab) && File.Exists(VanishPrefab) && File.Exists(AppearPrefab)) return;
            if (!File.Exists(IssenFbx) || !File.Exists(WarpFbx)) return;
            Debug.LogWarning("[ArtVFX] A generated prefab is missing. Use Katsuro/VFX/Rebuild Art VFX explicitly; existing manual materials and prefabs were preserved.");
        };
    }

    [MenuItem("Katsuro/VFX/Rebuild Art VFX (Issen & Warp)")]
    private static void Build()
    {
        var additive = Shader.Find("Katsuro/VFXAdditive");
        var smoke = Shader.Find("Katsuro/VFXSmoke");
        if (additive == null || smoke == null)
        {
            Debug.LogWarning("[ArtVFX] Katsuro/VFXAdditive or Katsuro/VFXSmoke shader not found. Setup skipped.");
            return;
        }
        ConfigureImporters();
        Directory.CreateDirectory(MaterialDir);
        Directory.CreateDirectory(PrefabDir);

        // --- Issen slash (Just Avoid counter 2 strikes / after enemy warp)
        var issen = BuildPrefab(IssenPrefab, IssenFbx, new[]
        {
            Part("Slash_Glow", Additive(additive, "M_Issen_Glow", Root + "/IssenSlash/Textures/T_Slash_Glow.png", new Color(.8f, .9f, 1f)),
                new KatsuroTimedVFX.Track { FadeIn = .015f, Hold = .03f, FadeOut = .08f, Peak = .8f }),
            Part("Slash_LightSheet", Additive(additive, "M_Issen_LightSheet", Root + "/IssenSlash/Textures/T_Slash_LightSheet.png", new Color(.88f, .94f, 1f)),
                new KatsuroTimedVFX.Track { Delay = .015f, FadeIn = .04f, Hold = .03f, FadeOut = .12f, Peak = 1.5f, Wipe = true,
                    ScaleFrom = new Vector3(1f, .45f, 1f), ScaleTo = new Vector3(1f, 1.15f, 1f) }),
            Part("Slash_Core", Additive(additive, "M_Issen_Core", Root + "/IssenSlash/Textures/T_Slash_Core.png", Color.white),
                new KatsuroTimedVFX.Track { FadeIn = .03f, Hold = .02f, FadeOut = .05f, Peak = 9f, Wipe = true }),
            Part("Slash_Afterimage", Additive(additive, "M_Issen_Afterimage", Root + "/IssenSlash/Textures/T_Slash_Afterimage.png", new Color(.85f, .92f, 1f)),
                new KatsuroTimedVFX.Track { Delay = .04f, FadeIn = .03f, Hold = .04f, FadeOut = .14f, Peak = 1.4f }),
        });

        // --- Warp vanish (black mist, pivot = feet)
        var vanish = BuildPrefab(VanishPrefab, WarpFbx, new[]
        {
            Part("Vanish_Smoke", Smoke(smoke, "M_Warp_Smoke", Root + "/WarpAttackVFX/Textures/T_Warp_Smoke.png", .25f),
                new KatsuroTimedVFX.Track { FadeIn = .04f, Hold = .03f, FadeOut = .16f, Peak = .85f, Smoke = true, Scroll = .22f,
                    ScaleFrom = new Vector3(.85f, .6f, .85f), ScaleTo = new Vector3(1.1f, 1.15f, 1.1f) }),
            Part("Vanish_Cinders", Smoke(smoke, "M_Warp_Cinders", Root + "/WarpAttackVFX/Textures/T_Warp_Cinders.png", .4f),
                new KatsuroTimedVFX.Track { Delay = .03f, FadeIn = .03f, Hold = .05f, FadeOut = .16f, Peak = 1f, Smoke = true,
                    ScaleFrom = new Vector3(1f, .9f, 1f), ScaleTo = new Vector3(1.15f, 1.3f, 1.15f) }),
        });

        // --- Warp appear (cold converge + flash + shock, pivot = convergence point)
        var appear = BuildPrefab(AppearPrefab, WarpFbx, new[]
        {
            Part("Appear_Converge", Additive(additive, "M_Warp_Converge", Root + "/WarpAttackVFX/Textures/T_Warp_Converge.png", new Color(.85f, .93f, 1f)),
                new KatsuroTimedVFX.Track { FadeIn = .015f, Hold = .03f, FadeOut = .02f, Peak = 7f,
                    ScaleFrom = Vector3.one, ScaleTo = Vector3.one * .04f, EaseIn = true }),
            Part("Appear_Flash", Additive(additive, "M_Warp_Flash", Root + "/WarpAttackVFX/Textures/T_Warp_Flash.png", new Color(.9f, .96f, 1f)),
                new KatsuroTimedVFX.Track { Delay = .05f, FadeIn = .01f, Hold = .015f, FadeOut = .07f, Peak = 9f,
                    ScaleFrom = Vector3.one * .6f, ScaleTo = Vector3.one * 1.1f }),
            Part("Appear_Shock", Additive(additive, "M_Warp_Shock", Root + "/WarpAttackVFX/Textures/T_Warp_Shock.png", new Color(.8f, .9f, 1f)),
                new KatsuroTimedVFX.Track { Delay = .055f, FadeIn = .01f, Hold = 0f, FadeOut = .12f, Peak = .6f,
                    ScaleFrom = Vector3.one * .3f, ScaleTo = Vector3.one * 1.3f }),
        });

        if (issen == null || vanish == null || appear == null)
        {
            Debug.LogWarning("[ArtVFX] Some meshes were not found in the FBX files. Prefabs that could not be built were skipped.");
        }
        AssignReferences(issen, vanish, appear);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArtVFX] Issen / Warp VFX prefabs built and connected (Assets/Art/Prefabs).");
    }

    private struct PartSpec
    {
        public string Mesh;
        public Material Material;
        public KatsuroTimedVFX.Track Track;
    }

    private static PartSpec Part(string mesh, Material material, KatsuroTimedVFX.Track track) =>
        new PartSpec { Mesh = mesh, Material = material, Track = track };

    private static KatsuroTimedVFX BuildPrefab(string path, string fbx, PartSpec[] parts)
    {
        var meshes = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Mesh>().ToArray();
        var root = new GameObject(Path.GetFileNameWithoutExtension(path));
        try
        {
            var vfx = root.AddComponent<KatsuroTimedVFX>();
            var tracks = new KatsuroTimedVFX.Track[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var mesh = meshes.FirstOrDefault(m => m.name == parts[i].Mesh);
                if (mesh == null || parts[i].Material == null)
                {
                    Debug.LogWarning($"[ArtVFX] Mesh '{parts[i].Mesh}' not found in {fbx}.");
                    return null;
                }
                Debug.Log($"[ArtVFX] {Path.GetFileNameWithoutExtension(path)}/{mesh.name} bounds center={mesh.bounds.center:F2} size={mesh.bounds.size:F2}");
                var child = new GameObject(parts[i].Mesh);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = parts[i].Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderer.allowOcclusionWhenDynamic = false;
                parts[i].Track.Renderer = renderer;
                tracks[i] = parts[i].Track;
            }
            vfx.EditorSetTracks(tracks);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        return AssetDatabase.LoadAssetAtPath<KatsuroTimedVFX>(path);
    }

    private static Material Additive(Shader shader, string name, string texture, Color tint)
    {
        var material = LoadOrCreate(shader, name);
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        material.SetColor("_Color", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Smoke(Shader shader, string name, string texture, float phaseSpread)
    {
        var material = LoadOrCreate(shader, name);
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        material.SetColor("_Color", Color.white);
        material.SetFloat("_PhaseSpread", phaseSpread);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreate(Shader shader, string name)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        return material;
    }

    private static void ConfigureImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/IssenSlash/Textures", Root + "/WarpAttackVFX/Textures" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            bool ring = path.EndsWith("T_Warp_Shock.png");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapModeU = ring ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        foreach (string path in new[] { IssenFbx, WarpFbx })
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
        }
    }

    private static void AssignReferences(KatsuroTimedVFX issen, KatsuroTimedVFX vanish, KatsuroTimedVFX appear)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:VFXConfig"))
        {
            var config = AssetDatabase.LoadAssetAtPath<VFXConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (config == null) continue;
            var so = new SerializedObject(config);
            SetIfEmpty(so, "_counterIssenSlash", issen);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyAttackPattern"))
        {
            var pattern = AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(AssetDatabase.GUIDToAssetPath(guid));
            if (pattern == null || !pattern.IsWarp) continue;
            var so = new SerializedObject(pattern);
            SetIfEmpty(so, nameof(EnemyAttackPattern.WarpVanishVFX), vanish);
            SetIfEmpty(so, nameof(EnemyAttackPattern.WarpAppearVFX), appear);
            SetIfEmpty(so, nameof(EnemyAttackPattern.WarpSlashVFX), issen);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetIfEmpty(SerializedObject so, string property, Object value)
    {
        var prop = so.FindProperty(property);
        if (prop == null || value == null || prop.objectReferenceValue != null) return;
        prop.objectReferenceValue = value;
        EditorUtility.SetDirty(so.targetObject);
    }
}
