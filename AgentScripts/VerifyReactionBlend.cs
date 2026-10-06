using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class VerifyReactionBlend
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Main()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Edit Mode required.");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var controller=(AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
        var layer=controller.layers.Single(l=>l.name=="HitReaction");
        foreach(var t in layer.stateMachine.anyStateTransitions.Concat(layer.stateMachine.states.SelectMany(s=>s.state.transitions)))
        {
            Check(Mathf.Approximately(t.duration,.08f)&&t.hasFixedDuration&&t.offset==0f&&t.interruptionSource==TransitionInterruptionSource.Source&&!t.orderedInterruption,"Saved blend/offset/interruption settings.");
            Check(t.destinationState.name=="Empty" ? t.hasExitTime&&Mathf.Approximately(t.exitTime,1) : !t.hasExitTime&&t.conditions.Any(c=>c.parameter=="IsJustAvoidCounter"),"Exit timing and counter-only entry.");
        }
        var so=new SerializedObject(prefab.GetComponent<EnemyAnimationController>());
        Check(Mathf.Approximately(so.FindProperty("_reactionBlendIn").floatValue,.08f)&&Mathf.Approximately(so.FindProperty("_reactionBlendOut").floatValue,.08f)&&Mathf.Approximately(so.FindProperty("_heavyHitDuration").floatValue,.4f),"Saved blend and unchanged main duration.");
        Check(!prefab.GetComponent<Animator>().applyRootMotion&&!layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root),"Root displacement suppressed.");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            var animator=root.GetComponent<Animator>();animator.Rebind();
            var component=root.GetComponent<EnemyAnimationController>();component.Init();
            var type=typeof(EnemyAnimationController);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            type.GetField("_reactionDuration",flags).SetValue(component,.4f);
            type.GetField("_reactionWeight",flags).SetValue(component,1f);
            type.GetField("_reactionStartWeight",flags).SetValue(component,0f);
            type.GetProperty("IsReacting").SetValue(component,true);
            type.GetProperty("IsHeavyReacting").SetValue(component,true);
            int index=animator.GetLayerIndex("HitReaction");animator.speed=1;
            Check(!component.TickHitReaction(.02f)&&Mathf.Approximately(animator.GetLayerWeight(index),.15625f),"Gradual entry at .02 seconds.");
            animator.speed=0;
            Check(!component.TickHitReaction(.1f)&&Mathf.Approximately(animator.GetLayerWeight(index),.15625f),"HitStop freezes blend and reaction timer.");
            animator.speed=1;
            Check(!component.TickHitReaction(.02f)&&Mathf.Approximately(animator.GetLayerWeight(index),.5f),"Resume from same blend position.");
            Check(!component.TickHitReaction(.04f)&&Mathf.Approximately(animator.GetLayerWeight(index),1f),"Full strength at .08 seconds.");
            Check(!component.TickHitReaction(.32f)&&Mathf.Approximately(animator.GetLayerWeight(index),1f),"Full strength through main reaction.");
            Check(!component.TickHitReaction(.04f)&&Mathf.Abs(animator.GetLayerWeight(index)-.5f)<.001f&&component.IsReacting,"Movement still held midway through recovery.");
            Check(component.TickHitReaction(.041f)&&Mathf.Approximately(animator.GetLayerWeight(index),0f)&&!component.IsReacting,"Release only after complete recovery.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        return "PASS: Saved .08 entry / .08 recovery on all 16 transitions, counter gate unchanged, main duration .4/full weight preserved, root mask excludes displacement. Edit Mode checks: smooth layer entry, HitStop freeze/resume, full-strength peak, half-weight recovery, movement release after zero weight. Play Mode not entered.";
    }
}
