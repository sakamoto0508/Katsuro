using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>シーンに配置したゲーム内表示の文字を更新する。表示要素の生成や配置変更は行わない。</summary>
public sealed class RunHUD : MonoBehaviour
{
    /// <summary>HP・スキルゲージ・無敵・Just Avoidボーナスを表示するPlayer参照。</summary>
    [UnityEngine.Tooltip("HP・スキルゲージ・無敵・Just Avoidボーナスを表示するPlayer参照。")]
    [SerializeField] private PlayerController _player;
    /// <summary>ボスHP比率を取得するEnemy参照。</summary>
    [UnityEngine.Tooltip("ボスHP比率を取得するEnemy参照。")]
    [SerializeField] private EnemyController _enemy;
    /// <summary>ボスHPを直接反映するImage。EnemyGauge未設定時に使用する。</summary>
    [UnityEngine.Tooltip("ボスHPを直接反映するImage。EnemyGauge未設定時に使用する。")]
    [SerializeField] private Image _enemyHpFill;
    /// <summary>ボスHPと遅延ダメージを表示するゲージ。設定時は直接Fillより優先する。</summary>
    [UnityEngine.Tooltip("ボスHPと遅延ダメージを表示するゲージ。設定時は直接Fillより優先する。")]
    [SerializeField] private DamageTrailGauge _enemyGauge;
    /// <summary>残機を表すImage一覧。先頭から残機数だけ点灯色にする。</summary>
    [UnityEngine.Tooltip("残機を表すImage一覧。先頭から残機数だけ点灯色にする。")]
    [Header("Life orbs (lit slots equal remaining lives)")]
    [UnityEngine.Serialization.FormerlySerializedAs("_lifeSeals")]
    [SerializeField] private Image[] _lifeOrbs = new Image[0];
    /// <summary>残っている命の表示色。</summary>
    [UnityEngine.Tooltip("残っている命の表示色。")]
    [SerializeField] private Color _lifeActiveColor = Color.white;
    /// <summary>消費済みの命の表示色。</summary>
    [UnityEngine.Tooltip("消費済みの命の表示色。")]
    [SerializeField] private Color _lifeEmptyColor = new Color(.16f, .16f, .16f, .8f);
    /// <summary>Run HUD全体の表示Alphaを制御するCanvasGroup。</summary>
    [UnityEngine.Tooltip("Run HUD全体の表示Alphaを制御するCanvasGroup。")]
    [SerializeField] private CanvasGroup _visibility;
    /// <summary>現在の挑戦者名を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("現在の挑戦者名を表示するTMPテキスト。")]
    [Header("Scene labels (move each Rect Transform to adjust layout)")]
    [SerializeField] private TMP_Text _challenger;
    /// <summary>前回勝者または初期相手の名前を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("前回勝者または初期相手の名前を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _opponent;
    /// <summary>Playerの現在HPを数値で表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("Playerの現在HPを数値で表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _health;
    /// <summary>現在の残機数を文字で表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("現在の残機数を文字で表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _lives;
    /// <summary>選択した攻撃装備・防御装備の名前を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("選択した攻撃装備・防御装備の名前を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _equipment;
    /// <summary>Playerの現在スキルゲージを数値で表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("Playerの現在スキルゲージを数値で表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _skill;
    /// <summary>Just Avoidボーナスの倍率・残り時間と無敵状態を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("Just Avoidボーナスの倍率・残り時間と無敵状態を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _ghostStatus;
    private float _nextRefresh;
    private bool _presentationControlsVisibility;
    /// <summary>勝利演出などの表示所有者から、通常のラン情報を表示してよいか切り替える。</summary>
    public void SetPresentationVisibility(bool controlled) => _presentationControlsVisibility = controlled;

    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>共有Faderを接続し、配置済みのラン情報表示を一度だけ初期化する。</summary>
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

    /// <summary>ランの残機・相手・結果などを表示し、演出による非表示指定を優先する。</summary>
    private void LateUpdate()
    {
        bool visible = RunSession.Active && _player != null
            && !(_fader != null && _fader.IsTransitioning);
        if (_visibility != null && !_presentationControlsVisibility) _visibility.alpha = visible ? 1f : 0f;
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

    /// <summary>参照があり文字列が変わった場合だけTMPテキストを更新する。</summary>
    private static void SetLabel(TMP_Text label, string value)
    {
        if (label != null && label.text != value) label.text = value;
    }
}
