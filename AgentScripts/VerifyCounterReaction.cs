using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class VerifyCounterReaction
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static AnimatorState[] AllStates(AnimatorStateMachine sm)=>sm.states.Select(x=>x.state).Concat(sm.stateMachines.SelectMany(x=>AllStates(x.stateMachine))).ToArray();
    public static string Main()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling,"Edit Mode required.");
        var b=new StringBuilder();
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        Check(enemy.GetComponent<EnemyController>()!=null && enemy.GetComponent<EnemyAnimationController>()!=null && player.GetComponent<PlayerController>()!=null && player.GetComponent<PlayerAnimationController>()!=null,"Saved script references");
        var ec=enemy.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        var pc=player.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        var counter=ec.parameters.Single(p=>p.name=="IsJustAvoidCounter");
        Check(counter.type==AnimatorControllerParameterType.Bool && !counter.defaultBool,"Counter authorization defaults false.");
        var reaction=ec.layers.Single(l=>l.name=="HitReaction");
        Check(reaction.defaultWeight==0 && reaction.stateMachine.states.Length==9,"Existing reaction states preserved.");
        foreach(var transition in reaction.stateMachine.anyStateTransitions)
            Check(transition.conditions.Any(c=>c.parameter=="IsJustAvoidCounter" && c.mode==AnimatorConditionMode.If),"Every full-body reaction requires counter flag.");
        var tagged=AllStates(pc.layers[0].stateMachine).Where(s=>s.tag==PlayerAttacker.JustAvoidCounterTag).ToArray();
        Check(tagged.Length==2 && tagged.All(s=>AssetDatabase.GetAssetPath(s.motion).StartsWith("Assets/Mock/Animation/Player/JustVoidAttack/")),"Only existing dedicated counter attacks are tagged.");
        foreach(var s in tagged)
        {
            var clip=s.motion as AnimationClip;
            Check(clip!=null && clip.events.Any(e=>e.functionName=="AnimEvent_EnableWeaponHitbox"),"Counter attack has a hitbox event.");
            b.AppendLine("Counter tag: Base Layer."+s.name+" -> "+AssetDatabase.GetAssetPath(clip));
        }
        var start=pc.layers[0].stateMachine.states.Single(s=>s.state.name=="ARPG_Samurai_Attack_Heavy1_Start").state;
        Check(pc.layers[0].stateMachine.anyStateTransitions.Any(t=>t.destinationState==start && t.conditions.Any(c=>c.parameter=="JustAvoidWindow")),"Original Just Avoid success route unchanged.");
        Check(start.transitions.Any(t=>t.destinationState==tagged.Single(s=>s.name=="ARPG_Samurai_Attack_Heavy1") && t.conditions.Any(c=>c.parameter=="LightAttack")),"Original counter input route unchanged.");
        var feedback=new SerializedObject(enemy.GetComponent<CombatFeedback>());
        Check(Mathf.Approximately(feedback.FindProperty("_justAvoidCounterHitStop").floatValue,.1f),"Saved counter HitStop=.1");
        Check(Mathf.Approximately(feedback.FindProperty("_lightReactionAngle").floatValue,5) && Mathf.Approximately(feedback.FindProperty("_heavyReactionAngle").floatValue,10),"Original small Bone reaction preserved.");
        foreach(bool heavy in new[]{false,true})
        {
            var normal=new DamageInfo(1,Vector3.zero,Vector3.forward,null,null,heavy);
            var special=new DamageInfo(1,Vector3.zero,Vector3.forward,null,null,heavy,isJustAvoidCounter:true);
            Check(!normal.IsJustAvoidCounter && special.IsJustAvoidCounter && normal.IsHeavy==special.IsHeavy && normal.DamageAmount==special.DamageAmount,"Counter flag independent from Heavy and Damage.");
        }
        // Check ordinary accepted damage routing on a temporary prefab in a preview
        // scene. No Init, AI, animations, VFX, input, or Play Mode are run.
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(enemy,preview);
            var controller=root.GetComponent<EnemyController>();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var healthField=typeof(EnemyController).GetField("_health",flags);
            var pending=typeof(EnemyController).GetField("_pendingAction",flags);
            var interrupted=typeof(EnemyController).GetField("_reactionInterruptedAttack",flags);
            healthField.SetValue(controller,new EnemyHealth(null));
            foreach(bool heavy in new[]{false,true})
            {
                pending.SetValue(controller,(EnemyActionType?)EnemyActionType.Slash);
                controller.ApplyDamage(new DamageInfo(.01f,Vector3.zero,Vector3.forward,null,null,heavy));
                Check((EnemyActionType?)pending.GetValue(controller)==EnemyActionType.Slash && !(bool)interrupted.GetValue(controller),"Normal damage must not cancel pending attacks: Heavy="+heavy);
                Check(!root.GetComponent<EnemyAnimationController>().TryPlayHitReaction(new DamageInfo(1,Vector3.zero,Vector3.forward,null,null,heavy)),"Normal reaction requests rejected: Heavy="+heavy);
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        b.AppendLine("PASS: Normal Light/Heavy accepted-damage routing preserves pending attack and rejects full-body reaction; counter flag is independent of Heavy. All 8 Animator entries require counter; 2 existing Player counter states tagged; saved counter HitStop .1 and Bone 5/10 preserved. Play Mode not entered.");
        return b.ToString();
    }
}
