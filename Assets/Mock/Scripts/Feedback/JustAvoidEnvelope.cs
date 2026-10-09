using UnityEngine;

// Restart from the current value; recovery always uses real time supplied by the owner.
/// <summary>現在値から開始できる移行・保持・復帰の包絡線。時間の取得と適用先は呼び出し側が担当する。</summary>
public struct JustAvoidEnvelope
{
    private float _started, _from;
    /// <summary>移行・保持・復帰の時計が有効な間はtrue。</summary>
    public bool Playing { get; private set; }
    /// <summary>現在の包絡線重み。ゼロが通常、1が最大効果。</summary>
    public float Value { get; private set; }
    /// <summary>現在の包絡線値を起点として移行・保持・復帰の時計を再開始する。</summary>
    /// <param name="now">移行開始の基準となる実時刻。</param>
    public void Begin(float now) { _from = Value; _started = now; Playing = true; }
    /// <summary>指定実時刻の重みを計算し、全区間の終了時には自動的に状態をリセットする。</summary>
    /// <returns>移行・保持・復帰に対応する0から1の値。</returns>
    /// <param name="now">開始時と同じ時計の実時刻。</param>
    /// <param name="enter">最大効果へ移行する実時間秒数。</param>
    /// <param name="hold">最大効果を保持する実時間秒数。</param>
    /// <param name="restore">通常へ戻る実時間秒数。</param>
    public float Evaluate(float now, float enter, float hold, float restore)
    {
        if (!Playing) return Value;
        float t = Mathf.Max(0, now - _started);
        enter = Mathf.Max(0, enter); hold = Mathf.Max(0, hold); restore = Mathf.Max(0, restore);
        if (t < enter) Value = Mathf.Lerp(_from, 1, t / enter);
        else if (t < enter + hold) Value = 1;
        else if (restore > 0 && t < enter + hold + restore)
            Value = 1 - Mathf.SmoothStep(0, 1, (t - enter - hold) / restore);
        else Reset();
        return Value;
    }
    /// <summary>包絡線の再生状態を解除し、適用量をゼロへ戻す。</summary>
    public void Reset() { Playing = false; Value = 0; }
}
