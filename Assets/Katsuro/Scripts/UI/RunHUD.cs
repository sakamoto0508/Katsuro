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
    /// <summary>左下HUDの右隣に置く最大3件の状態表示。空の行は背景とともに非表示にする。</summary>
    [Tooltip("発動中の状態を優先順に表示する3行のTMP。各行の親を状態パネルにしてください。")]
    [SerializeField] private TMP_Text[] _statusLabels = new TMP_Text[0];
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

    /// <summary>相手名・HP・有効な状態表示を更新し、既存ラン情報の互換性と演出側の表示所有権を維持する。</summary>
    private void LateUpdate()
    {
        bool training = GameManager.Instance != null && GameManager.Instance.IsTutorial;
        bool visible = (RunSession.Active || training) && _player != null
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
        SetLabel(_opponent, training ? "修練の相手" : RunSession.Opponent?.Name ?? "名もなき守人");
        SetLabel(_lives, $"命 {RunSession.Lives}");
        SetLabel(_equipment, $"{RunSession.AttackNames[RunSession.Attack]} / {RunSession.DefenseNames[RunSession.Defense]}");
        var player = _player;
        SetLabel(_health, player != null ? $"HP {player.Hp:0}" : "HP --");
        SetLabel(_skill, player != null ? $"スキル {player.Gauge:0}" : "スキル --");
        SetLabel(_ghostStatus, player == null ? "" :
            (player.JustStacks > 0 ? $"半霊半生 +{player.JustBonus * 100:0}% / {player.JustSeconds:0.0}秒" : "")
            + (player.IsInvulnerable ? "  無敵" : ""));
        RefreshStatuses();
    }

    /// <summary>有効な無敵・Just Avoid・回復・バフを優先順に最大3件表示し、使用しない行を隠す。</summary>
    private void RefreshStatuses()
    {
        int index = 0;
        if (_player.IsInvulnerable)
            SetStatus(ref index, _player.ReviveProtectionSeconds > 0f
                ? $"無敵　{_player.ReviveProtectionSeconds:0.0}秒" : "無敵　継続中", new Color(.66f, .83f, .87f));
        if (_player.JustStacks > 0 && _player.JustSeconds > 0f)
            SetStatus(ref index, $"半霊半生　{_player.JustSeconds:0.0}秒", new Color(.87f, .73f, .43f));
        if (_player.IsHealingForHUD)
            SetStatus(ref index, "回復　継続中", new Color(.67f, .83f, .65f));
        if (_player.IsBuffActiveForHUD)
            SetStatus(ref index, "バフ　継続中", new Color(.88f, .63f, .49f));
        for (; index < _statusLabels.Length; index++)
            if (_statusLabels[index] != null) _statusLabels[index].transform.parent.gameObject.SetActive(false);
    }

    /// <summary>空き行に状態名・時間と識別色を反映する。上限を超えた低優先状態は表示しない。</summary>
    private void SetStatus(ref int index, string value, Color color)
    {
        if (index >= Mathf.Min(3, _statusLabels.Length)) return;
        var label = _statusLabels[index++];
        if (label == null) return;
        label.transform.parent.gameObject.SetActive(true);
        label.color = color;
        SetLabel(label, value);
    }

    /// <summary>参照があり文字列が変わった場合だけTMPテキストを更新する。</summary>
    private static void SetLabel(TMP_Text label, string value)
    {
        if (label != null && label.text != value) label.text = value;
    }
}
