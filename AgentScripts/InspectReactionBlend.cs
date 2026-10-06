using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class InspectReactionBlend
{
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("Play="+EditorApplication.isPlaying);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var a=prefab.GetComponent<Animator>();var controller=(AnimatorController)a.runtimeAnimatorController;
        b.AppendLine("RootMotion="+a.applyRootMotion+" component="+EditorJsonUtility.ToJson(prefab.GetComponent<EnemyAnimationController>()));
        var layer=controller.layers.Single(l=>l.name=="HitReaction");
        b.AppendLine("Layer="+layer.name+" mode="+layer.blendingMode+" weight="+layer.defaultWeight+" rootMask="+layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root));
        foreach(var t in layer.stateMachine.anyStateTransitions.Concat(layer.stateMachine.states.SelectMany(s=>s.state.transitions)))
            b.AppendLine("To="+t.destinationState?.name+" duration="+t.duration+" fixed="+t.hasFixedDuration+" exit="+t.hasExitTime+":"+t.exitTime+" offset="+t.offset+" interruption="+t.interruptionSource+" ordered="+t.orderedInterruption);
        var scene=EditorSceneManager.NewPreviewScene();
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        var animator=root.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
        var graph=PlayableGraph.Create("Inspect start poses");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var output=AnimationPlayableOutput.Create(graph,"Pose",animator);
        var bones=new[]{HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
        var positions=new Vector3[bones.Length];var rotations=new Quaternion[bones.Length];
        try
        {
            var idle=controller.layers[0].stateMachine.defaultState.motion as AnimationClip;
            var p=AnimationClipPlayable.Create(graph,idle);p.SetSpeed(0);output.SetSourcePlayable(p);p.SetTime(0);graph.Play();graph.Evaluate(.0001f);
            for(int i=0;i<bones.Length;i++){var bone=animator.GetBoneTransform(bones[i]);positions[i]=root.transform.InverseTransformPoint(bone.position);rotations[i]=bone.localRotation;}
            graph.DestroyPlayable(p);
            foreach(var clip in layer.stateMachine.states.Select(s=>s.state.motion).OfType<AnimationClip>().Distinct())
            {
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);output.SetSourcePlayable(playable);playable.SetTime(0);graph.Evaluate(.0001f);
                b.AppendLine(clip.name+" length="+clip.length+" loop="+clip.isLooping+" rootCurves="+clip.hasRootCurves+" bakeXZ="+settings.loopBlendPositionXZ);
                var idleBindings=AnimationUtility.GetCurveBindings(idle);
                var diffs=AnimationUtility.GetCurveBindings(clip).Where(binding=>idleBindings.Any(x=>x.path==binding.path&&x.type==binding.type&&x.propertyName==binding.propertyName)).Select(binding=>new { Name=binding.propertyName, Delta=Mathf.Abs(AnimationUtility.GetEditorCurve(clip,binding).Evaluate(0)-AnimationUtility.GetEditorCurve(idle,binding).Evaluate(0)) }).OrderByDescending(x=>x.Delta).Take(3);
                b.AppendLine(" First-key differences="+string.Join(",",diffs.Select(x=>x.Name+":"+x.Delta)));
                for(int i=0;i<bones.Length;i++){var bone=animator.GetBoneTransform(bones[i]);b.AppendLine(" start/Idle "+bones[i]+" position delta="+(root.transform.InverseTransformPoint(bone.position)-positions[i])+" local angle="+Quaternion.Angle(rotations[i],bone.localRotation));}
                graph.DestroyPlayable(playable);
            }
        }
        finally{graph.Destroy();EditorSceneManager.ClosePreviewScene(scene);}
        return b.ToString();
    }
}
