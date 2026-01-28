using System;
using UnityEngine;
using UniRx;

/// <summary>
/// 汎用スキルゲージ管理（現在値／最大値／パッシブ回復）。
/// </summary>
public class SkillGauge
{
    /// <summary>
    /// コンストラクタ。初期値は最大値で開始します。
    /// </summary>
    /// <param name="max">ゲージの最大値（1以上にクランプされます）。</param>
    /// <param name="passiveRecoveryPerSecond">パッシブ回復量（1秒あたり）。</param>
    public SkillGauge(float max, float passiveRecoveryPerSecond)
    {
        _max = Mathf.Max(1f, max);
        _passiveRecoveryPerSecond = Mathf.Max(0f, passiveRecoveryPerSecond);
        _value = _max;
        _valueRx = new ReactiveProperty<float>(_value);
        _normalizedRx = new ReactiveProperty<float>(Normalized);
    }

    /// <summary>ゲージ値のリアクティブプロパティ（購読用）。</summary>
    public IReadOnlyReactiveProperty<float> ValueReactive => _valueRx;

    /// <summary>正規化されたゲージ値（0..1）のリアクティブプロパティ（購読用）。</summary>
    public IReadOnlyReactiveProperty<float> NormalizedReactive => _normalizedRx;

    /// <summary>現在のゲージ生値（0 ～ Max）。</summary>
    public float Value => _value;

    /// <summary>正規化されたゲージ値（0..1）。</summary>
    public float Normalized => _max > 0f ? _value / _max : 0f;

    /// <summary>ゲージの最大値。</summary>
    public float Max => _max;

    private float _value;
    private readonly float _passiveRecoveryPerSecond;
    private readonly float _max;
    // 内部 ReactiveProperty（外部へは IReadOnlyReactiveProperty として公開）
    private readonly ReactiveProperty<float> _valueRx;
    private readonly ReactiveProperty<float> _normalizedRx;

    /// <summary>
    /// 指定量だけゲージを増加させます。増加した場合はリアクティブ値を更新します。
    /// </summary>
    /// <param name="amount">増加量（負値は無視）。</param>
    public void Add(float amount)
    {
        if (amount <= 0f) return;
        _value = Mathf.Min(_max, _value + amount);
        PublishIfChanged();
    }

    /// <summary>
    /// 指定量だけゲージを消費し、成功すれば true を返します。失敗時はゲージを変更しません。
    /// </summary>
    /// <param name="amount">消費量（負値は 0 扱い）。</param>
    public bool TryConsume(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (_value + Mathf.Epsilon >= amount)
        {
            _value -= amount;
            _value = Mathf.Clamp(_value, 0f, _max);
            PublishIfChanged();
            return true;
        }
        return false;
    }

    /// <summary>
    /// パッシブ回復を Tick 処理で行います（外部から毎フレーム呼ぶこと）。
    /// </summary>
    /// <param name="deltaTime">経過時間（秒）。</param>
    public void TickPassive(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (_passiveRecoveryPerSecond > 0f && _value < _max)
        {
            Add(_passiveRecoveryPerSecond * deltaTime);
        }
    }

    /// <summary>
    /// 内部的に値を 0..Max にクランプし、変化があれば ReactiveProperty を更新します。
    /// </summary>
    internal void Clamp()
    {
        var prev = _value;
        _value = Mathf.Clamp(_value, 0f, _max);
        if (!Mathf.Approximately(prev, _value))
            PublishIfChanged();
    }

    /// <summary>
    /// 内部のゲージ値とリアクティブプロパティの値を同期する。
    /// 現在値または正規化値に変化があった場合のみ通知を発行する。
    /// </summary>
    private void PublishIfChanged()
    {
        // 生のゲージ値（未正規化）
        if (!Mathf.Approximately(_valueRx.Value, _value))
            _valueRx.Value = _value;

        // 正規化値
        var norm = Normalized;
        if (!Mathf.Approximately(_normalizedRx.Value, norm))
            _normalizedRx.Value = norm;
    }
}
