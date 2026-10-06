using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectCounterReaction
{
    static void States(AnimatorStateMachine sm, string path, StringBuilder b)
    {
        foreach (var child in sm.states)
        {
            var s = child.state;
            b.AppendLine(path + s.name + " clip=" + AssetDatabase.GetAssetPath(s.motion));
            foreach (var t in s.transitions) b.AppendLine(" -> " + t.destinationState?.name + " " + string.Join(",", t.conditions.Select(x => x.parameter + ":" + x.mode + ":" + x.threshold)));
            if (s.motion is AnimationClip clip && (path+s.name).ToLowerInvariant().Contains("just"))
                b.AppendLine(" events=" + string.Join(",", clip.events.Select(e => e.functionName + "@" + e.time)));
        }
        foreach (var t in sm.anyStateTransitions) b.AppendLine(path + "ANY -> " + t.destinationState?.name + " " + string.Join(",", t.conditions.Select(x => x.parameter + ":" + x.mode + ":" + x.threshold)));
        foreach (var c in sm.stateMachines) States(c.stateMachine, path + c.stateMachine.name + "/", b);
    }
    public static string Main()
    {
        var b=new StringBuilder(); b.AppendLine("Play="+EditorApplication.isPlaying);
        foreach(var path in new[]{"Assets/Mock/Prefabs/Enemy.prefab","Assets/Mock/Prefabs/Player.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(root==null){b.AppendLine("Missing "+path);continue;}
            b.AppendLine(path);
            foreach(var a in root.GetComponentsInChildren<Animator>(true))
            {
                b.AppendLine("Animator="+a.name+" controller="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController));
                var c=a.runtimeAnimatorController as AnimatorController;if(c==null)continue;
                b.AppendLine("Params="+string.Join(",",c.parameters.Select(p=>p.name+":"+p.type)));
                foreach(var l in c.layers)States(l.stateMachine,l.name+"/",b);
            }
            foreach(var component in root.GetComponents<MonoBehaviour>())
                if(component!=null && (component is PlayerController || component is EnemyAnimationController))
                    b.AppendLine(EditorJsonUtility.ToJson(component));
        }
        foreach(var guid in AssetDatabase.FindAssets("t:HitStopManager"))b.AppendLine("HitStop asset="+AssetDatabase.GUIDToAssetPath(guid));
        foreach(var guid in AssetDatabase.FindAssets("t:PlayerStateConfig"))
        {
            var config=AssetDatabase.LoadAssetAtPath<PlayerStateConfig>(AssetDatabase.GUIDToAssetPath(guid));
            b.AppendLine("Config "+AssetDatabase.GetAssetPath(config)+" JustAvoidAttackClips="+string.Join(",",config.JustAvoidAttackClips.Select(x=>AssetDatabase.GetAssetPath(x))));
        }
        return b.ToString();
    }
}
