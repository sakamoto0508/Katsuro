using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>シーンに配置したゲーム内表示の文字を更新する。表示要素の生成や配置変更は行わない。</summary>
public sealed class RunHUD : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private EnemyController _enemy;
    [SerializeField] private Image _enemyHpFill;
    [SerializeField] private DamageTrailGauge _enemyGauge;
    [Header("Life orbs (lit slots equal remaining lives)")]
    [UnityEngine.Serialization.FormerlySerializedAs("_lifeSeals")]
    [SerializeField] private Image[] _lifeOrbs = new Image[0];
    [SerializeField] private Color _lifeActiveColor = Color.white;
    [SerializeField] private Color _lifeEmptyColor = new Color(.16f, .16f, .16f, .8f);
    [SerializeField] private CanvasGroup _visibility;
    [Header("Scene labels (move each Rect Transform to adjust layout)")]
    [SerializeField] private TMP_Text _challenger;
    [SerializeField] private TMP_Text _opponent;
    [SerializeField] private TMP_Text _health;
    [SerializeField] private TMP_Text _lives;
    [SerializeField] private TMP_Text _equipment;
    [SerializeField] private TMP_Text _skill;
    [SerializeField] private TMP_Text _ghostStatus;
    private float _nextRefresh;

    private GlobalFader _fader;
    private bool _initialized;
    public void Init(GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _fader = fader;
        if (_visibility != null)
        {
            _visibility.alpha = 0f;
            _visibility.interactable = false;
            _visibility.blocksRaycasts = false;
        }
    }

    private void LateUpdate()
    {
        bool visible = RunSession.Active && _player != null
            && !(_fader != null && _fader.IsTransitioning);
        if (_visibility != null) _visibility.alpha = visible ? 1f : 0f;
        float enemyHp = _enemy != null ? Mathf.Clamp01(_enemy.HpRatio) : 0f;
        if (_enemyGauge != null) _enemyGauge.SetNormalized(enemyHp);
        else if (_enemyHpFill != null) _enemyHpFill.fillAmount = enemyHp;
        if (!visible || Time.unscaledTime < _nextRefresh) return;
        for (int i = 0; i < _lifeOrbs.Length; i++)
            if (_lifeOrbs[i] != null) _lifeOrbs[i].color = i < RunSession.Lives ? _lifeActiveColor : _lifeEmptyColor;
        _nextRefresh = Time.unscaledTime + .1f;

        SetLabel(_challenger, RunSession.PlayerName);
        SetLabel(_opponent, RunSession.Opponent?.Name ?? "名もなき守人");
        SetLabel(_lives, $"命 {RunSession.Lives}");
        SetLabel(_equipment, $"{RunSession.AttackNames[RunSession.Attack]} / {RunSession.DefenseNames[RunSession.Defense]}");
        var player = _player;
        SetLabel(_health, player != null ? $"HP {player.Hp:0}" : "HP --");
        SetLabel(_skill, player != null ? $"スキル {player.Gauge:0}" : "スキル --");
        SetLabel(_ghostStatus, player == null ? "" :
            (player.JustStacks > 0 ? $"半霊半生 +{player.JustBonus * 100:0}% / {player.JustSeconds:0.0}秒" : "")
            + (player.IsInvulnerable ? "  無敵" : ""));
    }

    private static void SetLabel(TMP_Text label, string value)
    {
        if (label != null && label.text != value) label.text = value;
    }
}
