using System;
using UniRx;
using UnityEngine;

/// <summary>Enemyの最大HPと現在HPを保持して変更を通知する。死亡演出とAI停止はControllerに任せる。</summary>
public class EnemyHealth : IDisposable
{
    /// <summary>基礎HPにラン継承倍率を適用し、現在HPのReactive通知を初期化する。</summary>
    public EnemyHealth(EnemyStuts status)
    {
        _maxHp = status != null ? status.EnemyMaxHealth * RunSession.EnemyHealth : 100f * RunSession.EnemyHealth;
        _hpRx = new ReactiveProperty<float>(_maxHp);
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
    private readonly ReactiveProperty<float> _hpRx;
    private readonly float _maxHp;

    /// <summary>
    /// 指定した量のダメージを適用する（0 以下は無視）。
    /// </summary>
    /// <param name="amount">適用するダメージ量（生値）</param>
    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        _hpRx.Value = Mathf.Max(0f, _hpRx.Value - amount);
    }

    /// <summary>EnemyのHP通知に使用するReactivePropertyを解放する。</summary>
    public void Dispose()
    {
        _hpRx?.Dispose();
    }
}
