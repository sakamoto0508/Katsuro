using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class EditableUIRunner
{
    private static int count;
    private static void Check(bool value,string name) { if(!value) throw new Exception(name); count++; Debug.Log("PASS: "+name); }
    private static void Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    private static T Get<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    public static void Run()
    {
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/RunSetupCanvas.prefab");
            var setup=UnityEngine.Object.Instantiate(prefab);
            var view=setup.GetComponent<RunSetupUI>();
            var group=setup.GetComponent<CanvasGroup>();
            view.Init();
            Check(group.alpha==0 && !group.blocksRaycasts,"Setup starts hidden and does not block title");
            var title=new GameObject("Title test").AddComponent<TitleManager>();
            view.Open(title);
            view.Init();
            Check(view.IsOpen && group.alpha==1 && group.interactable,"Open activates authored panel");
            var input=Get<TMP_InputField>(view,"_nameInput");
            Check(input.characterLimit==16 && input.textComponent.font!=null,"Name field has font and character limit");
            var attacks=Get<Toggle[]>(view,"_attackOptions");
            var defenses=Get<Toggle[]>(view,"_defenseOptions");
            attacks[2].isOn=true; defenses[1].isOn=true;
            Check(attacks.Count(t=>t.isOn)==1 && defenses.Count(t=>t.isOn)==1,"Each equipment group has one selection");
            Check(Get<TMP_Text>(view,"_attackDescription").text==RunSession.AttackDescription(2),"Attack description follows uGUI selection");
            Check(Get<TMP_Text>(view,"_defenseDescription").text==RunSession.DefenseDescription(1),"Defense description follows uGUI selection");

            // Render an isolated preview; the saved prefab remains ScreenSpaceOverlay.
            var camera=new GameObject("Preview camera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            var rt=new RenderTexture(1280,720,24); camera.targetTexture=rt;
            var canvas=setup.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();
            foreach(var label in setup.GetComponentsInChildren<TMP_Text>()) label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render();
            var prior=RenderTexture.active; RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            File.WriteAllBytes(Path.GetFullPath("../setup-ui-preview.png"),image.EncodeToPNG());
            RenderTexture.active=prior; UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            view.Confirm();
            Check(!RunSession.Active,"Opening input cannot confirm in the same frame");
            input.text="UI検証";
            Set(view,"_openedFrame",-1);
            Get<Button>(view,"_startButton").onClick.Invoke();
            Check(RunSession.Active && RunSession.PlayerName=="UI検証" && RunSession.Attack==2 && RunSession.Defense==1,"Start button passes name and chosen equipment");
            Check(!view.IsOpen && !group.interactable,"Submitting locks the panel");
            UnityEngine.Object.DestroyImmediate(setup);

            prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/DamageNumbersCanvas.prefab");
            var damage=UnityEngine.Object.Instantiate(prefab);
            var numbers=damage.GetComponent<DamageNumbers>();
            var normal=Get<TMP_Text>(numbers,"_normalTemplate");
            normal.fontSize=42; normal.color=Color.cyan;
            numbers.Init();
            DamageNumbers.Prepare(camera);
            Canvas.ForceUpdateCanvases();
            DamageNumbers.Show(Vector3.zero,23,false);
            numbers.Init();
            var active=Get<TMP_Text[]>(numbers,"active"); var label0=active.First(t=>t!=null);
            label0.ForceMeshUpdate();
            Check(label0.fontSize==42 && label0.color.g==1 && label0.color.r==0,"Damage style comes from uGUI template");
            Check(label0.text=="23" && label0.alpha>0 && label0.textInfo.meshInfo[0].vertexCount>0,"Damage label produces visible text geometry");
            Check(!normal.gameObject.activeSelf,"Template stays hidden in combat");
            for(int i=0;i<80;i++) DamageNumbers.Show(Vector3.zero,i+1,i%2==0);
            Check(damage.GetComponentsInChildren<TMP_Text>(true).Length==66 && active.Count(t=>t!=null)==32,"Pool stays bounded: 32 visible slots, two styles");
            var starts=Get<float[]>(numbers,"started");
            for(int i=0;i<starts.Length;i++) starts[i]=Time.unscaledTime-10;
            Call(numbers,"LateUpdate");
            Check(active.All(t=>t==null),"Numbers expire without creating new objects");
            UnityEngine.Object.DestroyImmediate(damage);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(title.gameObject);
            Debug.Log("EDITABLE_UI_RESULT: "+count+" passed");
            EditorApplication.Exit(0);
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
