using UnityEngine;
using UnityEngine.UI;

/// <summary>ゲージの充填率だけを更新する。画像の色・素材・配置は画面側の設定を維持する。</summary>
public sealed class DamageTrailGauge : MonoBehaviour
{
    [SerializeField] private Image _currentFill;
    [SerializeField] private Image _trailFill;
    [SerializeField, Min(0)] private float _delay = .25f;
    [SerializeField, Min(.01f)] private float _catchupPerSecond = .65f;
    private bool _initialized;
    private float _target, _catchupAt;

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

    private void Update() => Tick(Time.unscaledTime, Time.unscaledDeltaTime);

    private void Tick(float now, float deltaTime)
    {
        if (!_initialized || _trailFill == null || now < _catchupAt) return;
        _trailFill.fillAmount = Mathf.MoveTowards(_trailFill.fillAmount, _target,
            Mathf.Max(.01f, _catchupPerSecond) * Mathf.Max(0, deltaTime));
    }

    private void OnDisable()
    {
        if (_initialized && _trailFill != null) _trailFill.fillAmount = _target;
        _catchupAt = 0;
    }
}
