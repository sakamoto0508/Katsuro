using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class VerifyEnemyReaction
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static string Main()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Must remain in Edit Mode.");
        var b = new StringBuilder();
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var animator = root.GetComponent<Animator>();
        Check(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman, "Valid Humanoid Avatar required.");
        var c = animator.runtimeAnimatorController as AnimatorController;
        Check(c != null && c.layers.Length == 2 && c.layers[0].name == "Base Layer", "Preserved base layer and one reaction layer required.");
        var layer = c.layers.Single(l => l.name == "HitReaction");
        Check(layer.defaultWeight == 0f && layer.avatarMask != null && !layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root), "No idle override or reaction root displacement.");
        foreach (var pair in new[] { ("HitReaction", AnimatorControllerParameterType.Trigger), ("HitDirection", AnimatorControllerParameterType.Int), ("HitType", AnimatorControllerParameterType.Int), ("HitPlaybackSpeed", AnimatorControllerParameterType.Float) })
            Check(c.parameters.Count(p => p.name == pair.Item1 && p.type == pair.Item2) == 1, "Parameter " + pair.Item1);
        var sm = layer.stateMachine;
        Check(sm.states.Length == 9 && sm.defaultState.name == "Empty" && sm.anyStateTransitions.Length == 8, "8 reactions and Empty with 8 entry routes.");
        string[] directions = { "Front", "Back", "Left", "Right" };
        for (int type = 0; type < 2; type++)
        for (int direction = 0; direction < 4; direction++)
        {
            string name = (type == 0 ? "Light" : "Heavy") + directions[direction];
            var state = sm.states.Single(s => s.state.name == name).state;
            var clip = state.motion as AnimationClip;
            Check(clip != null && clip.humanMotion && !clip.isLooping && clip.events.Length == 0, name + " clip");
            Check(state.mirror == (direction == 2), name + " mirroring");
            Check(state.speedParameterActive && state.speedParameter == "HitPlaybackSpeed" && Mathf.Approximately(state.speed, clip.length), name + " timing");
            var enter = sm.anyStateTransitions.Single(t => t.destinationState == state);
            Check(!enter.hasExitTime && enter.conditions.Length == 4 && enter.conditions.Any(x => x.parameter == "IsJustAvoidCounter" && x.mode == AnimatorConditionMode.If) && enter.conditions.Any(x => x.parameter == "HitReaction" && x.mode == AnimatorConditionMode.If) && enter.conditions.Any(x => x.parameter == "HitDirection" && x.threshold == direction) && enter.conditions.Any(x => x.parameter == "HitType" && x.threshold == type), name + " entry conditions");
            Check(state.transitions.Length == 1 && state.transitions[0].destinationState == sm.defaultState && state.transitions[0].hasExitTime && Mathf.Approximately(state.transitions[0].exitTime, 1f), name + " exit");
            b.AppendLine(name + " -> " + clip.name + " mirrored=" + state.mirror + " duration=" + (type == 0 ? .24f : .4f) + " + .04 fade");
        }
        var component = root.GetComponent<EnemyAnimationController>(); Check(component != null && root.GetComponent<EnemyController>() != null && root.GetComponent<CombatFeedback>() != null, "Enemy script references");
        var so = new SerializedObject(component);
        Check(so.FindProperty("_reactionLayerName").stringValue == "HitReaction" && Mathf.Approximately(so.FindProperty("_lightHitDuration").floatValue, .24f) && Mathf.Approximately(so.FindProperty("_heavyHitDuration").floatValue, .4f) && Mathf.Approximately(so.FindProperty("_lightHitWeight").floatValue, .6f), "Saved prefab fields");
        Check(c.layers[0].stateMachine.anyStateTransitions.Any(t => t.conditions.Any(p => p.parameter == "EnemyDead")), "Existing Death route preserved");
        var enemy = new GameObject("Direction-only check"); var attacker = new GameObject("Source-only check");
        try
        {
            Vector3[] local = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (float angle in new[] { 0f, 90f, 213f })
            {
                enemy.transform.rotation = Quaternion.Euler(0, angle, 0);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 world = enemy.transform.TransformDirection(local[i]); attacker.transform.position = enemy.transform.position + world * 2;
                    Check(EnemyAnimationController.GetHitDirection(enemy.transform, new DamageInfo(1, Vector3.zero, Vector3.zero, attacker, null)) == i, "Instigator direction " + angle + ":" + i);
                    Check(EnemyAnimationController.GetHitDirection(enemy.transform, new DamageInfo(1, Vector3.zero, -world, null, null)) == i, "HitNormal fallback " + angle + ":" + i);
                    Check(EnemyAnimationController.GetHitDirection(enemy.transform, new DamageInfo(1, world, Vector3.zero, null, null)) == i, "HitPoint fallback " + angle + ":" + i);
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(enemy); UnityEngine.Object.DestroyImmediate(attacker); }
        b.AppendLine("PASS: 36 local direction checks, serialized fields, 4 parameters, 8 clips/routes/exits, root mask, Humanoid rig, Death route. Play Mode was not entered.");
        b.AppendLine("Enemy prefab GUID=" + AssetDatabase.AssetPathToGUID("Assets/Mock/Prefabs/Enemy.prefab") + "; controller GUID=" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c)));
        return b.ToString();
    }
}
