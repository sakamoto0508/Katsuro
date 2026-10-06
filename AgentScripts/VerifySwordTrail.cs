using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
public static class VerifySwordTrail
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static FieldInfo F(string name)=>typeof(SwordTrail).GetField(name,Flags);
    static void Set(SwordTrail trail,string name,object value)=>F(name).SetValue(trail,value);
    static T Get<T>(SwordTrail trail,string name)=>(T)F(name).GetValue(trail);
    static void Call(SwordTrail trail,string name)=>typeof(SwordTrail).GetMethod(name,Flags).Invoke(trail,null);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static AnimatorState[] States(AnimatorStateMachine sm)=>sm.states.Select(s=>s.state).Concat(sm.stateMachines.SelectMany(s=>States(s.stateMachine))).ToArray();
    public static string Main()
    {
        Check(!EditorApplication.isPlaying&&!EditorApplication.isCompiling,"Edit Mode required.");
        var b=new StringBuilder();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        var trails=prefab.GetComponentsInChildren<SwordTrail>(true);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Resources/SwordTrailSteel.mat");
        Check(material!=null&&material.shader.name=="Katsuro/BladeTrail"&&material.renderQueue==3000,"Saved material and transparent queue.");
        var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity.ToString()=="Error").ToArray();Check(errors.Length==0,"Shader errors: "+string.Join(",",errors.Select(m=>m.message)));
        Check(material.GetFloat("_CoreWidth")<.05f&&material.GetFloat("_CoreBrightness")>2&&material.GetFloat("_AfterGlowAlpha")<material.GetFloat("_BodyAlpha"),"Thin bright Core and weaker AfterGlow.");
        var leader=trails.Single(t=>new SerializedObject(t).FindProperty("_sharedTrail").objectReferenceValue==null);
        foreach(var trail in trails)
        {
            var so=new SerializedObject(trail);
            Check(so.FindProperty("_trailMaterial").objectReferenceValue==material,"Material assigned to all trail components.");
            Check(Mathf.Approximately(so.FindProperty("_duration").floatValue,.11f)&&Mathf.Approximately(so.FindProperty("_heavyDuration").floatValue,.16f)&&Mathf.Approximately(so.FindProperty("_counterDuration").floatValue,.18f),"Saved lifetimes.");
            Check(trail==leader||so.FindProperty("_sharedTrail").objectReferenceValue==leader,"One mesh owner for five colliders.");
            var legacy=trail.GetComponent<TrailRenderer>();Check(legacy==null||!legacy.enabled&&!legacy.emitting,"Legacy line trail kept off.");
        }
        var leaderSo=new SerializedObject(leader);
        Check(leaderSo.FindProperty("_bladeBase").objectReferenceValue!=null&&leaderSo.FindProperty("_tip").objectReferenceValue!=null,"Existing blade endpoints retained.");
        var controller=(AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
        var attacks=States(controller.layers[0].stateMachine).Where(s=>s.motion is AnimationClip clip&&clip.events.Any(e=>e.functionName=="AnimEvent_EnableWeaponHitbox")).ToArray();
        Check(attacks.Length==13&&attacks.Count(s=>s.tag==PlayerAttacker.JustAvoidCounterTag)==2,"All 13 existing attack windows including 2 counter attacks.");
        foreach(var state in attacks)
        {
            var clip=(AnimationClip)state.motion;
            Check(clip.events.Any(e=>e.functionName=="AnimEvent_DisableWeaponHitbox"),"Every attack has an end event.");
        }
        var scene=EditorSceneManager.NewPreviewScene();Mesh mesh=null;SwordTrail sample=null;
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            sample=root.GetComponentsInChildren<SwordTrail>(true).Single(t=>Get<SwordTrail>(t,"_sharedTrail")==null);
            mesh=new Mesh();Set(sample,"mesh",mesh);Set(sample,"initialized",true);Set(sample,"owner",root.transform);
            var tip=Get<Transform>(sample,"_tip");
            foreach(var style in new[]{SwordTrail.AttackStyle.Light,SwordTrail.AttackStyle.Heavy,SwordTrail.AttackStyle.JustAvoidCounter})
            {
                sample.SetActive(false);sample.SetStyle(style);sample.SetActive(true);
                // Set only sample-clock inputs, then invoke the actual generator.
                Set(sample,"previousTime",Time.unscaledTime-.016f);Set(sample,"previousTip",tip.position-Vector3.right*.01f);Set(sample,"previousOwnerPosition",root.transform.position);
                Call(sample,"Sample");Check(Get<float>(sample,"speedStrength")<.001f,"Slow blade does not illuminate: "+style);
                tip.position += Vector3.right*.4f;
                Set(sample,"previousTime",Time.unscaledTime-.016f);Set(sample,"previousTip",tip.position-Vector3.right*.4f);Set(sample,"previousOwnerPosition",root.transform.position);
                Call(sample,"Sample");
                int count=Get<int>(sample,"count");Check(count>=2&&Get<float>(sample,"speedStrength")>.4f,"Fast blade illuminates: "+style);
                float life=Get<float[]>(sample,"lifetimes")[count-1];
                Check(life<=(style==SwordTrail.AttackStyle.Light?.11f:style==SwordTrail.AttackStyle.Heavy?.16f:.18f)+.001f,"Lifetime upper bound.");
                Call(sample,"LateUpdate");Check(mesh.vertexCount==count*2&&mesh.triangles.All(i=>i<mesh.vertexCount),"Ribbon mesh indices.");
                Check(mesh.uv.Length==mesh.vertexCount&&mesh.uv2.Length==mesh.vertexCount&&mesh.uv.All(uv=>uv.x>=0&&uv.x<=1)&&mesh.uv.Any(uv=>uv.y==0)&&mesh.uv.Any(uv=>uv.y==1),"Age UV and blade Root/Tip UV plus speed/width channel.");
                sample.SetActive(false);
                for(int i=0;i<count;i++)Get<float[]>(sample,"times")[i]=Time.unscaledTime-1;
                Call(sample,"LateUpdate");Check(mesh.vertexCount==0&&Get<int>(sample,"count")==0,"Stopped trail fully expires.");
                b.AppendLine(style+": fast samples generated; lifetime="+life+"; complete expiry PASS.");
            }
            // Owner translation alone must not create a sword-speed burst.
            sample.SetActive(true);Set(sample,"previousTime",Time.unscaledTime-.016f);Set(sample,"previousTip",tip.position-Vector3.right*.4f);Set(sample,"previousOwnerPosition",root.transform.position-Vector3.right*.4f);
            Call(sample,"Sample");Check(Get<float>(sample,"speedStrength")==0,"Owner translation excluded from blade speed.");
        }
        finally
        {
            if(sample!=null)Set(sample,"mesh",null);
            if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        b.AppendLine("PASS: C# and Shader checks; saved material/prefab/endpoints; shared owner; 13 attack event windows; slow/fast response; speed/age/width UVs; profile lifetime bounds; after-stop expiry; owner-motion rejection. Play Mode and visual gameplay test were not performed.");
        return b.ToString();
    }
}
