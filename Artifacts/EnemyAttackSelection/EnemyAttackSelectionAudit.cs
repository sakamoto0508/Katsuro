using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Edit Mode専用の読み取り検証。Play Mode、Scene保存、アセット変更は行わない。</summary>
public static class EnemyAttackSelectionAudit
{
    private const string Folder = "Artifacts/EnemyAttackSelection";

    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only");
        Directory.CreateDirectory(Folder);
        var log = new StringBuilder();
        var source = AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>("Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset");
        Check(source != null && source.NearCandidates.Length == 5, "Migrated config: four attacks and StepBack", log);
        var attacks = source.NearCandidates.Where(c => c.Attack != null).ToArray();
        Check(attacks.Length == 4 && attacks.Select(c => c.Attack).Distinct().Count() == 4, "Four distinct attack references", log);
        Check(Math.Abs(attacks.Sum(c => c.Weight) - 50f) < .001f, "Original attack weight total 50", log);
        Check(attacks.Count(c => c.Attack.IsHeavy) == 2, "Two Light and two Heavy definitions", log);
        var config = UnityEngine.Object.Instantiate(source);
        try
        {
            var maker = new EnemyDecisionMaker(new System.Random(811));
            const int draws = 120000;
            var counts = new int[5];
            var last = new EnemyDecision(EnemyActionType.Wait);
            for (int i = 0; i < draws; i++)
            {
                var result = maker.DecideAction(1f, last, config);
                int index = Array.FindIndex(source.NearCandidates, c => c.Action == result.Action && c.Attack == result.Attack);
                Check(index >= 0, "Selected known candidate", null);
                counts[index]++;
            }
            for (int i = 0; i < counts.Length; i++)
            {
                double expected = source.NearCandidates[i].Weight / 60d;
                Check(Math.Abs(counts[i] / (double)draws - expected) < .01, "Weighted frequency candidate " + i + ": " + counts[i], log);
            }
            config.RepeatPenalty = 0f;
            config.RepeatPenaltyPerAttack = false;
            Check(maker.DecideAction(1f, new EnemyDecision(EnemyActionType.Slash, attacks[0].Attack), config).Action == EnemyActionType.StepBack,
                "Category penalty preserves legacy behavior", log);
            config.RepeatPenaltyPerAttack = true;
            bool otherAttack = false;
            for (int i = 0; i < 1000; i++)
            {
                var result = maker.DecideAction(1f, new EnemyDecision(EnemyActionType.Slash, attacks[0].Attack), config);
                Check(result.Attack != attacks[0].Attack, "Repeated attack excluded", null);
                otherAttack |= result.Attack != null;
            }
            Check(otherAttack, "Other attacks remain eligible with per-attack penalty", log);
            foreach (var action in new[] { EnemyActionType.Wait, EnemyActionType.Approach, EnemyActionType.StepBack, EnemyActionType.WaitWalk })
            {
                config.NearCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = action, Weight = 1 } };
                Check(maker.DecideAction(1f, new EnemyDecision(EnemyActionType.WarpAttack), config).Action == action, "Movement/wait candidate " + action, log);
            }
            config.NearCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = float.NaN },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = float.PositiveInfinity },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = -1 } };
            Check(maker.DecideAction(1f, last, config).Action == EnemyActionType.Wait, "Invalid weights fall back to Wait", log);
            config.NearCandidates = null;
            Check(maker.DecideAction(1f, last, config).Action == EnemyActionType.Wait, "Missing candidates fall back to Wait", log);
            config.NearCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = 1 } };
            Check(maker.DecideAction(1f, last, config).Attack == null, "Legacy attack candidate remains supported", log);
            Check(maker.DecideAction(13f, last, config).Action == EnemyActionType.Approach, "Far band preserved", log);
            Check(maker.DecideAction(4f, last, config).Action != EnemyActionType.Slash, "Mid band preserved", log);
        }
        finally { UnityEngine.Object.DestroyImmediate(config); }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
        var serialized = new SerializedObject(prefab.GetComponent<EnemyController>());
        Check(serialized.FindProperty("_decisionConfig").objectReferenceValue == source, "Prefab decision config reference", log);
        var registered = serialized.FindProperty("_attackData");
        Check(registered.arraySize == 4, "Prefab four attack registrations preserved", log);
        for (int i = 0; i < registered.arraySize; i++)
            Check(attacks.Any(a => a.Attack == registered.GetArrayElementAtIndex(i).objectReferenceValue), "Prefab attack reference " + i, log);
        var colliders = serialized.FindProperty("_enemyWeaponColliders");
        for (int i = 0; i < colliders.arraySize; i++)
        {
            var collider = colliders.GetArrayElementAtIndex(i).objectReferenceValue as Collider;
            Check(collider != null && collider.GetComponent<WeaponHitboxRelay>() != null, "Weapon collider and relay " + i, log);
        }
        var animator = serialized.FindProperty("_animator").objectReferenceValue as Animator;
        var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        foreach (var attack in attacks)
            Check(controller.parameters.Any(p => p.name == attack.Attack.AnimatorTrigger && p.type == AnimatorControllerParameterType.Trigger), "Animator Trigger " + attack.Attack.AnimatorTrigger, log);
        Check((int)EnemyActionType.Slash == 3 && (int)EnemyActionType.HeavySlash2 == 6 && (int)EnemyActionType.Wait == 9, "Serialized enum values preserved", log);
        var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject owner = null, target = null;
        EnemyDecisionConfig busyConfig = null;
        try
        {
            owner = new GameObject("AI audit owner");
            target = new GameObject("AI audit target");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, preview);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(target, preview);
            var enemy = owner.AddComponent<EnemyController>();
            target.transform.position = Vector3.forward;
            busyConfig = UnityEngine.Object.Instantiate(source);
            busyConfig.NearCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Attack = attacks[2].Attack, Weight = 1 } };
            busyConfig.RepeatPenalty = 1f;
            var ai = new EnemyAI(enemy, target.transform, null, busyConfig);
            var pending = typeof(EnemyController).GetField("_pendingAction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Check(((EnemyDecision)pending.GetValue(enemy)).Attack == attacks[2].Attack, "AI passes exact selected Heavy data to Controller", log);
            pending.SetValue(enemy, null);
            ai.Tick(30f);
            Check(pending.GetValue(enemy) == null, "Busy attack does not reconsider during Tick", log);
            ai.OnAttackFinished();
            Check(pending.GetValue(enemy) != null, "Attack completion draws next action", log);
            pending.SetValue(enemy, null);
            ai.OnActionFailed();
            ai.Tick(30f);
            Check(pending.GetValue(enemy) != null, "Failed action recovers and draws again", log);
        }
        finally
        {
            if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (busyConfig != null) UnityEngine.Object.DestroyImmediate(busyConfig);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        }
        var invalidAttacker = new EnemyAttacker(null, null, null, null, null);
        Check(!invalidAttacker.PerformAttack((EnemyAttackData)null), "Missing attack/Animator/weapon fails safely", log);
        invalidAttacker.Dispose();
        File.WriteAllText(Folder + "/EditModeValidation.txt", log.ToString());

        var clips = new StringBuilder("path\tclip\tseconds\thumanoid\thasRootCurves\taverageSpeed\tXZ baked\tkeepOriginalXZ\n");
        var curves = new StringBuilder("path\tclip\tcurvePath\tproperty\tfirst\tlast\tmin\tmax\n");
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            bool arpg = path.StartsWith("Assets/InportAssets/ARPGPack/ARPGSamurai/") && path.EndsWith(".anim");
            bool ual = path.Contains("Universal Animation Library") && path.EndsWith(".fbx") && path.Contains("UAL2_Standard");
            bool extra = path.StartsWith("Assets/InportAssets/Anim/") && path.EndsWith(".anim") ||
                path.StartsWith("Assets/InportAssets/Kevin Iglesias/Human Animations/Animations/Male/Combat/") && path.EndsWith(".fbx") && (path.Contains("/2H/") || path.Contains("CombatDamage"));
            if (!arpg && !ual && !extra) continue;
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
            {
                if (clip.name.StartsWith("__preview__")) continue;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                clips.AppendLine($"{path}\t{clip.name}\t{clip.length}\t{clip.humanMotion}\t{clip.hasRootCurves}\t{clip.averageSpeed}\t{settings.loopBlendPositionXZ}\t{settings.keepOriginalPositionXZ}");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!binding.propertyName.StartsWith("RootT.") && !(binding.path.ToLowerInvariant().EndsWith("root") && binding.propertyName.Contains("Position"))) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0) continue;
                    var keys = curve.keys;
                    curves.AppendLine($"{path}\t{clip.name}\t{binding.path}\t{binding.propertyName}\t{keys[0].value}\t{keys[keys.Length - 1].value}\t{keys.Min(k => k.value)}\t{keys.Max(k => k.value)}");
                }
            }
        }
        File.WriteAllText(Folder + "/UnityClips.tsv", clips.ToString());
        File.WriteAllText(Folder + "/UnityRootCurves.tsv", curves.ToString());
        Debug.Log("Enemy attack selection audit passed (Edit Mode, no gameplay).");
    }

    private static void Check(bool valid, string message, StringBuilder log)
    {
        if (!valid) throw new InvalidOperationException(message);
        log?.AppendLine("PASS " + message);
    }
}
