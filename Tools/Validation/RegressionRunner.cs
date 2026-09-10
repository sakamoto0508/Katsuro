using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RegressionRunner
{
    private static int _passed;
    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception(name);
        _passed++;
        Debug.Log("PASS: " + name);
    }
    public static void Run()
    {
        try
        {
            var picker = new EnemyDecisionMaker();
            Check(picker.Decide(0, EnemyActionType.Wait, null) == EnemyActionType.Wait, "Missing config falls back to Wait");
            var config = ScriptableObject.CreateInstance<EnemyDecisionConfig>();
            config.NearCandidates = new[] { new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = 10 } };
            config.RepeatPenalty = 0;
            Check(picker.Decide(0, EnemyActionType.Slash, config) == EnemyActionType.Wait, "Zero repeat weight excludes previous action");
            config.RepeatPenalty = 1;
            Check(picker.Decide(0, EnemyActionType.Slash, config) == EnemyActionType.Slash, "Repeat weight one permits same action");
            config.RepeatPenalty = 0.25f;
            Check(picker.Decide(0, EnemyActionType.Slash, config) == EnemyActionType.Slash, "Nonzero repeat penalty retains a sole candidate");
            config.NearCandidates = new[] {
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = float.NaN },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Thrust, Weight = -10 },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Approach, Weight = 0 },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.HeavySlash, Weight = 5 }
            };
            for (int i = 0; i < 1000; i++)
                if (picker.Decide(0, EnemyActionType.Wait, config) != EnemyActionType.HeavySlash) throw new Exception("Invalid weight selected");
            Check(true, "Invalid and zero weights never selected (1000 draws)");
            config.FarCandidates = null;
            Check(picker.Decide(100, EnemyActionType.Wait, config) == EnemyActionType.Wait, "Missing distance candidate list is safe");
            config.NearCandidates = new[] {
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Slash, Weight = 1 },
                new EnemyDecisionConfig.ActionWeight { Action = EnemyActionType.Thrust, Weight = 3 }
            };
            int slashes = 0;
            for (int i = 0; i < 20000; i++) if (picker.Decide(0, EnemyActionType.Wait, config) == EnemyActionType.Slash) slashes++;
            Check(slashes > 4300 && slashes < 5700, "Weighted distribution remains approximately 1:3");
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) picker.Decide(0, EnemyActionType.Wait, config);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "1000 decisions allocate no managed memory");
            UnityEngine.Object.DestroyImmediate(config);

            var owner = new GameObject("Weapon");
            var collider = owner.AddComponent<BoxCollider>();
            var target = new GameObject("Target");
            var targetCollider = target.AddComponent<BoxCollider>();
            var weapon = new EnemyWeapon(new Collider[] { collider, null }, 7);
            Check(!collider.enabled, "Weapon starts with hitbox disabled");
            int hits = 0;
            Action<Collider> handler = other => hits++;
            weapon.RegisterHitObserver(handler);
            weapon.RegisterHitObserver(handler);
            var relay = owner.GetComponent<WeaponHitboxRelay>();
            Check(relay != null && collider.isTrigger, "Typed relay is created and collider is a trigger");
            weapon.EnableHitbox();
            relay.SendMessage("OnTriggerEnter", targetCollider);
            Check(hits == 1, "Registering the same observer does not duplicate hits");
            weapon.UnregisterHitObserver(handler);
            relay.SendMessage("OnTriggerEnter", targetCollider);
            Check(hits == 1, "Unregistered observer receives no hits");
            weapon.CurrentAttackDamage = 19;
            Check(weapon.Damage() == 19, "Attack damage overrides base damage");
            weapon.DisableHitbox();
            Check(!collider.enabled && weapon.Damage() == 7, "Disabling resets hitbox and attack damage");
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("REGRESSION_RESULT: " + _passed + " passed");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
