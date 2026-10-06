using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Text;
using System.Linq;
public static class InspectEnemyReaction
{
    static void States(AnimatorStateMachine sm,string prefix,StringBuilder b)
    {
        foreach(var child in sm.states)
        {
            var s=child.state;
            b.AppendLine("STATE "+prefix+s.name+" motion="+AssetDatabase.GetAssetPath(s.motion)+" speed="+s.speed+" writeDefaults="+s.writeDefaultValues);
            foreach(var t in s.transitions)b.AppendLine(" transition -> "+(t.isExit?"Exit":t.destinationState!=null?t.destinationState.name:t.destinationStateMachine?.name)+" exit="+t.hasExitTime+":"+t.exitTime+" duration="+t.duration+" conditions="+string.Join(",",t.conditions.Select(c=>c.parameter+":"+c.mode+":"+c.threshold)));
        }
        foreach(var t in sm.anyStateTransitions)b.AppendLine("ANY -> "+t.destinationState?.name+" conditions="+string.Join(",",t.conditions.Select(c=>c.parameter+":"+c.mode+":"+c.threshold)));
        foreach(var c in sm.stateMachines)States(c.stateMachine,prefix+c.stateMachine.name+"/",b);
    }
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("Play="+EditorApplication.isPlaying+" compiling="+EditorApplication.isCompiling);
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        b.AppendLine("ENEMY "+root+" controller="+EditorJsonUtility.ToJson(root.GetComponent<EnemyController>())+" feedback="+EditorJsonUtility.ToJson(root.GetComponent<CombatFeedback>()));
        foreach(var a in root.GetComponentsInChildren<Animator>(true))
        {
            b.AppendLine("ANIMATOR "+a.name+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController)+" avatar="+AssetDatabase.GetAssetPath(a.avatar)+" human="+a.isHuman+" valid="+(a.avatar!=null&&a.avatar.isValid)+" rootMotion="+a.applyRootMotion+" culling="+a.cullingMode+" update="+a.updateMode);
            var controller=a.runtimeAnimatorController as AnimatorController;
            if(controller==null)continue;
            foreach(var p in controller.parameters)b.AppendLine("PARAM "+p.name+" "+p.type);
            foreach(var l in controller.layers){b.AppendLine("LAYER "+l.name+" weight="+l.defaultWeight+" mode="+l.blendingMode+" mask="+AssetDatabase.GetAssetPath(l.avatarMask)+" default="+l.stateMachine.defaultState?.name);States(l.stateMachine,l.name+"/",b);}
            foreach(var c in controller.animationClips.Distinct())b.AppendLine("USED CLIP "+AssetDatabase.GetAssetPath(c)+" human="+c.humanMotion+" length="+c.length+" loop="+c.isLooping);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/InportAssets/Anim","Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid","Assets/InportAssets/ARPGPack/ARPGSamurai/Additional_Animations/Humanoid"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var name=System.IO.Path.GetFileNameWithoutExtension(path);
            if(!name.Contains("Hit")||name.Contains("Guard")||name.Contains("Shield")||name.Contains("Air")||name.Contains("knockdown")||name.Contains("Climb"))continue;
            var c=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(c==null)continue;
            var settings=AnimationUtility.GetAnimationClipSettings(c);
            b.AppendLine("CANDIDATE "+path+" human="+c.humanMotion+" legacy="+c.legacy+" length="+c.length+" loop="+settings.loopTime+" root="+c.hasRootCurves+" keepXZ="+settings.keepOriginalPositionXZ+" bakeXZ="+settings.loopBlendPositionXZ+" curves="+AnimationUtility.GetCurveBindings(c).Length+" events="+c.events.Length);
        }
        return b.ToString();
    }
}
