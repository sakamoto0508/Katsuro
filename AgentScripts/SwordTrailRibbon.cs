using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class SwordTrailRibbon
{
    const string Player="Assets/Mock/Prefabs/Player.prefab", Mat="Assets/Mock/Resources/SwordTrailSteel.mat";
    static readonly string[] Props={"_BodyAlpha","_BodyWidth","_CoreWidth","_CoreBrightness","_AfterGlowAlpha","_TailFade","_TipBrightness","_FadePower"};
    static readonly float[] Values={.14f,.12f,.022f,2.8f,.03f,.4f,1.35f,1.8f};
    static string Path(Transform t) => t==null?"null":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    static string Signature(GameObject root)
    {
        var s=new StringBuilder();
        foreach(var t in root.GetComponentsInChildren<SwordTrail>(true))
        {
            var so=new SerializedObject(t);
            s.AppendLine(Path(t.transform)+" base="+Path((so.FindProperty("_bladeBase").objectReferenceValue as Transform))+" tip="+Path(so.FindProperty("_tip").objectReferenceValue as Transform)+" shared="+Path((so.FindProperty("_sharedTrail").objectReferenceValue as SwordTrail)?.transform));
        }
        foreach(var a in root.GetComponentsInChildren<Animator>(true))
            if(a.runtimeAnimatorController!=null)
                foreach(var c in a.runtimeAnimatorController.animationClips.Distinct().OrderBy(c=>AssetDatabase.GetAssetPath(c)))
                    foreach(var e in AnimationUtility.GetAnimationEvents(c))s.AppendLine(AssetDatabase.GetAssetPath(c)+":"+e.time.ToString("R")+":"+e.functionName+":"+e.stringParameter+":"+e.floatParameter+":"+e.intParameter);
        return s.ToString();
    }
    public static string Inspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(Player);
        var s=new StringBuilder(); var signature=Signature(root);
        var before=SessionState.GetString("Ribbon.references","");
        if(before.Length==0)SessionState.SetString("Ribbon.references",signature);
        else if(before!=signature)throw new Exception("Blade references or animation events changed");
        s.AppendLine("Blade references and "+signature.Split('\n').Length+" reference/event records checked.");
        var trails=root.GetComponentsInChildren<SwordTrail>(true);
        if(trails.Length!=5)throw new Exception("Expected five SwordTrail components");
        int owners=0;
        foreach(var t in trails)
        {
            var so=new SerializedObject(t);var shared=so.FindProperty("_sharedTrail").objectReferenceValue as SwordTrail;
            if(shared==null)
            {
                owners++; var b=so.FindProperty("_bladeBase").objectReferenceValue as Transform;var tip=so.FindProperty("_tip").objectReferenceValue as Transform;
                if(b==null||tip==null)throw new Exception("Owner endpoints missing");
                s.AppendLine("Owner blade length="+Vector3.Distance(b.position,tip.position)+" base local="+b.localPosition+" tip local="+tip.localPosition);
            }
            else if(!trails.Contains(shared)||new SerializedObject(shared).FindProperty("_sharedTrail").objectReferenceValue!=null)throw new Exception("Invalid shared owner");
            s.Append(Path(t.transform));
            foreach(var n in new[]{"_bladeTrailStart","_duration","_heavyDuration","_counterDuration","_minimumSpeed","_fullSpeed","_heavyBrightness","_counterBrightness","_brightness","_fade"})
            {var p=so.FindProperty(n);if(p!=null)s.Append(" "+n+"="+p.floatValue);}
            s.AppendLine(" material="+AssetDatabase.GetAssetPath(so.FindProperty("_trailMaterial").objectReferenceValue));
            var legacy=t.GetComponent<TrailRenderer>();if(legacy!=null&&legacy.enabled)throw new Exception("Legacy renderer enabled");
        }
        if(owners!=1)throw new Exception("Expected one shared mesh owner");
        var m=AssetDatabase.LoadAssetAtPath<Material>(Mat);
        foreach(var p in Props)s.AppendLine(p+"="+m.GetFloat(p));
        s.AppendLine("References/events preserved; one owner + four shared; legacy renderers disabled. Shader messages: "+string.Join(";",ShaderUtil.GetShaderMessages(m.shader).Select(x=>x.severity+":"+x.message)));
        return s.ToString();
    }
    public static string Configure()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Compiled Edit Mode required");
        var m=AssetDatabase.LoadAssetAtPath<Material>(Mat);
        for(int i=0;i<Props.Length;i++)m.SetFloat(Props[i],Values[i]);
        ShaderUtil.CompilePass(m,0,true);
        if(ShaderUtil.GetShaderMessages(m.shader).Any(x=>x.severity.ToString()=="Error"))throw new Exception("Shader compile error");
        EditorUtility.SetDirty(m);
        var root=PrefabUtility.LoadPrefabContents(Player);
        try
        {
            if(Signature(root)!=SessionState.GetString("Ribbon.references",""))throw new Exception("References/events differ before edit");
            foreach(var t in root.GetComponentsInChildren<SwordTrail>(true))
            {
                var so=new SerializedObject(t);
                string[] names={"_bladeTrailStart","_duration","_heavyDuration","_counterDuration","_minimumSpeed","_fullSpeed"};
                float[] values={.48f,.11f,.13f,.15f,3.5f,16f};
                for(int i=0;i<names.Length;i++)so.FindProperty(names[i]).floatValue=values[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root,Player,out bool saved);if(!saved)throw new Exception("Prefab save failed");
            AssetDatabase.SaveAssetIfDirty(m);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return "Saved Player prefab and SwordTrailSteel material; shader pass compiled without errors.\n"+Inspect();
    }
    public static string ValidateSampling()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var root=PrefabUtility.LoadPrefabContents(Player);
        try
        {
            var t=root.GetComponentsInChildren<SwordTrail>(true).Single(x=>new SerializedObject(x).FindProperty("_sharedTrail").objectReferenceValue==null);
            var so=new SerializedObject(t);
            var b=so.FindProperty("_bladeBase").objectReferenceValue as Transform;
            var tip=so.FindProperty("_tip").objectReferenceValue as Transform;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            Action<string,object> set=(n,v)=>typeof(SwordTrail).GetField(n,flags).SetValue(t,v);
            Func<string,object> get=n=>typeof(SwordTrail).GetField(n,flags).GetValue(t);
            Action<float> sample=speed=>{
                set("count",0);set("hasPrevious",true);set("previousTime",Time.unscaledTime-.1f);
                set("previousTip",tip.position-Vector3.right*speed*.1f);set("speedStrength",0f);
                typeof(SwordTrail).GetMethod("Sample",flags).Invoke(t,null);
            };
            float[] durations={.11f,.13f,.15f}, widths={1f,1.04f,1.08f}, brightness={1f,1.18f,1.35f};
            for(int i=0;i<3;i++)
            {
                t.SetStyle((SwordTrail.AttackStyle)i);sample(20f);
                var bases=(Vector3[])get("bases");var tips=(Vector3[])get("tips");
                if(Vector3.Distance(bases[0],Vector3.Lerp(b.position,tip.position,.48f))>.00001f||tips[0]!=tip.position)throw new Exception("Mesh endpoints incorrect");
                if(Mathf.Abs(((float[])get("lifetimes"))[0]-durations[i])>.0001f||Mathf.Abs(((float[])get("sampleWidths"))[0]-widths[i])>.0001f||Mathf.Abs(((float[])get("strengths"))[0]-brightness[i])>.0001f)throw new Exception("Style sample settings incorrect");
            }
            sample(2f);if(((float[])get("strengths"))[0]!=0f)throw new Exception("Slow seed must be invisible");
            return "Edit Mode sampling passed: real endpoints => outer52% mesh; fast Light/Heavy/Counter duration, width and brightness verified; 2m/s seed alpha strength zero. No Play Mode or visual test.";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
