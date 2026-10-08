using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectEnemyWalking
{
    static void Tree(BlendTree tree,StringBuilder s)
    {
        s.AppendLine("TREE "+tree.name+" type="+tree.blendType+" parameters="+tree.blendParameter+"/"+tree.blendParameterY);
        foreach(var c in tree.children)
        {
            s.AppendLine(" CHILD "+c.position+" threshold="+c.threshold+" mirror="+c.mirror+" clip="+AssetDatabase.GetAssetPath(c.motion));
            if(c.motion is BlendTree sub)Tree(sub,s);
            if(c.motion is AnimationClip clip)
            {
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                s.AppendLine("  clip mirror="+settings.mirror+" XZbake="+settings.loopBlendPositionXZ+" Ybake="+settings.loopBlendPositionY+" RotBake="+settings.loopBlendOrientation);
                var bindings=AnimationUtility.GetCurveBindings(clip);
                foreach(var b in bindings.Where(b=>b.propertyName=="RootT.x"||b.propertyName=="RootT.z"))
                {var curve=AnimationUtility.GetEditorCurve(clip,b);s.AppendLine("  "+b.propertyName+" start="+curve.Evaluate(0)+" end="+curve.Evaluate(clip.length));}
            }
        }
    }
    public static string Inspect()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var animator=root.GetComponent<Animator>();var agent=root.GetComponent<UnityEngine.AI.NavMeshAgent>();var controller=root.GetComponent<EnemyController>();
        var status=new SerializedObject(controller).FindProperty("_enemyStuts").objectReferenceValue as EnemyStuts;
        var s=new StringBuilder();s.AppendLine("Prefab Enemy; animator="+AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)+" auto rootMotion="+animator.applyRootMotion+" agent enabled="+agent.enabled+" updatePosition="+agent.updatePosition+" updateRotation="+agent.updateRotation+" rootMotionScale="+new SerializedObject(controller).FindProperty("_attackRootMotionScale").floatValue);
        s.AppendLine("Status "+AssetDatabase.GetAssetPath(status)+" RotationMode="+status.RotationMode+" TurnSpeed="+status.TurnSpeed+" ChaseStart="+status.ChaseStartDistance+" Stop="+status.StopDistance);
        var ac=animator.runtimeAnimatorController as AnimatorController;
        foreach(var layer in ac.layers)foreach(var state in States(layer.stateMachine))if(state.motion is BlendTree tree)Tree(tree,s);
        System.IO.File.WriteAllText("AgentScripts/EnemyWalkingSettings.txt",s.ToString());return s.ToString();
    }
    public static string SaveAndInspect()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Compiled Edit Mode required");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        AssetDatabase.SaveAssetIfDirty(root);
        AssetDatabase.SaveAssetIfDirty(root.GetComponent<Animator>().runtimeAnimatorController);
        return "Scripts saved/imported; existing Enemy Prefab/Animator settings require no changes. SaveAssetIfDirty completed.\n"+Inspect();
    }
    static System.Collections.Generic.IEnumerable<AnimatorState> States(AnimatorStateMachine sm)
    {foreach(var state in sm.states)yield return state.state;foreach(var child in sm.stateMachines)foreach(var state in States(child.stateMachine))yield return state;}
}
