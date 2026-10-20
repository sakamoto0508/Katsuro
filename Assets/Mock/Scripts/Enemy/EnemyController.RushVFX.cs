using UnityEngine;

/// <summary>RushAttack の進行に合わせて GatotsuRushVFX へ合図を送る。見た目だけで、移動・判定・AI には触れない。</summary>
public partial class EnemyController
{
    private GatotsuRushVFX _rushVfx;

    private GatotsuRushVFX RushVfx(EnemyAttackPattern pattern)
    {
        if (pattern == null || pattern.RushVFX == null || !pattern.UsesDashTuning) return null;
        if (_rushVfx == null)
        {
            Transform tip = null, bladeBase = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "TrailBladeTip") tip = t;
                else if (t.name == "TrailBladeBase") bladeBase = t;
            }
            _rushVfx = gameObject.AddComponent<GatotsuRushVFX>();
            _rushVfx.Initialize(pattern.RushVFX, _animator, tip, bladeBase);
        }
        return _rushVfx;
    }

    /// <summary>構えに入った。構え時間（Stance Duration）から刀先の収束光のタイミングを決める。</summary>
    private void OnRushPatternBegun(EnemyAttackPattern pattern) => RushVfx(pattern)?.BeginStance(pattern.WindupDuration);

    /// <summary>突進開始（攻撃判定 ON）。刺突線は元Clip時刻 ThrustLine.ClipTime を突進区間の再生速度で実時間へ換算する。</summary>
    private void OnRushDashStarted(EnemyAttackPattern pattern)
    {
        var vfx = RushVfx(pattern);
        if (vfx == null) return;
        float clipDelay = Mathf.Max(0f, pattern.RushVFX.ThrustLine.ClipTime - pattern.WindupEndTime);
        vfx.BeginDash(pattern.DashDuration, clipDelay / Mathf.Max(.01f, pattern.DashPlaybackSpeed));
    }

    private void OnRushEnded(bool cancelled) => _rushVfx?.End(cancelled);

    /// <summary>この敵の攻撃が命中したとき CombatFeedback から呼ばれる。刺突線を HitStop と同じ時間だけ保持する。</summary>
    public void NotifyAttackHitFeedback(float hitStopSeconds) => _rushVfx?.OnHit(hitStopSeconds);
}
