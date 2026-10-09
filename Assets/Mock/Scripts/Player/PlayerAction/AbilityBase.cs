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
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly ReactiveProperty<bool> _isActiveRx = new ReactiveProperty<bool>(false);
    private readonly Subject<float> _consumedSubject = new Subject<float>();

    /// <summary>ゲージと能力コスト設定を保持し、ゲージ未指定の能力生成を拒否する。</summary>
    private protected AbilityBase(SkillGauge gauge, SkillGaugeCostConfig costConfig = null)
    {
        _skillGauge = gauge ?? throw new ArgumentNullException(nameof(gauge));
        _costConfig = costConfig;
    }

    /// <summary>能力の有効状態をReactivePropertyへ反映して購読者に通知する。</summary>
    private protected void SetActive(bool active)
    {
        _isActiveRx.Value = active;
    }

    /// <summary>能力が消費または進行した量を通知し、HP処理などを外部の購読者へ委譲する。</summary>
    private protected void PublishConsumed(float value)
    {
        _consumedSubject.OnNext(value);
    }

    public bool IsActive => _isActiveRx.Value;

    /// <summary>能力の有効フラグを解除する。派生能力は終了時の追加処理を拡張できる。</summary>
    public virtual void End() => SetActive(false);

    /// <summary>派生能力が時間経過による消費・終了判定を実装するための更新フック。基底では状態を変更しない。</summary>
    public virtual void Tick(float deltaTime) { }

    /// <summary>能力通知を完了し、購読・消費通知・有効状態のReactiveリソースを解放する。</summary>
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
        => _costConfig != null ? Mathf.Max(0.01f, _costConfig.DashPerSecond) : 25f;

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
