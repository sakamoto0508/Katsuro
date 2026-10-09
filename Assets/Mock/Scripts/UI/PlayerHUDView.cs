using UnityEngine;
using UnityEngine.UI;

namespace Mock.UI
{
    /// <summary>空ゲージの上に赤い遅延ダメージ、その上に緑の現在体力を重ねて表示する。</summary>
    public class PlayerHUDView : MonoBehaviour, IKatsuroPlayerHUDView
    {
        /// <summary>Playerの現在HPを即時反映するゲージImage。</summary>
        [UnityEngine.Tooltip("Playerの現在HPを即時反映するゲージImage。")]
        [SerializeField] private Image _hpFill;
        /// <summary>ダメージ前のHPから遅れて減る追従ゲージImage。</summary>
        [UnityEngine.Tooltip("ダメージ前のHPから遅れて減る追従ゲージImage。")]
        [SerializeField] private Image _damageFill;
        /// <summary>Playerのスキルゲージ比率を反映するImage。</summary>
        [UnityEngine.Tooltip("Playerのスキルゲージ比率を反映するImage。")]
        [SerializeField] private Image _skillFill;
        /// <summary>HPダメージ後に追従ゲージが減り始めるまでの時間（実時間の秒）。</summary>
        [UnityEngine.Tooltip("HPダメージ後に追従ゲージが減り始めるまでの時間（実時間の秒）。")]
        [Header("Damage trail (unscaled seconds)")]
        [SerializeField, Min(0f)] private float _damageDelay = .25f;
        /// <summary>HP追従ゲージの減少速度（正規化ゲージ量/秒）。</summary>
        [UnityEngine.Tooltip("HP追従ゲージの減少速度（正規化ゲージ量/秒）。")]
        [SerializeField, Min(.01f)] private float _damageCatchupPerSecond = .65f;
        private bool _initialized;
        private float _targetHp;
        private float _catchupAt;

        /// <summary>現在HPを即反映し、減少時は赤いダメージ履歴を遅延追従させる。回復時は両表示を揃える。</summary>
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

        /// <summary>HPの遅延表示を実時間で進め、HitStop中もUIの追従を維持する。</summary>
        private void Update() => TickDamageTrail(Time.unscaledTime, Time.unscaledDeltaTime);

        /// <summary>減少後の待機時間を過ぎた赤いゲージを現在HPへ指定速度で近づける。</summary>
        private void TickDamageTrail(float now, float deltaTime)
        {
            if (!_initialized || _damageFill == null || now < _catchupAt) return;
            _damageFill.fillAmount = Mathf.MoveTowards(_damageFill.fillAmount, _targetHp,
                Mathf.Max(.01f, _damageCatchupPerSecond) * Mathf.Max(0, deltaTime));
        }

        /// <summary>スキルゲージ値を0から1へ制限してImageの充填量へ反映する。</summary>
        public void SetSkillNormalized(float normalized)
        {
            if (_skillFill != null) _skillFill.fillAmount = Mathf.Clamp01(normalized);
        }

        /// <summary>HPの遅延表示を現在値へ揃え、待機時刻をリセットする。</summary>
        private void OnDisable()
        {
            if (_damageFill != null && _initialized) _damageFill.fillAmount = _targetHp;
            _catchupAt = 0;
        }
    }
}
