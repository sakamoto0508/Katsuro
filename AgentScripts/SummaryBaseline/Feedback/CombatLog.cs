using System.Diagnostics;
public static class CombatLog
{
    [Conditional("KATSURO_COMBAT_LOG")]
    public static void Trace(object message) => UnityEngine.Debug.Log(message);
}
