using UnityEngine;

public class PlayerSprint
{
    public PlayerSprint(SkillGauge skillGauge, float sprintCostPerSecond)
    {
        _skillGauge = skillGauge ?? throw new System.ArgumentNullException(nameof(skillGauge));
        _sprintGaugeCostPerSecond = Mathf.Max(0.01f, sprintCostPerSecond);
    }

    /// <summary>ダッシュ可能か（ゲージがあるか）。</summary>
    public bool CanDash => _skillGauge.Value > Mathf.Epsilon;
    /// <summary>現在ダッシュ中か。</summary>
    public bool IsSprint => _isSprinting && _skillGauge.Value > Mathf.Epsilon;

    private readonly SkillGauge _skillGauge;
    private readonly float _sprintGaugeCostPerSecond;
    private bool _isSprinting;

    /// <summary>ダッシュを開始する。</summary>
    public void BeginDash()
    {
        if (CanDash) _isSprinting = true;
    }

    /// <summary>ダッシュを終了する。</summary>
    public void EndDash() => _isSprinting = false;

    /// <summary>毎フレームの進行処理：ダッシュ継続時はゲージを消費し、枯渇時は自動でダッシュを停止する。</summary>
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        if (_isSprinting)
        {
            float cost = _sprintGaugeCostPerSecond * deltaTime;
            if (!_skillGauge.TryConsume(cost))
            {
                // ゲージ枯渇でダッシュ停止
                _isSprinting = false;
            }
        }
        else
        {
            // 非ダッシュ時はパッシブ回復を任せる（SkillGauge.TickPassive を呼ぶのは外部が責務）
        }
    }
}
