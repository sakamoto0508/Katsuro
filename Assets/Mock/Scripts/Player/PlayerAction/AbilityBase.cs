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
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly ReactiveProperty<bool> _isActiveRx = new ReactiveProperty<bool>(false);
    private readonly Subject<float> _consumedSubject = new Subject<float>();

    private protected AbilityBase(SkillGauge gauge)
    {
        _skillGauge = gauge ?? throw new ArgumentNullException(nameof(gauge));
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
}
