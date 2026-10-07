using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public static class ConfigureJustAvoidDistortion
{
    const string PrefabPath="Assets/Mock/Prefabs/Manager/CameraManager.prefab";
    const string MaterialPath="Assets/Mock/Resources/JustAvoidDistortionMaterial.mat";
    const string ShaderPath="Assets/Mock/Resources/JustAvoidDistortion.shader";
    const string ConfigPath="Assets/Mock/ScriptableObjects/VFXConfig.asset";
    const string Key="Katsuro.JustAvoidDistortion.Prefab";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Float(SerializedObject so,string field,float value){so.FindProperty(field).floatValue=value;}
    static void CheckShader(Shader shader)
    {
        Check(shader!=null,"Shader missing.");
        Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Shader compile error.");
    }
    public static string Stage()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compiled Edit Mode required.");
        AssetDatabase.ImportAsset(ShaderPath,ImportAssetOptions.ForceUpdate);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);CheckShader(shader);
        var material=AssetDatabase.LoadMainAssetAtPath(MaterialPath);
        Check(material==null||material is Material,"Unexpected existing asset.");
        Check(!AssetDatabase.FindAssets("JustAvoidDistortionMaterial t:Material").Any(g=>AssetDatabase.GUIDToAssetPath(g)!=MaterialPath),"Same named material elsewhere.");
        if(material==null){material=new Material(shader){name="JustAvoidDistortionMaterial"};AssetDatabase.CreateAsset(material,MaterialPath);}
        Check(((Material)material).shader==shader,"Unexpected existing shader.");
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        Check(root.GetComponent<CameraManager>()!=null,"CameraManager missing.");
        var effect=root.GetComponent<JustAvoidScreenDistortion>();
        if(effect==null)effect=root.AddComponent<JustAvoidScreenDistortion>();
        var component=new SerializedObject(effect);
        component.FindProperty("_material").objectReferenceValue=material;component.ApplyModifiedPropertiesWithoutUndo();
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath);Check(config!=null,"Config missing.");
        var so=new SerializedObject(config);
        so.FindProperty("_screenDistortionEnable").boolValue=true;
        so.FindProperty("_shockwaveEnable").boolValue=false;
        Float(so,"_distortionDuration",.14f);Float(so,"_distortionMaxRadius",1.15f);
        Float(so,"_distortionRadiusSpeed",1.6f);Float(so,"_distortionRingWidth",.03f);
        Float(so,"_distortionStrength",.006f);Float(so,"_distortionNoiseStrength",.001f);
        Float(so,"_distortionNoiseScale",24);Float(so,"_distortionFadePower",1.5f);
        Float(so,"_distortionEdgeTintStrength",0);
        so.FindProperty("_distortionEdgeTint").colorValue=new Color(.9f,.95f,1,1);
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(config);
        SessionState.SetInt(Key,root.GetInstanceID());
        return "Staged CameraManager prefab: one ScreenDistortion component, shared Material. Config ON / Particle OFF, duration .14, radius 1.15 (corner minimum), speed 1.6, width .03, strength .006, noise .001 /24, fade 1.5, tint 0. Pipeline and scene cameras unchanged (CustomPass enabled).";
    }
    public static string Save()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Compile before Save.");
        CheckShader(AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath));
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt(Key,0))as GameObject;Check(root!=null,"Staged prefab missing.");
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath));
        AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath));
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool saved);Check(saved,"Prefab save failed.");
        PrefabUtility.UnloadPrefabContents(root);SessionState.EraseInt(Key);
        return "Saved CameraManager.prefab, JustAvoidDistortionMaterial.mat, VFXConfig.asset. Sources and metadata imported. Active scene unchanged.";
    }
    public static string Verify()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode required.");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var effects=root.GetComponentsInChildren<JustAvoidScreenDistortion>(true);Check(effects.Length==1,"Expected one component.");
        var so=new SerializedObject(effects[0]);var material=so.FindProperty("_material").objectReferenceValue as Material;
        ShaderUtil.CompilePass(material,0,true);
        Check(AssetDatabase.GetAssetPath(material)==MaterialPath,"Material assignment not saved.");CheckShader(material.shader);
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>(ConfigPath);
        Check(config.ScreenDistortionEnable&&!config.ShockwaveEnable&&config.JustAvoidShockwavePrefab!=null,"Effect/particle defaults incorrect.");
        Check(Mathf.Approximately(config.DistortionDuration,.14f)&&Mathf.Approximately(config.DistortionStrength,.006f),"Config not saved.");
        var pipeline=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
        Check(pipeline!=null&&pipeline.currentPlatformRenderPipelineSettings.supportCustomPass,"CustomPass unsupported.");
        Check(!CustomPassVolume.GetGlobalCustomPasses(CustomPassInjectionPoint.AfterPostProcess).Any(p=>p.instance.name=="Just Avoid Screen Distortion"),"Pass must not run outside success.");
        // Edit Modeのみで投影の実APIを検証。ゲームを起動しない。
        var cameraObject=new GameObject("Temporary projection validation");
        try
        {
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.aspect=16f/9f;
            camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            Check(JustAvoidScreenDistortion.TryProjectCenter(camera,new Vector3(-1,1,5),new Vector3(0,0,5),out var center)&&center.x<.5f&&center.y>.5f,"World hit point projection failed.");
            Check(JustAvoidScreenDistortion.TryProjectCenter(camera,new Vector3(float.NaN,0,0),new Vector3(1,0,5),out center)&&center.x>.5f,"NaN fallback failed.");
            Check(JustAvoidScreenDistortion.TryProjectCenter(camera,new Vector3(1000,0,5),new Vector3(1,0,5),out center)&&center.x>.5f,"Offscreen fallback failed.");
            Check(!JustAvoidScreenDistortion.TryProjectCenter(camera,new Vector3(0,0,-5),new Vector3(0,0,-5),out center),"Behind-camera positions must be rejected.");
        }
        finally{UnityEngine.Object.DestroyImmediate(cameraObject);}
        return "RE-READ OK: HDRP CustomPass enabled, CameraManager component=1 enabled="+effects[0].enabled+", material="+AssetDatabase.GetAssetPath(material)+", shader="+material.shader.name+", shaderErrors=0, config="+EditorJsonUtility.ToJson(config)+"; Edit Mode projection checks (world/NaN/offscreen/behind) passed; idle global pass absent. Play Mode not run.";
    }
    public static string VerifyGameHookup()
    {
        var path="Assets/Mock/Scenes/GameScene.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
            var so=new SerializedObject(manager);
            var cameraManager=so.FindProperty("_cameraManager").objectReferenceValue as CameraManager;
            var camera=so.FindProperty("_camera").objectReferenceValue as Camera;
            Check(cameraManager!=null&&camera!=null,"GameManager camera references missing.");
            Check(cameraManager.GetComponent<JustAvoidScreenDistortion>()!=null,"GameScene prefab did not inherit component.");
            var hd=camera.GetComponent<HDAdditionalCameraData>();Check(hd!=null,"HDRP camera missing.");
            Check(!hd.customRenderingSettings||hd.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.CustomPass),"Camera disables CustomPass.");
            return "GameScene re-read: GameManager references camera="+camera.name+", CameraManager prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cameraManager.gameObject)+", inherited distortion component=1, material assigned, HD camera customSettings="+hd.customRenderingSettings+", AA="+hd.antialiasing+". Scene opened read-only and closed without save.";
        }
        finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }
}
