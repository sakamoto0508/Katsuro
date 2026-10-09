using System.Diagnostics;
/// <summary>戦闘の診断ログを条件付きで出力する。リリース時の不要なログ呼び出しを抑える。</summary>
public static class CombatLog
{
    /// <summary>戦闘診断用の文字列をUnity Consoleへ出力する。条件付きコンパイルで不要な呼び出しを省く。</summary>
    [Conditional("KATSURO_COMBAT_LOG")]
    public static void Trace(object message) => UnityEngine.Debug.Log(message);
}
