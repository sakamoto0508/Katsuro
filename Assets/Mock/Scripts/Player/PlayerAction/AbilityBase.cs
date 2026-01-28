using System;
using UniRx;
using UnityEngine;

/// <summary>
/// 能力の共通ベース（ゲージ参照・チャネリング状態の Reactive 公開・通知の共通化）。
/// OnConsumed は派生クラスで意味を定義する（Heal: 回復% / Ghost: 消費ゲージ量など）。
/// </summary>
public class AbilityBase : IDisposable
{
    public IReadOnlyReactiveProperty<bool> IsActiveRx => _isActiveRx;
    public IObservable<float> OnConsumed => _consumedSubject;

    private protected readonly SkillGauge _skillGauge;
    private protected readonly SkillGaugeCostConfig _costConfig;
    private protected readonly PlayerStateConfig _fallbackStateConfig;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly ReactiveProperty<bool> _isActiveRx = new ReactiveProperty<bool>(false);
    private readonly Subject<float> _consumedSubject = new Subject<float>();

    private protected AbilityBase(SkillGauge gauge, SkillGaugeCostConfig costConfig = null,
        PlayerStateConfig fallbackStateConfig = null)
    {
        _skillGauge = gauge ?? throw new ArgumentNullException(nameof(gauge));
        _costConfig = costConfig;
        _fallbackStateConfig = fallbackStateConfig;
    }

    private protected void SetActive(bool active)
    {
        _isActiveRx.Value = active;
    }

    private protected void PublishConsumed(float value)
    {
        _consumedSubject.OnNext(value);
    }

    public bool IsActive => _isActiveRx.Value;

    public virtual void End() => SetActive(false);

    public virtual void Tick(float deltaTime) { }

    public void Dispose()
    {
        _consumedSubject.OnCompleted();
        _consumedSubject.Dispose();
        _disposables.Dispose();
        _isActiveRx.Dispose();
    }

    /// <summary>
    /// ダッシュの継続コスト（1秒あたり）を取得します。
    /// 両方未設定の場合はデフォルト値 25f を使用し、返値は最小 0.01f にクランプされます。
    /// </summary>
    private protected float GetDashCostPerSecond()
        => _costConfig != null ? Mathf.Max(0.01f, _costConfig.DashPerSecond)
            : Mathf.Max(0.01f, _fallbackStateConfig?.DashGaugeCostPerSecond ?? 25f);

    /// <summary>
    /// ゴースト（幽霊化）の起動時ワンタイムコストを取得します。
    /// costConfig があればそれを利用し、なければデフォルト 20f を返します。
    /// </summary>
    private protected float GetGhostActivationCost()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.GhostActivationCost) : 20f;

    /// <summary>
    /// ゴースト中に継続して消費されるコスト（1秒あたり）を取得します。
    /// costConfig があればそれを利用し、なければデフォルト 5f を返します。
    /// </summary>
    private protected float GetGhostPerSecondCost()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.GhostPerSecondCost) : 5f;

    /// <summary>
    /// 自傷（Self Sacrifice）時のゲージ消費量（1秒あたり）を取得します。
    /// costConfig があればそれを利用し、なければデフォルト 10f を返します。
    /// </summary>
    private protected float GetSelfSacrificeGaugePerSecond()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.SelfSacrificeGaugePerSecond) : 10f;

    /// <summary>
    /// 自傷を許可する最小HP割合（0..1）を取得します。
    /// costConfig があればそれを利用し、なければデフォルト 0.1f を返します。
    /// </summary>
    private protected float GetSelfSacrificeMinHpRatio()
        => _costConfig != null ? Mathf.Clamp01(_costConfig.SelfSacrificeMinHpRatio) : 0.1f;

    /// <summary>
    /// 回復(Heal)で使用する「1% 回復あたりのゲージ消費量」を取得します。
    /// costConfig があればそれを利用し、なければデフォルト 2f を返します。
    /// </summary>
    private protected float GetHealGaugePerPercent()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.HealGaugePerPercent) : 2f;

    /// <summary>
    /// バフモード（Buff Mode）での継続ゲージ消費（1秒あたり）を取得します。
    /// costConfig があればそれを利用し、なければデフォルト 8f を返します。
    /// </summary>
    private protected float GetBuffGaugePerSecond()
        => _costConfig != null ? Mathf.Max(0f, _costConfig.BuffGaugePerSecond) : 8f;
}
