using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Text;
public static class SampleEnemyHits
{
    public static string Main()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"),scene);
        var a=root.GetComponentInChildren<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.applyRootMotion=false;
        var graph=PlayableGraph.Create("Read existing Hit poses");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var output=AnimationPlayableOutput.Create(graph,"Pose",a);
        var b=new StringBuilder();
        try
        {
            foreach(var guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/InportAssets/ARPGPack/ARPGHalberd/Animations/Humanoid"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                var name=System.IO.Path.GetFileNameWithoutExtension(path);
                if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"_Hit[1-5]$"))continue;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);output.SetSourcePlayable(playable);
                playable.SetTime(0);graph.Evaluate(0);
                var hips=a.GetBoneTransform(HumanBodyBones.Hips);var chest=a.GetBoneTransform(HumanBodyBones.Chest);var head=a.GetBoneTransform(HumanBodyBones.Head);
                var initial=root.transform.InverseTransformDirection(head.position-hips.position);
                b.AppendLine(path+" length="+clip.length+" human="+clip.humanMotion+" loop="+clip.isLooping);
                foreach(float n in new[]{.15f,.3f,.45f,.65f,.85f})
                {
                    playable.SetTime(n*clip.length);graph.Evaluate(0);
                    var delta=root.transform.InverseTransformDirection(head.position-hips.position)-initial;
                    b.AppendLine(" n="+n+" head-relative-hips delta="+delta+" chestEuler="+chest.localEulerAngles+" hips="+root.transform.InverseTransformPoint(hips.position));
                }
                graph.DestroyPlayable(playable);
            }
        }
        finally{graph.Destroy();EditorSceneManager.ClosePreviewScene(scene);}
        return b.ToString();
    }
}
