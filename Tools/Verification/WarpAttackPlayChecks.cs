// Run with GameScene in Play Mode. Uses temporary SO clones; stop Play Mode afterwards.
// unity command run_script --caller plugin --skill unity-cli --format json -- --file Tools/Verification/WarpAttackPlayChecks.cs --entry WarpAttackPlayChecks.Run --timeout_ms 25000
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class WarpAttackPlayChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Get(object value, string name) => value.GetType().GetField(name, Fields).GetValue(value);
    static void Set(object value, string name, object field) => value.GetType().GetField(name, Fields).SetValue(value, field);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Fields).Invoke(value, args);
    static object V(Vector3 v) => new { v.x, v.y, v.z };

    public static Task<string> Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("GameScene Play Mode required");
        var done = new TaskCompletionSource<string>();
        var enemy = Object.FindFirstObjectByType<EnemyController>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        var animator = enemy.GetComponent<Animator>();
        var agent = enemy.GetComponent<NavMeshAgent>();
        var mover = (EnemyMover)Get(enemy, "_mover");
        var originalConfig = (EnemyDecisionConfig)Get(enemy, "_decisionConfig");
        var originalAI = Get(enemy, "_ai");
        var config = Object.Instantiate(originalConfig);
        var pattern = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(
            "Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/WarpAttack.asset"));
        var passed = new List<string>();
        var samples = new List<object>();
        var inputs = Object.FindObjectsByType<InputBuffer>(FindObjectsSortMode.None);
        var inputEnabled = Array.ConvertAll(inputs, x => x.enabled);
        bool vanish = false, hidden = false, appear = false, slash = false, hitbox = false, moved = false;
        float maxRootTravel = 0f, started = Time.unscaledTime, phaseStarted = started;
        int firstFrame = Time.frameCount, previousFrame = -1, phase = 0;
        Vector3 origin = enemy.transform.position;
        EditorApplication.CallbackFunction tick = null;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            passed.Add(name);
        }
        void Finish(Exception error = null)
        {
            EditorApplication.update -= tick;
            try
            {
                enemy.CancelAttackPattern(false);
                mover.ReleaseMovementAfterAttack();
                Set(enemy, "_decisionConfig", originalConfig); Set(enemy, "_ai", originalAI);
                for (int i = 0; i < inputs.Length; i++) if (inputs[i] != null) inputs[i].enabled = inputEnabled[i];
                ((IDictionary)Get(enemy, "_patternCooldowns")).Remove(pattern);
                Object.Destroy(config); Object.Destroy(pattern);
                Directory.CreateDirectory("Tools/Verification/Results");
                File.WriteAllText("Tools/Verification/Results/WarpAttackDistance.PlayMode.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { success = error == null, error = error?.ToString(),
                        passed, frames = Time.frameCount - firstFrame, vanish, hidden, appear, slash, hitbox, moved,
                        maxRootTravel, samples }, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception cleanup) { error = error ?? cleanup; }
            if (error != null) done.TrySetException(error);
            else done.TrySetResult("PASS " + passed.Count + " GameScene Play Mode checks");
        }
        try
        {
            Check(!enemy.IsTraining && GameManager.Instance.IsCombatActive, "real GameScene combat is active");
            Set(enemy, "_ai", null);
            foreach (var input in inputs) input.enabled = false;
            enemy.CancelAttackPattern(false); mover.ReleaseMovementAfterAttack();
            var animation = enemy.GetComponent<EnemyAnimationController>();
            animation.CancelHitReaction(); animation.InterruptAttackForHitReaction();
            mover.ReleaseMovementAfterReaction();
            Set(enemy, "_pendingAction", null);
            animator.Play("Base Layer.Idle", 0, 0); animator.Update(0f);
            pattern.MinAttackDistance = 25f; pattern.MaxAttackDistance = 26f;
            pattern.TeleportDistance = 2.5f;
            config.NearDistance = 3f; config.FarDistance = 12f;
            var candidate = new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.WarpAttack, Pattern = pattern, Weight = 20f };
            config.MidCandidates = new[] { candidate };
            Set(enemy, "_decisionConfig", config);
            bool found = false;
            var rb = player.GetComponent<Rigidbody>();
            for (int i = 0; i < 12 && !found; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(i * 30f, Vector3.up) * Vector3.forward;
                if (!NavMesh.SamplePosition(origin + direction * 6f, out var floor, 1.5f, NavMesh.AllAreas)) continue;
                rb.linearVelocity = rb.angularVelocity = Vector3.zero;
                rb.position = floor.position + Vector3.up * .1f;
                player.transform.SetPositionAndRotation(rb.position, Quaternion.LookRotation(-direction));
                Physics.SyncTransforms();
                found = EnemyWarpDestination.TryFind(player.transform, enemy.GetComponent<CapsuleCollider>(), agent, pattern, out _);
            }
            Check(found, "safe NavMesh Warp destination found using real capsules");
            mover.HoldMovementForAttack();
            float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
            var decision = new EnemyDecisionMaker(new System.Random(9)).DecideAction(distance,
                new EnemyDecision(EnemyActionType.Wait), config, enemy.CanSelectAttackCandidate);
            Check(decision.Pattern == pattern, "Mid Warp selected through real Controller eligibility despite 25-26m Min/Max");
            Check((bool)Call(enemy, "BeginAttackPattern", pattern), "selected Mid Warp actually starts");
            Check(!enemy.CanSelectAttackCandidate(candidate), "same Warp is ineligible during Cooldown");
            tick = () =>
            {
                try
                {
                    if (!EditorApplication.isPlaying) throw new Exception("Play Mode stopped during test");
                    if (Time.frameCount == previousFrame) return;
                    previousFrame = Time.frameCount;
                    if (Time.unscaledTime - started > 18f) throw new TimeoutException("Warp Play Mode verification timeout");
                    bool isHidden = (bool)Get(enemy, "_warpHidden");
                    bool preparing = Get(enemy, "_preparingWarp") != null;
                    var vanishVFX = Get(enemy, "_warpVanish") as KatsuroTimedVFX;
                    var appearVFX = Get(enemy, "_warpAppear") as KatsuroTimedVFX;
                    var slashVFX = Get(enemy, "_warpSlash") as KatsuroTimedVFX;
                    bool activeHitbox = (bool)Get(Get(enemy, "_attacker"), "_isHitboxActive");
                    if (phase == 0)
                    {
                        vanish |= vanishVFX != null && vanishVFX.IsPlaying;
                        hidden |= isHidden; appear |= appearVFX != null && appearVFX.IsPlaying;
                        slash |= slashVFX != null && slashVFX.IsPlaying; hitbox |= activeHitbox;
                        moved |= Vector3.Distance(origin, enemy.transform.position) > 1f;
                        maxRootTravel = Mathf.Max(maxRootTravel, (float)Get(mover, "_attackTravel"));
                    }
                    samples.Add(new { phase, frame = Time.frameCount, time = Time.unscaledTime - started,
                        position = V(enemy.transform.position), preparing, hidden = isHidden, active = enemy.IsAttackPatternActive,
                        hitbox = activeHitbox, agent = agent.enabled, root = (float)Get(mover, "_attackTravel"),
                        clip = Array.ConvertAll(animator.GetCurrentAnimatorClipInfo(0), x => x.clip.name) });
                    if (phase == 0 && !enemy.IsAttackPatternActive)
                    {
                        Check(Time.frameCount > firstFrame + 5, "live game frames advanced");
                        Check(vanish && hidden && appear && moved, "vanish / hidden teleport / appearance completed");
                        Check(slash && hitbox, "existing attack Animation Event opened hitbox and Warp Issen");
                        Check(maxRootTravel <= pattern.MaximumTravelDistance + .01f, "post-Warp Root Motion respects safety cap");
                        Check(agent.enabled && enemy.GetComponentInChildren<SkinnedMeshRenderer>().enabled,
                            "normal movement and body visibility restored after attack");
                        // The first Warp ends in Near. Reset only the test fixture to Mid before another selection.
                        Check(agent.Warp(origin), "test actor reset to its original NavMesh position");
                        enemy.GetComponent<Rigidbody>().position = origin;
                        enemy.transform.position = origin;
                        Physics.SyncTransforms();
                        pattern.TeleportDistance = .1f;
                        // Remove only this temporary pattern's cooldown to isolate destination validation.
                        ((IDictionary)Get(enemy, "_patternCooldowns")).Remove(pattern);
                        Check(!enemy.CanSelectAttackCandidate(candidate), "overlapping destination remains ineligible");
                        Check(!(bool)Call(enemy, "BeginAttackPattern", pattern), "invalid destination is rejected at actual start");
                        pattern.TeleportDistance = 2.5f;
                        mover.HoldMovementForAttack();
                        Check((bool)Call(enemy, "BeginAttackPattern", pattern), "second valid Warp starts for interruption check");
                        phase = 1; phaseStarted = Time.unscaledTime;
                    }
                    else if (phase == 1 && isHidden)
                    {
                        pattern.TeleportDistance = 10000f; // invalidate the upcoming destination during disappearance
                        phase = 2;
                    }
                    else if (phase == 2 && !enemy.IsAttackPatternActive)
                    {
                        Check(!isHidden && agent.enabled && !activeHitbox, "invalid destination during Warp safely cancels and restores movement/visibility");
                        Finish();
                    }
                    else if (phase > 0 && Time.unscaledTime - phaseStarted > 6f)
                        throw new TimeoutException("invalid-destination cancellation did not finish");
                }
                catch (Exception error) { Finish(error); }
            };
            EditorApplication.update += tick;
        }
        catch (Exception error) { Finish(error); }
        return done.Task;
    }
}
