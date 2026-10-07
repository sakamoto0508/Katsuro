using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MeasureAttackRootMotion
{
    public static string Measure()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode only");
        var report=new StringBuilder("# Attack Root Motion: Edit Mode avatar evaluation\n\nDistances are isolated clip extraction, not runtime/gameplay travel.\n");
        foreach(string name in new[]{"Player","Enemy"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+name+".prefab");
            var clips=prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips.Distinct().Where(c=>AssetDatabase.GetAssetPath(c).Contains("Attack"));
            foreach(var clip in clips)
            {
                var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                var graph=PlayableGraph.Create("Root motion inspection");
                try
                {
                    foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;
                    foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
                    var agent=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
                    var animator=root.GetComponent<Animator>();animator.fireEvents=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);p.SetApplyPlayableIK(false);
                    var output=AnimationPlayableOutput.Create(graph,"measure",animator);output.SetSourcePlayable(p);graph.Play();graph.Evaluate(0);
                    Vector3 total=Vector3.zero;float distance=0,maxDelta=0;
                    int frames=Mathf.CeilToInt(clip.length*120);float dt=clip.length/frames;
                    for(int i=0;i<frames;i++){graph.Evaluate(dt);var delta=animator.deltaPosition;delta.y=0;total+=delta;distance+=delta.magnitude;maxDelta=Mathf.Max(maxDelta,delta.magnitude);}
                    report.AppendLine(name+" | "+AssetDatabase.GetAssetPath(clip)+" | applyRootMotion=false scripted extraction | netXZ="+total.magnitude.ToString("F3")+"m pathXZ="+distance.ToString("F3")+"m maxDelta="+maxDelta.ToString("F4")+"m | "+total.ToString("F3"));
                }
                finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
            }
        }
        System.IO.File.WriteAllText("AgentScripts/AttackRootMotionMeasurements.md",report.ToString());return report.ToString();
    }
}
