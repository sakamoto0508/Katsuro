using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>シーンに配置して使用する画面表示。配置・文字・見た目はプレハブで設定する。</summary>
public sealed class RunSetupUI : MonoBehaviour
{
    /// <summary>名前・装備選択画面全体の表示と入力受付を切り替えるCanvasGroup。</summary>
    [UnityEngine.Tooltip("名前・装備選択画面全体の表示と入力受付を切り替えるCanvasGroup。")]
    [SerializeField] private CanvasGroup _panel;
    /// <summary>タイトル専用の開始案内。準備画面の表示中は親の不透明度で隠し、文字自身の点滅処理を維持する。</summary>
    [UnityEngine.Tooltip("タイトル専用の開始案内のCanvasGroup。準備画面を表示している間だけ隠します。")]
    [SerializeField] private CanvasGroup _titleStartPrompt;
    /// <summary>新しいRunの挑戦者名を入力するTMP Input Field。</summary>
    [UnityEngine.Tooltip("新しいRunの挑戦者名を入力するTMP Input Field。")]
    [SerializeField] private TMP_InputField _nameInput;
    /// <summary>攻撃装備を選ぶToggle一覧。配列順をRunSession.AttackNamesに合わせる。</summary>
    [UnityEngine.Tooltip("攻撃装備を選ぶToggle一覧。配列順をRunSession.AttackNamesに合わせる。")]
    [SerializeField] private Toggle[] _attackOptions;
    /// <summary>防御装備を選ぶToggle一覧。配列順をRunSession.DefenseNamesに合わせる。</summary>
    [UnityEngine.Tooltip("防御装備を選ぶToggle一覧。配列順をRunSession.DefenseNamesに合わせる。")]
    [SerializeField] private Toggle[] _defenseOptions;
    /// <summary>選択中の攻撃装備の効果説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("選択中の攻撃装備の効果説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _attackDescription;
    /// <summary>選択中の防御装備の効果説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("選択中の防御装備の効果説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _defenseDescription;
    /// <summary>前回勝者と継承装備の説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("前回勝者と継承装備の説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _opponent;
    /// <summary>残機・復活・勝者継承ルールを表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("残機・復活・勝者継承ルールを表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _rules;
    /// <summary>前回の勝敗結果と保存エラーを表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("前回の勝敗結果と保存エラーを表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _result;
    /// <summary>入力した名前と装備でRunを確定し、戦闘を開始するButton。</summary>
    [UnityEngine.Tooltip("入力した名前と装備でRunを確定し、戦闘を開始するButton。")]
    [SerializeField] private Button _startButton;
    /// <summary>選択した名前と装備で修練場へ入るボタン。</summary>
    [SerializeField, Tooltip("正式Runを開始せず修練場へ移動するボタン。")]
    private Button _tutorialButton;
    public bool IsOpen { get; private set; }
    private TitleManager _title;
    private int _attack, _defense, _openedFrame;
    private bool _initialized;
    /// <summary>装備Toggleと開始Buttonの通知を一度だけ接続し、初期状態では設定画面を隠す。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        SetVisible(false);
        for (int i = 0; i < _attackOptions.Length; i++)
        {
            int option = i;
            _attackOptions[i].onValueChanged.AddListener(value => { if (value) { _attack = option; RefreshDescriptions(); } });
        }
        for (int i = 0; i < _defenseOptions.Length; i++)
        {
            int option = i;
            _defenseOptions[i].onValueChanged.AddListener(value => { if (value) { _defense = option; RefreshDescriptions(); } });
        }
        _startButton.onClick.AddListener(Confirm);
        if (_tutorialButton != null) _tutorialButton.onClick.AddListener(ConfirmTutorial);
    }
    /// <summary>保存中の名前・装備・前回勝者を表示し、操作可能な設定画面とUI選択を開始する。</summary>
    public void Open(TitleManager title)
    {
        _title = title;
        _openedFrame = Time.frameCount;
        IsOpen = true;
        _nameInput.SetTextWithoutNotify(RunSession.PlayerName);
        _attack = RunSession.Attack;
        _defense = RunSession.Defense;
        for (int i = 0; i < _attackOptions.Length; i++) _attackOptions[i].SetIsOnWithoutNotify(i == _attack);
        for (int i = 0; i < _defenseOptions.Length; i++) _defenseOptions[i].SetIsOnWithoutNotify(i == _defense);
        RefreshDescriptions();
        var champion = RunSession.ReadChampion();
        _opponent.text = champion == null ? "最初の相手：名もなき守人"
            : $"前回の勝者：{champion.Name} / {RunSession.AttackNames[champion.Attack]}・{RunSession.DefenseNames[champion.Defense]}";
        int lives = Mathf.Max(1, GameplayRules.Current.StartingLives);
        _rules.text = lives == 1
            ? $"命は{lives}つ。倒されれば挑戦終了。勝利すると痕跡を更新。"
            : $"命は{lives}つ。復活してもボスのHPは維持。勝利すると痕跡を更新。";
        _result.text = string.Join("\n", new[] { RunSession.Result, RunSession.SaveError }).Trim();
        _startButton.interactable = true;
        if (_tutorialButton != null) _tutorialButton.interactable = true;
        SetVisible(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_attackOptions[_attack].gameObject);
    }
    /// <summary>現在選択中の攻撃・防御装備の効果説明を更新する。</summary>
    private void RefreshDescriptions()
    {
        _attackDescription.text = RunSession.AttackDescription(_attack);
        _defenseDescription.text = RunSession.DefenseDescription(_defense);
    }
    /// <summary>画面を開いた同フレームの入力を除外し、選択内容でランを開始してTitleの遷移を要求する。</summary>
    public void Confirm()
    {
        ConfirmDestination(false);
    }
    /// <summary>選択内容を保持して修練場への遷移を要求する。</summary>
    public void ConfirmTutorial() => ConfirmDestination(true);
    /// <summary>二重実行と開始フレームの決定を除外し、選択内容だけを保存して行き先を要求する。</summary>
    private void ConfirmDestination(bool tutorial)
    {
        // 画面を開いたときの入力で、そのまま決定されないようにする。
        if (!IsOpen || _title == null || Time.frameCount == _openedFrame) return;
        RunSession.Prepare(_nameInput.text, _attack, _defense);
        IsOpen = false;
        _startButton.interactable = false;
        if (_tutorialButton != null) _tutorialButton.interactable = false;
        _panel.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        _title.BeginConfiguredGame(tutorial);
    }
    /// <summary>設定パネルの表示と入力受付を切り替え、タイトル専用の開始案内を反対の表示状態にする。</summary>
    private void SetVisible(bool value)
    {
        _panel.alpha = value ? 1f : 0f;
        _panel.interactable = value;
        _panel.blocksRaycasts = value;
        if (_titleStartPrompt != null)
        {
            _titleStartPrompt.alpha = value ? 0f : 1f;
            _titleStartPrompt.interactable = !value;
            _titleStartPrompt.blocksRaycasts = !value;
        }
    }
}
