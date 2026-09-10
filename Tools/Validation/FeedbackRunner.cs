using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FeedbackRunner
{
    private static int count;
    private static void Check(bool result, string description)
    {
        if (!result) throw new Exception(description);
        count++; Debug.Log("PASS: " + description);
    }
    private static void Call(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    public static void Run()
    {
        try
        {
            var go = new GameObject("Speed regression");
            var animator = go.AddComponent<Animator>();
            var speed = AnimationSpeedController.For(animator);
            speed.Init();
            speed.SetStatus(.05f);
            speed.SetTemporary(0, 10);
            Check(animator.speed == 0, "Hit stop during slow");
            speed.Init();
            speed.SetStatus(1);
            Check(animator.speed == 0, "Status expiry must not cancel hit stop");
            speed.ClearTemporary();
            Check(Mathf.Approximately(animator.speed, 1), "Status expires first: normal speed restored");
            speed.SetStatus(.05f);
            speed.SetTemporary(0, 10);
            speed.ClearTemporary();
            Check(Mathf.Approximately(animator.speed, .05f), "Hit stop expires first: slow preserved");
            speed.Init();
            speed.SetStatus(1);
            Check(Mathf.Approximately(animator.speed, 1), "Slow then expires without retaining captured speed");
            speed.SetTemporary(0, 10);
            speed.SetTemporary(.2f, 10);
            speed.ClearTemporary();
            Check(Mathf.Approximately(animator.speed, 1), "Overlapping temporary effects restore base");
            speed.SetTemporary(0, 0);
            Check(Mathf.Approximately(animator.speed, 1), "Zero duration expires immediately");
            speed.SetStatus(.05f);
            speed.SetTemporary(0, 10);
            Call(speed, "OnDisable");
            Check(Mathf.Approximately(animator.speed, 1), "Disable restores baseline");
            var statuses = go.AddComponent<StatusEffectManager>();
            statuses.Init();
            var def = ScriptableObject.CreateInstance<StatusEffectDef>();
            statuses.ApplyStatusEffect(new StatusEffectInstance(def, go));
            speed.SetTemporary(0, 10);
            statuses.RemoveStatusEffect(def.Id);
            speed.ClearTemporary();
            Check(Mathf.Approximately(animator.speed, 1), "Real status manager and hit stop share the same writer");
            statuses.ApplyStatusEffect(new StatusEffectInstance(def, go));
            Call(statuses, "OnDisable");
            Check(Mathf.Approximately(animator.speed, 1) && !statuses.HasStatusEffect(def.Id), "Status manager disable clears status and speed");
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("FEEDBACK_RESULT: " + count + " passed");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
