using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public static class InspectJustAvoidRendering
{
    public static string GameCamera()
    {
        var path="Assets/Mock/Scenes/GameScene.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var b=new StringBuilder();
        try
        {
            foreach(var camera in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)))
            {
                b.AppendLine(camera.name+" enabled="+camera.enabled+" rect="+camera.rect+" HDR="+camera.allowHDR+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(camera.gameObject));
                var hd=camera.GetComponent<HDAdditionalCameraData>();if(hd!=null)b.AppendLine("HD customSettings="+hd.customRenderingSettings+" customPass="+hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.CustomPass)+" postprocess="+hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.Postprocess)+" AA="+hd.antialiasing);
            }
            foreach(var v in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Volume>(true)))b.AppendLine("Volume "+v.name+" profile="+AssetDatabase.GetAssetPath(v.sharedProfile));
        }
        finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        return b.ToString();
    }
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("Unity="+Application.unityVersion+" Play="+EditorApplication.isPlaying+" pipeline="+AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline));
        var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
        if(asset!=null)b.AppendLine("HDRP supportCustomPass="+asset.currentPlatformRenderPipelineSettings.supportCustomPass);
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Mock/Prefabs/Manager"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var camera in root.GetComponentsInChildren<Camera>(true))
            {
                var hd=camera.GetComponent<HDAdditionalCameraData>();
                b.AppendLine("CAMERA "+path+" name="+camera.name+" tag="+camera.tag+" type="+camera.cameraType+" rect="+camera.rect+" HDR="+camera.allowHDR+" target="+camera.targetTexture);
                if(hd!=null)b.AppendLine("HD Camera "+EditorJsonUtility.ToJson(hd));
            }
            foreach(var volume in root.GetComponentsInChildren<CustomPassVolume>(true))b.AppendLine("CustomPass "+path+" "+EditorJsonUtility.ToJson(volume));
            var manager=root.GetComponent<CameraManager>();if(manager!=null)b.AppendLine("CameraManager "+EditorJsonUtility.ToJson(manager));
        }
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");
        foreach(var camera in Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()))
        {
            b.AppendLine("Scene camera "+camera.name+" scene="+camera.gameObject.scene.path+" tag="+camera.tag+" enabled="+camera.enabled+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(camera.gameObject));
            var hd=camera.GetComponent<HDAdditionalCameraData>();if(hd!=null)b.AppendLine("Scene HD Camera "+EditorJsonUtility.ToJson(hd));
        }
        b.AppendLine("VFXConfig "+EditorJsonUtility.ToJson(config));
        if(config.JustAvoidShockwavePrefab!=null)
        {
            b.AppendLine("Particle Shockwave="+AssetDatabase.GetAssetPath(config.JustAvoidShockwavePrefab));
            foreach(var p in config.JustAvoidShockwavePrefab.GetComponentsInChildren<ParticleSystem>(true))b.AppendLine(p.name+" duration="+p.main.duration+" lifetime="+p.main.startLifetime.constant+" shader="+p.GetComponent<ParticleSystemRenderer>().sharedMaterial?.shader.name);
        }
        foreach(var volume in Resources.FindObjectsOfTypeAll<Volume>().Where(v=>v.gameObject.scene.IsValid()))b.AppendLine("Scene Volume "+volume.name+" profile="+AssetDatabase.GetAssetPath(volume.sharedProfile)+" weight="+volume.weight);
        return b.ToString();
    }
}
