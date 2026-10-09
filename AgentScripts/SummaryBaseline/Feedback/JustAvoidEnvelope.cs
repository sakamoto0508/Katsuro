using UnityEngine;

// Restart from the current value; recovery always uses real time supplied by the owner.
public struct JustAvoidEnvelope
{
    private float _started, _from;
    public bool Playing { get; private set; }
    public float Value { get; private set; }
    public void Begin(float now) { _from = Value; _started = now; Playing = true; }
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
    public void Reset() { Playing = false; Value = 0; }
}
