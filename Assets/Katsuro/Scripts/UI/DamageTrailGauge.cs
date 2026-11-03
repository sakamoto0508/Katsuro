using UnityEngine;
using UnityEngine.UI;

/// <summary>ゲージの充填率だけを更新する。画像の色・素材・配置は画面側の設定を維持する。</summary>
public sealed class DamageTrailGauge : MonoBehaviour
{
    /// <summary>現在値を即時反映する手前のゲージImage。Filled方式で使用する。</summary>
    [UnityEngine.Tooltip("現在値を即時反映する手前のゲージImage。Filled方式で使用する。")]
    [SerializeField] private Image _currentFill;
    /// <summary>ダメージ前の値から遅れて追従する奥のゲージImage。</summary>
    [UnityEngine.Tooltip("ダメージ前の値から遅れて追従する奥のゲージImage。")]
    [SerializeField] private Image _trailFill;
    /// <summary>ダメージ追従ゲージが減り始めるまでの待機時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("ダメージ追従ゲージが減り始めるまでの待機時間（実時間の秒）。")]
    [SerializeField, Min(0)] private float _delay = .25f;
    /// <summary>追従ゲージが現在値へ近づく速度（正規化ゲージ量/秒）。大きいほど速く追従する。</summary>
    [UnityEngine.Tooltip("追従ゲージが現在値へ近づく速度（正規化ゲージ量/秒）。大きいほど速く追従する。")]
    [SerializeField, Min(.01f)] private float _catchupPerSecond = .65f;
    private bool _initialized;
    private float _target, _catchupAt;

    /// <summary>現在ゲージを即時更新する。減少時だけ遅延追従を予約し、回復・初期化時は遅延ゲージも一致させる。</summary>
    public void SetNormalized(float value)
    {
        value = Mathf.Clamp01(value);
        if (_currentFill != null) _currentFill.fillAmount = value;
        if (!_initialized || value > _target)
        {
            if (_trailFill != null) _trailFill.fillAmount = value;
            _catchupAt = 0;
        }
        else if (value < _target) _catchupAt = Time.unscaledTime + Mathf.Max(0, _delay);
        _target = value;
        _initialized = true;
    }

    /// <summary>TimeScaleに依存せず遅延ゲージの追従を進める。</summary>
    private void Update() => Tick(Time.unscaledTime, Time.unscaledDeltaTime);

    /// <summary>待機時間が終わった遅延ゲージを現在値へ指定速度で近づける。</summary>
    private void Tick(float now, float deltaTime)
    {
        if (!_initialized || _trailFill == null || now < _catchupAt) return;
        _trailFill.fillAmount = Mathf.MoveTowards(_trailFill.fillAmount, _target,
            Mathf.Max(.01f, _catchupPerSecond) * Mathf.Max(0, deltaTime));
    }

    /// <summary>遅延表示を現在値へ揃えて待機時刻を解除する。</summary>
    private void OnDisable()
    {
        if (_initialized && _trailFill != null) _trailFill.fillAmount = _target;
        _catchupAt = 0;
    }
}
