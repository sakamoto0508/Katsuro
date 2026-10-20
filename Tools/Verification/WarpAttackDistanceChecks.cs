// Run in the connected Editor (Edit Mode):
// unity command run_script --caller plugin --skill unity-cli --format json -- --file Tools/Verification/WarpAttackDistanceChecks.cs --entry WarpAttackDistanceChecks.Run
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class WarpAttackDistanceChecks
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode required");
        var passed = new List<string>();
        var owned = new List<Object>();
        NavMeshDataInstance nav = default;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            passed.Add(name);
        }
        T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }
        try
        {
            var warp = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(
                "Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/WarpAttack.asset")));
            var config = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyDecisionConfig>(
                "Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset")));
            var candidate = new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.WarpAttack, Pattern = warp, Weight = 20f };
            var wait = new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Wait, Weight = 10f };
            var last = new EnemyDecision(EnemyActionType.Wait);
            var maker = new EnemyDecisionMaker(new System.Random(17));
            config.NearDistance = 3f; config.FarDistance = 12f;
            config.MidCandidates = new[] { candidate };
            config.NearCandidates = config.FarCandidates = new[] { wait };
            warp.MinAttackDistance = 25f; warp.MaxAttackDistance = 26f;
            Check(warp.IsValid, "narrow Warp pattern remains structurally valid");
            Check(!config.IsMidDistance(3f) && config.IsMidDistance(3.001f) &&
                config.IsMidDistance(12f) && !config.IsMidDistance(12.001f), "Near exclusive / Far inclusive Mid boundaries");
            foreach (float distance in new[] { 3.001f, 6f, 12f })
                Check(maker.DecideAction(distance, last, config).Pattern == warp, "Mid Warp ignores narrow Min/Max at " + distance);
            Check(maker.DecideAction(3f, last, config).Action == EnemyActionType.Wait &&
                maker.DecideAction(12.001f, last, config).Action == EnemyActionType.Wait, "only active distance-band candidates participate");
            config.NearDistance = 5f; config.FarDistance = 22f;
            Check(maker.DecideAction(20f, last, config).Pattern == warp &&
                maker.DecideAction(5f, last, config).Action == EnemyActionType.Wait,
                "Mid Warp follows changed AI thresholds beyond old Pattern max");
            config.FarCandidates = new[] { candidate };
            Check(maker.DecideAction(24f, last, config).Action == EnemyActionType.Wait &&
                maker.DecideAction(25.5f, last, config).Pattern == warp, "Far Warp preserves legacy Min/Max filtering");
            config.NearCandidates = new[] { candidate };
            Check(maker.DecideAction(4f, last, config).Action == EnemyActionType.Wait, "Near Warp preserves legacy Min/Max filtering");
            Check(!warp.IsInSelectionRange(20f, null), "missing AI config uses legacy Min/Max");
            Check(maker.DecideAction(10f, last, config, _ => false).Action == EnemyActionType.Wait,
                "controller eligibility callback still vetoes Warp");
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
                Check(maker.DecideAction(invalid, last, config).Action == EnemyActionType.Wait, "invalid input rejected: " + invalid);
            config.RepeatPenalty = 0f;
            Check(maker.DecideAction(10f, new EnemyDecision(EnemyActionType.WarpAttack, pattern: warp), config).Action == EnemyActionType.Wait,
                "RepeatPenalty still excludes repeated action");
            config.RepeatPenalty = .25f; config.RepeatPenaltyPerAttack = true;
            Check(maker.DecideAction(10f, new EnemyDecision(EnemyActionType.WarpAttack, pattern: warp), config).Pattern == warp,
                "nonzero per-pattern repeat weight remains selectable");
            config.MidCandidates = new[] { candidate, wait };
            int selected = 0;
            for (int i = 0; i < 1000; i++) if (maker.DecideAction(10f, new EnemyDecision(EnemyActionType.Approach), config).Pattern == warp) selected++;
            Check(selected > 580 && selected < 750, "candidate weights retain 20:10 weighted selection");
            var rush = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(
                "Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/RushAttack.asset")));
            rush.MinAttackDistance = 25f; rush.MaxAttackDistance = 26f;
            config.MidCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.RushAttack, Pattern = rush, Weight = 1f } };
            Check(maker.DecideAction(10f, last, config).Action == EnemyActionType.Wait, "Rush retains Min/Max in Mid");
            var combo = Own(Object.Instantiate(warp));
            var comboData = Own(Object.Instantiate(warp.Attack));
            var data = new SerializedObject(comboData);
            data.FindProperty("_actionType").intValue = (int)EnemyActionType.Slash;
            data.ApplyModifiedPropertiesWithoutUndo(); combo.Attack = comboData;
            config.MidCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Pattern = combo, Weight = 1f } };
            Check(maker.DecideAction(10f, last, config).Action == EnemyActionType.Wait, "normal combo retains Min/Max in Mid");
            config.MidCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.StepBack, Weight = 1f } };
            Check(maker.DecideAction(10f, last, config).Action == EnemyActionType.StepBack, "Backstep still selects without Pattern");
            warp.AnimatorState = "";
            config.MidCandidates = new[] { candidate };
            Check(maker.DecideAction(10f, last, config).Action == EnemyActionType.Wait, "invalid Warp pattern is not rescued by Mid exemption");
            warp.AnimatorState = "Base Layer.Attacks.WarpAttack";

            // Isolated NavMesh and colliders, far above the real GameScene. No saved asset is modified.
            Vector3 origin = new Vector3(2000f, 2000f, 2000f);
            var settings = NavMesh.GetSettingsByIndex(0);
            var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(40f, .2f, 40f),
                transform = Matrix4x4.TRS(origin - Vector3.up * .1f, Quaternion.identity, Vector3.one), area = 0 } };
            var navData = Own(NavMeshBuilder.BuildNavMeshData(settings, sources,
                new Bounds(origin, new Vector3(40f, 8f, 40f)), Vector3.zero, Quaternion.identity));
            Check(navData != null, "isolated NavMesh built");
            nav = NavMesh.AddNavMeshData(navData);
            var player = Own(new GameObject("Warp check target")); player.transform.position = origin;
            var playerBody = player.AddComponent<CapsuleCollider>(); playerBody.radius = .3f; playerBody.height = 1.8f; playerBody.center = Vector3.up;
            var enemy = Own(new GameObject("Warp check body")); enemy.transform.position = origin + Vector3.forward * 6f;
            var body = enemy.AddComponent<CapsuleCollider>(); body.radius = .3f; body.height = 1.8f; body.center = Vector3.up;
            var agent = enemy.AddComponent<NavMeshAgent>(); agent.enabled = false; agent.agentTypeID = settings.agentTypeID;
            Physics.SyncTransforms();
            foreach (float distance in new[] { 2f, 2.5f, 3f })
            {
                warp.TeleportDistance = distance;
                Check(EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out var at) &&
                    Vector3.Distance(Vector3.ProjectOnPlane(at - origin, Vector3.up), Vector3.back * distance) < .02f,
                    "SO TeleportDistance changes safe destination to " + distance + "m");
            }
            warp.TeleportDistance = 2.5f; warp.TeleportAngleOffset = 45f;
            Check(EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out var angled) &&
                Vector3.Distance(Vector3.ProjectOnPlane(angled - origin, Vector3.up),
                    Quaternion.AngleAxis(45f, Vector3.up) * Vector3.back * 2.5f) < .02f, "SO angle rotates the destination around Player's back");
            warp.TeleportAngleOffset = 0f;
            var wall = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = origin + Vector3.back * 1.25f + Vector3.up;
            wall.transform.localScale = new Vector3(2f, 2f, .2f); Physics.SyncTransforms();
            Check(!EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out _), "unbaked corridor wall rejects destination");
            Object.DestroyImmediate(wall); Physics.SyncTransforms();
            warp.TeleportDistance = .1f;
            Check(!EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out _), "too-close destination overlapping Player is rejected");
            warp.TeleportDistance = 100f;
            Check(!EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out _), "off-NavMesh destination is rejected");
            warp.TeleportDistance = 2.5f;
            wall = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = origin + Vector3.back * 2.5f + Vector3.up;
            wall.transform.localScale = Vector3.one; Physics.SyncTransforms();
            Check(!EnemyWarpDestination.TryFind(player.transform, body, agent, warp, out _), "occupied destination is rejected");
            var result = new { mode = "EditMode checks via connected Editor", count = passed.Count, passed };
            Directory.CreateDirectory("Tools/Verification/Results");
            File.WriteAllText("Tools/Verification/Results/WarpAttackDistance.EditMode.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return "PASS " + passed.Count + " checks";
        }
        finally
        {
            if (nav.valid) nav.Remove();
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        }
    }
}
