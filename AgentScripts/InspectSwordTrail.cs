using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectSwordTrail
{
    static string Path(Transform t)=>t==null?"null":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    static void States(AnimatorStateMachine sm,StringBuilder b)
    {
        foreach(var s in sm.states)
        {
            if(!(s.state.motion is AnimationClip clip))continue;
            if(!clip.events.Any(e=>e.functionName.Contains("Hitbox")))continue;
            b.AppendLine("Attack "+s.state.name+" tag="+s.state.tag+" clip="+AssetDatabase.GetAssetPath(clip)+" length="+clip.length+" speed="+s.state.speed+" events="+string.Join(",",clip.events.Select(e=>e.functionName+"@"+e.time)));
        }
        foreach(var child in sm.stateMachines)States(child.stateMachine,b);
    }
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("Play="+EditorApplication.isPlaying+" pipeline="+GraphicsSettings.currentRenderPipeline);
        foreach(var path in new[]{"Assets/Mock/Prefabs/Player.prefab","Assets/Mock/Prefabs/Enemy.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);b.AppendLine(path);
            foreach(var trail in root.GetComponentsInChildren<SwordTrail>(true))
            {
                var so=new SerializedObject(trail);
                b.AppendLine(Path(trail.transform)+" "+EditorJsonUtility.ToJson(trail));
                var a=so.FindProperty("_bladeBase").objectReferenceValue as Transform;var tip=so.FindProperty("_tip").objectReferenceValue as Transform;
                b.AppendLine(" Base="+Path(a)+" Tip="+Path(tip)+" bladeLength="+(a!=null&&tip!=null?Vector3.Distance(a.position,tip.position):0));
                var legacy=trail.GetComponent<TrailRenderer>();if(legacy!=null)b.AppendLine(" Legacy enabled="+legacy.enabled+" emission="+legacy.emitting+" material="+AssetDatabase.GetAssetPath(legacy.sharedMaterial));
            }
            var animator=root.GetComponent<Animator>();if(animator!=null){b.AppendLine("Animator "+AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));foreach(var l in ((AnimatorController)animator.runtimeAnimatorController).layers)States(l.stateMachine,b);}
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Mock"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.shader!=null&&m.shader.name=="Katsuro/BladeTrail")b.AppendLine("Trail Material="+AssetDatabase.GetAssetPath(m)+" queue="+m.renderQueue+" "+EditorJsonUtility.ToJson(m));
        }
        foreach(var guid in AssetDatabase.FindAssets("t:VolumeProfile",new[]{"Assets/Mock"}))
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(guid));if(profile.TryGet<Bloom>(out var bloom))b.AppendLine("Bloom "+AssetDatabase.GetAssetPath(profile)+" active="+bloom.active+" intensity="+bloom.intensity.value+" threshold="+bloom.threshold.value);
        }
        foreach(var volume in Resources.FindObjectsOfTypeAll<Volume>().Where(v=>v.gameObject.scene.IsValid()))
        {
            var profile=volume.sharedProfile;
            if(profile!=null && profile.TryGet<Bloom>(out var bloom))b.AppendLine("Scene Bloom "+volume.name+" profile="+AssetDatabase.GetAssetPath(profile)+" enabled="+volume.enabled+" weight="+volume.weight+" active="+bloom.active+" intensity="+bloom.intensity.value+" threshold="+bloom.threshold.value);
        }
        return b.ToString();
    }
}
