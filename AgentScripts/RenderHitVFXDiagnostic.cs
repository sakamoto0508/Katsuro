using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Text;
public static class RenderHitVFXDiagnostic
{
    public static string Main()=>Render("HitVFX",false);
    public static string Before()=>Render("HitVFX-before-spread",false);
    public static string After()=>Render("HitVFX-after-spread",true);
    static string Render(string label,bool staged)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var scene=EditorSceneManager.NewPreviewScene();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Effects/SwordHitVFX.prefab");
        var source=staged?EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroSpreadRoot",0)) as GameObject:prefab;
        if(source==null)throw new Exception("Preview source missing");
        var go=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(go,scene);
        var camGo=new GameObject("HitVFX diagnostic camera");SceneManager.MoveGameObjectToScene(camGo,scene);
        var camera=camGo.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;
        camera.orthographic=true;camera.orthographicSize=.55f;camera.nearClipPlane=.01f;camera.farClipPlane=10;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.012f,.012f,1);
        var hd=camGo.AddComponent<HDAdditionalCameraData>();hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=camera.backgroundColor;
        var rt=new RenderTexture(768,512,24,RenderTextureFormat.ARGBHalf);rt.Create();
        var b=new StringBuilder();
        try
        {
            foreach(var p in go.GetComponentsInChildren<ParticleSystem>()){p.useAutoRandomSeed=false;p.randomSeed=42;p.Simulate(.075f,false,true,true);}
            camGo.transform.position=new Vector3(2,0,.25f);camGo.transform.LookAt(new Vector3(0,0,.25f));
            var req=new RenderPipeline.StandardRequest{destination=rt};
            Check(RenderPipeline.SupportsRenderRequest(camera,req),"Render request unsupported");
            RenderPipeline.SubmitRenderRequest(camera,req);
            var prior=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();RenderTexture.active=prior;
            int redPixels=0;float maxRed=0;foreach(var color in tex.GetPixels()){if(color.r>color.g*1.5f&&color.r>color.b*1.5f&&color.r>.025f)redPixels++;maxRed=Mathf.Max(maxRed,color.r);}
            // RenderTextureはLinear。PNG表示用にsRGBへ変換する。
            var pixels=tex.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;tex.SetPixels(pixels);tex.Apply();
            var path=System.IO.Path.GetFullPath("AgentScripts/"+label+"-diagnostic.png");System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            b.AppendLine("Isolated HDRP side preview at 0.075s, 1.1m vertical view; redPixels="+redPixels+" maxRed="+maxRed+" image="+path+" PlayMode=False");
            camera.orthographicSize=2.5f;
            RenderPipeline.SubmitRenderRequest(camera,req);
            prior=RenderTexture.active;RenderTexture.active=rt;
            tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();RenderTexture.active=prior;
            redPixels=0;int minY=rt.height,maxY=0;pixels=tex.GetPixels();
            for(int i=0;i<pixels.Length;i++){var color=pixels[i];if(color.r>color.g*1.5f&&color.r>color.b*1.5f&&color.r>.025f){redPixels++;minY=Mathf.Min(minY,i/rt.width);maxY=Mathf.Max(maxY,i/rt.width);}pixels[i]=color.gamma;}
            tex.SetPixels(pixels);tex.Apply();path=System.IO.Path.GetFullPath("AgentScripts/"+label+"-game-scale.png");System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            b.AppendLine("5m vertical view, no character occlusion: redPixels="+redPixels+" total effect height="+(maxY-minY+1)+"px; image="+path);
            camGo.transform.position=new Vector3(0,0,2);camGo.transform.LookAt(new Vector3(0,0,.25f));
            RenderPipeline.SubmitRenderRequest(camera,req);
            prior=RenderTexture.active;RenderTexture.active=rt;
            tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();RenderTexture.active=prior;
            redPixels=0;minY=rt.height;maxY=0;pixels=tex.GetPixels();
            for(int i=0;i<pixels.Length;i++){var color=pixels[i];if(color.r>color.g*1.5f&&color.r>color.b*1.5f&&color.r>.025f){redPixels++;minY=Mathf.Min(minY,i/rt.width);maxY=Mathf.Max(maxY,i/rt.width);}pixels[i]=color.gamma;}
            tex.SetPixels(pixels);tex.Apply();path=System.IO.Path.GetFullPath("AgentScripts/"+label+"-front.png");System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            b.AppendLine("5m vertical front view: redPixels="+redPixels+" total height="+(maxY-minY+1)+"px; image="+path);
            return b.ToString();
        }
        finally{rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
