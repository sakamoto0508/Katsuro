using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class ConfigureEnemyReaction
{
    const string ControllerPath = "Assets/Mock/AnimationController/Enemy.controller";
    const string PrefabPath = "Assets/Mock/Prefabs/Enemy.prefab";
    const string StageKey = "Katsuro.EnemyReaction.PrefabStage";
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static string Stage()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Edit Mode and completed compilation required.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Require(controller != null && controller.layers.Length == 1, "Expected existing one-layer Enemy Controller; inspect before retrying.");
        Require(!controller.parameters.Any(p => p.name.StartsWith("Hit")), "Hit parameters already exist; inspect before retrying.");
        Require(!AssetDatabase.LoadAllAssetsAtPath(ControllerPath).Any(a => a.name == "HitReaction" || a.name == "HitReactionBodyMask"), "Existing reaction subasset; inspect before retrying.");
        var clips = Enumerable.Range(1, 4).Select(i => AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/InportAssets/ARPGPack/ARPGHalberd/Animations/Humanoid/ARPG_Halberd_Hit" + i + ".anim")).ToArray();
        foreach (var c in clips) Require(c != null && c.humanMotion && !c.isLooping && c.events.Length == 0, "Expected non-looping Humanoid Hit clip without attack events.");
        controller.AddParameter("HitReaction", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HitDirection", AnimatorControllerParameterType.Int);
        controller.AddParameter("HitType", AnimatorControllerParameterType.Int);
        controller.AddParameter("HitPlaybackSpeed", AnimatorControllerParameterType.Float);
        var mask = new AvatarMask { name = "HitReactionBodyMask" };
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, i != (int)AvatarMaskBodyPart.Root);
        AssetDatabase.AddObjectToAsset(mask, controller);
        var sm = new AnimatorStateMachine { name = "HitReaction" };
        AssetDatabase.AddObjectToAsset(sm, controller);
        controller.AddLayer(new AnimatorControllerLayer { name = "HitReaction", stateMachine = sm, defaultWeight = 0f, blendingMode = AnimatorLayerBlendingMode.Override, avatarMask = mask });
        var empty = sm.AddState("Empty", new Vector3(300, 0)); empty.writeDefaultValues = true; sm.defaultState = empty;
        string[] directions = { "Front", "Back", "Left", "Right" };
        for (int type = 0; type < 2; type++)
        for (int direction = 0; direction < 4; direction++)
        {
            var state = sm.AddState((type == 0 ? "Light" : "Heavy") + directions[direction], new Vector3(300 + type * 300, 80 + direction * 70));
            state.motion = direction == 0 ? clips[0] : direction == 1 ? clips[type == 0 ? 2 : 3] : clips[1];
            state.mirror = direction == 2;
            state.writeDefaultValues = true;
            state.speed = ((AnimationClip)state.motion).length;
            state.speedParameterActive = true; state.speedParameter = "HitPlaybackSpeed";
            var enter = sm.AddAnyStateTransition(state);
            enter.hasExitTime = false; enter.hasFixedDuration = true; enter.duration = .015f; enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.If, 0, "HitReaction");
            enter.AddCondition(AnimatorConditionMode.Equals, direction, "HitDirection");
            enter.AddCondition(AnimatorConditionMode.Equals, type, "HitType");
            var exit = state.AddTransition(empty);
            exit.hasExitTime = true; exit.exitTime = 1f; exit.hasFixedDuration = true; exit.duration = .04f;
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(sm); EditorUtility.SetDirty(mask); EditorUtility.SetDirty(controller);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var component = root.GetComponent<EnemyAnimationController>(); Require(component != null, "Missing EnemyAnimationController component.");
        var so = new SerializedObject(component);
        so.FindProperty("_reactionLayerName").stringValue = "HitReaction";
        so.FindProperty("_lightHitDuration").floatValue = .24f;
        so.FindProperty("_heavyHitDuration").floatValue = .4f;
        so.FindProperty("_lightHitWeight").floatValue = .6f;
        so.ApplyModifiedPropertiesWithoutUndo();
        SessionState.SetInt(StageKey, root.GetInstanceID());
        return "Staged 4 parameters, full-body mask excluding root, 8 reaction states and routes, existing 4 Hit clips; prefab pending save after Compile.";
    }
    public static string Save()
    {
        Require(!EditorApplication.isPlaying && !EditorApplication.isCompiling, "Compile must complete before Save.");
        var root = EditorUtility.InstanceIDToObject(SessionState.GetInt(StageKey, 0)) as GameObject;
        Require(root != null, "Staged prefab missing; do not overwrite.");
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ControllerPath)) if (obj != null) AssetDatabase.SaveAssetIfDirty(obj);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success); Require(success, "Prefab save failed.");
        PrefabUtility.UnloadPrefabContents(root); SessionState.EraseInt(StageKey);
        return "Saved existing Enemy.controller and Enemy.prefab; original asset paths/GUIDs retained.";
    }
}
