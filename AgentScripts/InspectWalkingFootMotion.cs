using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class InspectWalkingFootMotion
{
    public static string Inspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var controller=prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        var tree=controller.layers[0].stateMachine.states.Select(s=>s.state.motion).OfType<BlendTree>().Single();
        var report=new StringBuilder("Edit Mode foot pose analysis: stance-foot travel is opposite locomotion. No runtime visual check.\n");
        foreach(var child in tree.children)
        {
            var clip=child.motion as AnimationClip;var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var graph=PlayableGraph.Create("Walking foot motion inspection");
            try
            {
                foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))c.enabled=false;
                root.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
                var animator=root.GetComponent<Animator>();animator.fireEvents=false;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                var output=AnimationPlayableOutput.Create(graph,"feet",animator);output.SetSourcePlayable(playable);graph.Play();graph.Evaluate(0f);
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if(left==null||right==null)throw new Exception("Avatar foot bones missing");
                int frames=Mathf.CeilToInt(clip.length*120);var feet=new[]{new Vector3[frames+1],new Vector3[frames+1]};
                for(int i=0;i<=frames;i++)
                {
                    if(i>0)graph.Evaluate(clip.length/frames);
                    feet[0][i]=root.transform.InverseTransformPoint(left.position);feet[1][i]=root.transform.InverseTransformPoint(right.position);
                }
                Vector2 stance=Vector2.zero;int samples=0;
                foreach(var foot in feet)
                {
                    float low=foot.Min(p=>p.y);
                    for(int i=1;i<=frames;i++)if(foot[i-1].y<=low+.04f&&foot[i].y<=low+.04f)
                    {var d=foot[i]-foot[i-1];stance-=new Vector2(d.x,d.z);samples++;}
                }
                float alignment=Vector2.Dot(stance.normalized,child.position.normalized);
                report.AppendLine(clip.name+" tree="+child.position+" inferred direction="+stance.normalized+" alignment="+alignment.ToString("F3")+" contact samples="+samples);
            }
            finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        System.IO.File.WriteAllText("AgentScripts/EnemyWalkingFootMotion.txt",report.ToString());return report.ToString();
    }
}
