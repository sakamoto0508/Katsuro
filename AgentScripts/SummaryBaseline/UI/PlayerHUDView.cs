using UnityEngine;
using UnityEngine.UI;

namespace Mock.UI
{
    /// <summary>空ゲージの上に赤い遅延ダメージ、その上に緑の現在体力を重ねて表示する。</summary>
    public class PlayerHUDView : MonoBehaviour, IKatsuroPlayerHUDView
    {
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _damageFill;
        [SerializeField] private Image _skillFill;
        [Header("Damage trail (unscaled seconds)")]
        [SerializeField, Min(0f)] private float _damageDelay = .25f;
        [SerializeField, Min(.01f)] private float _damageCatchupPerSecond = .65f;
        private bool _initialized;
        private float _targetHp;
        private float _catchupAt;

        public void SetHpNormalized(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (_hpFill != null) _hpFill.fillAmount = normalized;
            if (!_initialized || normalized > _targetHp)
            {
                // 初期表示・回復・復活時には、緑の現在体力と赤い遅延表示を一致させる。
                if (_damageFill != null) _damageFill.fillAmount = normalized;
                _catchupAt = 0;
            }
            else if (normalized < _targetHp)
            {
                _catchupAt = Time.unscaledTime + Mathf.Max(0, _damageDelay);
            }
            _targetHp = normalized;
            _initialized = true;
        }

        private void Update() => TickDamageTrail(Time.unscaledTime, Time.unscaledDeltaTime);

        private void TickDamageTrail(float now, float deltaTime)
        {
            if (!_initialized || _damageFill == null || now < _catchupAt) return;
            _damageFill.fillAmount = Mathf.MoveTowards(_damageFill.fillAmount, _targetHp,
                Mathf.Max(.01f, _damageCatchupPerSecond) * Mathf.Max(0, deltaTime));
        }

        public void SetSkillNormalized(float normalized)
        {
            if (_skillFill != null) _skillFill.fillAmount = Mathf.Clamp01(normalized);
        }

        private void OnDisable()
        {
            if (_damageFill != null && _initialized) _damageFill.fillAmount = _targetHp;
            _catchupAt = 0;
        }
    }
}
