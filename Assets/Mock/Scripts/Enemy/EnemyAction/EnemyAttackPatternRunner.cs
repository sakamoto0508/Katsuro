using System;

/// <summary>1Clipの再生・複数斬撃・Recoveryを管理。Animator/武器/AIはControllerへ委譲。</summary>
public sealed class EnemyAttackPatternRunner
{
    public enum Phase { Inactive, Windup, Playing, Recovery }
    public Phase CurrentPhase { get; private set; }
    public bool IsActive => CurrentPhase != Phase.Inactive;
    public EnemyAttackPattern Pattern { get; private set; }
    public int WindowIndex { get; private set; }
    public bool WindowActive { get; private set; }
    private float _elapsed;
    private bool _started;
    private readonly Func<EnemyAttackPattern, bool> _start;
    private readonly Action<EnemyAttackPattern> _windupFinished;
    private readonly Action _closeHitbox;
    private readonly Action<bool> _finished;
    public EnemyAttackPatternRunner(Func<EnemyAttackPattern, bool> start, Action closeHitbox, Action<bool> finished,
        Action<EnemyAttackPattern> windupFinished = null)
    { _start = start; _closeHitbox = closeHitbox; _finished = finished; _windupFinished = windupFinished; }

    public bool Begin(EnemyAttackPattern pattern)
    {
        if (IsActive || pattern == null || !pattern.IsValid) return false;
        Pattern = pattern;
        WindowIndex = 0;
        WindowActive = false;
        _elapsed = 0f;
        _started = false;
        CurrentPhase = pattern.WindupDuration > 0f && pattern.WindupEndTime > 0f ? Phase.Windup : Phase.Playing;
        return true;
    }
    public void Tick(float deltaTime)
    {
        if (!IsActive || !float.IsFinite(deltaTime) || deltaTime <= 0f) return;
        if (Pattern == null || !Pattern.IsValid) { Finish(false); return; }
        // Idleで待たず、最初の更新から同じClipの構えを開始する。
        if (!_started)
        {
            _started = true;
            if (!_start(Pattern)) Finish(false);
            return;
        }
        _elapsed += deltaTime;
        switch (CurrentPhase)
        {
            case Phase.Windup:
                // Trigger未消費・Animator停止などでも永久Busyにしない。
                if (_elapsed >= Pattern.WindupDuration + 1f) Finish(false);
                break;
            case Phase.Playing:
                if (_elapsed >= Pattern.Timeout / Pattern.PlaybackSpeed) Finish(false);
                break;
            case Phase.Recovery:
                if (_elapsed >= Pattern.RecoveryDuration) Finish(true);
                break;
        }
    }
    public void ObserveClipTime(float clipTime, int stateHash)
    {
        if (CurrentPhase != Phase.Windup || !_started || Pattern == null || !Pattern.IsValid ||
            stateHash != Pattern.StateHash || !float.IsFinite(clipTime) || clipTime < Pattern.WindupEndTime) return;
        CurrentPhase = Phase.Playing;
        _elapsed = 0f;
        _windupFinished?.Invoke(Pattern);
    }
    public bool OwnsAnimationState(int stateHash) =>
        (CurrentPhase == Phase.Windup || CurrentPhase == Phase.Playing) && Pattern != null && Pattern.IsValid && Pattern.StateHash == stateHash;
    public bool AcceptsState(int stateHash) => CurrentPhase == Phase.Playing && Pattern != null &&
        Pattern.IsValid && Pattern.StateHash == stateHash;
    public bool OpenWindow(int index, int stateHash)
    {
        if (!AcceptsState(stateHash) || WindowActive || index != WindowIndex + 1) return false;
        WindowIndex = index;
        WindowActive = true;
        return true;
    }
    public bool CloseWindow(int index, int stateHash)
    {
        if (!AcceptsState(stateHash) || !WindowActive || index != WindowIndex) return false;
        WindowActive = false;
        _closeHitbox();
        return true;
    }
    public bool EndClip(int stateHash)
    {
        if (!AcceptsState(stateHash)) return false;
        WindowActive = false;
        _closeHitbox();
        _elapsed = 0f;
        CurrentPhase = Phase.Recovery;
        return true;
    }
    public void Cancel()
    {
        if (!IsActive) return;
        CurrentPhase = Phase.Inactive;
        Pattern = null;
        WindowActive = false;
        WindowIndex = 0;
        _elapsed = 0f;
        _started = false;
        _closeHitbox();
    }
    private void Finish(bool success) { Cancel(); _finished(success); }
}
