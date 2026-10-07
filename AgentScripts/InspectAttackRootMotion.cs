using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectAttackRootMotion
{
    public static IEnumerable<AnimatorState> States(AnimatorStateMachine sm)
    {foreach(var s in sm.states)yield return s.state;foreach(var child in sm.stateMachines)foreach(var s in States(child.stateMachine))yield return s;}
    public static string Inspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var output=new StringBuilder();
        foreach(var name in new[]{"Player","Enemy"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+name+".prefab");
            var a=root.GetComponent<Animator>();var rb=root.GetComponent<Rigidbody>();var agent=root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            output.AppendLine(name+": animator="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController)+" rootMotion="+a.applyRootMotion+" update="+a.updateMode+" scale="+root.transform.localScale+" RB kinematic="+rb.isKinematic+" constraints="+rb.constraints+" collision="+rb.collisionDetectionMode+" interpolate="+rb.interpolation+" agent="+(agent==null?"none":("enabled="+agent.enabled+" updatePosition="+agent.updatePosition+" updateRotation="+agent.updateRotation)));
            var ac=a.runtimeAnimatorController as AnimatorController;
            foreach(var layer in ac.layers)
                foreach(var s in States(layer.stateMachine))output.AppendLine("STATE "+layer.name+"/"+s.name+" tag="+s.tag+" speed="+s.speed+" motion="+AssetDatabase.GetAssetPath(s.motion));
            foreach(var c in ac.animationClips.Distinct().Where(c=>c.name.IndexOf("Attack",StringComparison.OrdinalIgnoreCase)>=0||c.name.IndexOf("Slash",StringComparison.OrdinalIgnoreCase)>=0||c.name.IndexOf("Thrust",StringComparison.OrdinalIgnoreCase)>=0))
            {
                var path=AssetDatabase.GetAssetPath(c); var settings=AnimationUtility.GetAnimationClipSettings(c);
                var importer=AssetImporter.GetAtPath(path) as ModelImporter;
                output.AppendLine("CLIP "+path+" length="+c.length+" rootCurves="+c.hasRootCurves+" settings="+JsonUtility.ToJson(settings)+" rootNode="+(importer==null?"standalone .anim":importer.motionNodeName));
                foreach(var binding in AnimationUtility.GetCurveBindings(c).Where(b=>b.propertyName.StartsWith("Root")||b.propertyName.StartsWith("Motion")||b.propertyName.Contains("Position")))
                {
                    var curve=AnimationUtility.GetEditorCurve(c,binding);float min=curve.keys.Min(k=>k.value),max=curve.keys.Max(k=>k.value);
                    output.AppendLine(" CURVE "+binding.path+"/"+binding.propertyName+" start="+curve.Evaluate(0)+" end="+curve.Evaluate(c.length)+" min="+min+" max="+max);
                }
            }
            if(name=="Enemy")
            {
                var data=new SerializedObject(root.GetComponent<EnemyController>()).FindProperty("_attackData");
                for(int i=0;i<data.arraySize;i++){var d=data.GetArrayElementAtIndex(i).objectReferenceValue as EnemyAttackData;if(d!=null)output.AppendLine("DATA "+d.name+" action="+d.ActionType+" trigger="+d.AnimatorTrigger+" range="+d.Range);}
            }
        }
        System.IO.File.WriteAllText("AgentScripts/AttackRootMotionBefore.txt",output.ToString());
        return output.ToString();
    }
}
