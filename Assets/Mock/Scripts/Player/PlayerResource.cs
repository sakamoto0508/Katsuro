using System;
using UniRx;
using UnityEngine;

/// <summary>
/// ランタイムの HP 管理（Reactive で現在値を公開）。
/// </summary>
public class PlayerResource : IDisposable
{
    public PlayerResource(PlayerStatus status,PlayerAnimationController animController)
    {
        _maxHp = status != null ? status.MaxHealth : 100f;
        _hpRx = new ReactiveProperty<float>(_maxHp);
        _animController = animController;
    }

    /// <summary>
    /// 現在 HP の Reactive プロパティ（購読専用）。UI 等はこれを Subscribe して値更新を受け取る。
    /// </summary>
    public IReadOnlyReactiveProperty<float> CurrentHpReactive => _hpRx;
    /// <summary>
    /// 最大 HP（読み取り専用）。
    /// </summary>
    public float MaxHp => _maxHp;
    /// <summary>
    /// 現在 HP の生値（最新の値を返す）。Reactive を購読できない場所での即時参照に使う。
    /// </summary>
    public float CurrentHp => _hpRx.Value;
    /// <summary>
    /// 現在 HP の割合（0..1）。モデル側で正規化値を持っておくと便利なため提供します。
    /// </summary>
    public float CurrentHpRatio => _maxHp > 0f ? _hpRx.Value / _maxHp : 0f;
    private readonly PlayerAnimationController _animController;
    private readonly ReactiveProperty<float> _hpRx;
    private readonly float _maxHp;

    /// <summary>
    /// 指定した割合（percent）だけ HP を回復する（percent は 0..100）。
    /// </summary>
    /// <param name="percent">回復割合（1 = 1%）</param>
    public void HealByPercent(float percent)
    {
        if (percent <= 0f) return;
        float amount = _maxHp * (percent / 100f);
        _hpRx.Value = Mathf.Min(_maxHp, _hpRx.Value + amount);
    }

    /// <summary>
    /// 指定した量のダメージを適用する（0 以下は無視）。
    /// </summary>
    /// <param name="amount">適用するダメージ量（生値）</param>
    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        _hpRx.Value = Mathf.Max(0f, _hpRx.Value - amount);
        if(_hpRx.Value <= 0f)
        {
            PlayerDeath();
        }
    }

    public void PlayerDeath()
    {
        _animController?.PlayTrigger(_animController.AnimName.PlayerDead);
        AudioManager.Instance?.PlaySE("PlayerDeath");
        LoadSceneManager.Instance?.LoadSceneAsync(LoadSceneManager.Instance.SceneNameConfig.TitleScene, 1000).Forget();
    }

    public void Dispose()
    {
        _hpRx?.Dispose();
    }
}
