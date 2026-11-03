using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    /// <summary>各攻撃カードに表示する効果テキスト。配列順を_attackOptionsに合わせる。</summary>
    [UnityEngine.Tooltip("各攻撃カードに表示する効果テキスト。配列順を_attackOptionsに合わせる。")]
    [SerializeField] private TMP_Text[] _attackEffects;
    /// <summary>各防御カードに表示する効果テキスト。配列順を_defenseOptionsに合わせる。</summary>
    [UnityEngine.Tooltip("各防御カードに表示する効果テキスト。配列順を_defenseOptionsに合わせる。")]
    [SerializeField] private TMP_Text[] _defenseEffects;
    /// <summary>装備中の攻撃装備名を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("装備中の攻撃装備名を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _attackName;
    /// <summary>装備中の防御装備名を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("装備中の防御装備名を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _defenseName;
    /// <summary>選択中の攻撃装備の効果説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("選択中の攻撃装備の効果説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _attackDescription;
    /// <summary>選択中の防御装備の効果説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("選択中の防御装備の効果説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _defenseDescription;
    /// <summary>装備中の攻撃装備の詳しい説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("装備中の攻撃装備の詳しい説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _attackDetail;
    /// <summary>装備中の防御装備の詳しい説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("装備中の防御装備の詳しい説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _defenseDetail;
    /// <summary>攻撃装備ごとの詳しい説明文。配列順を_attackOptionsに合わせる。数値はRunSessionの効果文で表示する。</summary>
    [UnityEngine.Tooltip("攻撃装備ごとの詳しい説明文。配列順を_attackOptionsに合わせる。数値はRunSessionの効果文で表示する。")]
    [SerializeField, TextArea] private string[] _attackDetails;
    /// <summary>防御装備ごとの詳しい説明文。配列順を_defenseOptionsに合わせる。数値はRunSessionの効果文で表示する。</summary>
    [UnityEngine.Tooltip("防御装備ごとの詳しい説明文。配列順を_defenseOptionsに合わせる。数値はRunSessionの効果文で表示する。")]
    [SerializeField, TextArea] private string[] _defenseDetails;
    /// <summary>前回勝者と継承装備の説明を表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("前回勝者と継承装備の説明を表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _opponent;
    /// <summary>残機・復活・勝者継承ルールを表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("残機・復活・勝者継承ルールを表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _rules;
    /// <summary>前回の勝敗結果と保存エラーを表示するTMPテキスト。</summary>
    [UnityEngine.Tooltip("前回の勝敗結果と保存エラーを表示するTMPテキスト。")]
    [SerializeField] private TMP_Text _result;
    /// <summary>結果・エラーが空の時に隠す表示枠。</summary>
    [UnityEngine.Tooltip("結果・エラーが空の時に隠す表示枠。")]
    [SerializeField] private GameObject _resultGroup;
    /// <summary>入力した名前と装備でRunを確定し、戦闘を開始するButton。</summary>
    [UnityEngine.Tooltip("入力した名前と装備でRunを確定し、戦闘を開始するButton。")]
    [SerializeField] private Button _startButton;
    /// <summary>選択した名前と装備で修練場へ入るボタン。</summary>
    [SerializeField, Tooltip("正式Runを開始せず修練場へ移動するボタン。")]
    private Button _tutorialButton;
    /// <summary>表示開始時に下から滑り込ませる画面内容。</summary>
    [UnityEngine.Tooltip("表示開始時に下から滑り込ませる画面内容。")]
    [SerializeField] private RectTransform _content;
    /// <summary>表示開始時に順番にFadeさせる区画（見出し・左列・右列・操作列など）。</summary>
    [UnityEngine.Tooltip("表示開始時に順番にFadeさせる区画（見出し・左列・右列・操作列など）。")]
    [SerializeField] private CanvasGroup[] _revealGroups;
    /// <summary>表示開始時の画面内容の開始位置のずれ。</summary>
    [UnityEngine.Tooltip("表示開始時の画面内容の開始位置のずれ。")]
    [SerializeField] private Vector2 _openOffset = new Vector2(0f, -36f);
    /// <summary>表示開始のFade・Slide時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("表示開始のFade・Slide時間（実時間の秒）。")]
    [SerializeField] private float _openDuration = 0.35f;
    /// <summary>区画ごとの表示開始のずらし時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("区画ごとの表示開始のずらし時間（実時間の秒）。")]
    [SerializeField] private float _revealStagger = 0.05f;
    public bool IsOpen { get; private set; }
    private TitleManager _title;
    private int _attack, _defense, _openedFrame;
    private RunSetupSelectableEffect[] _attackEffectViews, _defenseEffectViews;
    private GameObject _lastFocused;
    private Vector2 _contentPosition;
    private Sequence _openSequence;
    private bool _initialized;
    /// <summary>装備Toggleと開始Buttonの通知を一度だけ接続し、初期状態では設定画面を隠す。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (_content != null) _contentPosition = _content.anchoredPosition;
        _attackEffectViews = EffectViews(_attackOptions);
        _defenseEffectViews = EffectViews(_defenseOptions);
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
        // 通知なしで切り替えたため、装備中の表示だけを即時に合わせる。
        SyncSelected(_attackOptions, _attackEffectViews);
        SyncSelected(_defenseOptions, _defenseEffectViews);
        for (int i = 0; i < _attackEffects.Length; i++) if (_attackEffects[i] != null) _attackEffects[i].text = RunSession.AttackDescription(i);
        for (int i = 0; i < _defenseEffects.Length; i++) if (_defenseEffects[i] != null) _defenseEffects[i].text = RunSession.DefenseDescription(i);
        RefreshDescriptions();
        var champion = RunSession.ReadChampion();
        _opponent.text = champion == null ? "最初の相手：名もなき守人"
            : $"前回の勝者：{champion.Name} / {RunSession.AttackNames[champion.Attack]}・{RunSession.DefenseNames[champion.Defense]}";
        int lives = Mathf.Max(1, GameplayRules.Current.StartingLives);
        _rules.text = lives == 1
            ? $"命は{lives}つ。倒されれば挑戦終了。勝利すると痕跡を更新。"
            : $"命は{lives}つ。復活してもボスのHPは維持。勝利すると痕跡を更新。";
        _result.text = string.Join("\n", new[] { RunSession.Result, RunSession.SaveError }).Trim();
        if (_resultGroup != null) _resultGroup.SetActive(!string.IsNullOrEmpty(_result.text));
        _startButton.interactable = true;
        if (_tutorialButton != null) _tutorialButton.interactable = true;
        SetVisible(true);
        PlayOpen();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _lastFocused = _attackOptions[_attack].gameObject;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_lastFocused);
    }
    /// <summary>マウス操作で選択が外れた後でも、キーボード・ゲームパッド入力で直前の項目へフォーカスを戻す。</summary>
    private void Update()
    {
        if (!IsOpen || EventSystem.current == null) return;
        var current = EventSystem.current.currentSelectedGameObject;
        if (current != null) { _lastFocused = current; return; }
        if (_lastFocused != null && NavigationPressed()) EventSystem.current.SetSelectedGameObject(_lastFocused);
    }
    /// <summary>フォーカス復帰のきっかけとなる方向・決定入力がこのフレームにあったか。</summary>
    /// <returns>キーボードの方向・決定キー、またはゲームパッドの方向・決定入力があればtrue。</returns>
    private static bool NavigationPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame
            || keyboard.tabKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) return true;
        var gamepad = Gamepad.current;
        return gamepad != null && (gamepad.dpad.ReadValue().sqrMagnitude > 0.25f || gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f
            || gamepad.buttonSouth.wasPressedThisFrame);
    }
    /// <summary>現在選択中の攻撃・防御装備の名前と効果説明を更新する。</summary>
    private void RefreshDescriptions()
    {
        if (_attackName != null) _attackName.text = RunSession.AttackNames[_attack];
        if (_defenseName != null) _defenseName.text = RunSession.DefenseNames[_defense];
        _attackDescription.text = RunSession.AttackDescription(_attack);
        _defenseDescription.text = RunSession.DefenseDescription(_defense);
        if (_attackDetail != null) _attackDetail.text = Detail(_attackDetails, _attack);
        if (_defenseDetail != null) _defenseDetail.text = Detail(_defenseDetails, _defense);
    }
    /// <summary>未設定や要素不足を許容して詳しい説明文を取り出す。</summary>
    /// <returns>対応する説明文。なければ空文字。</returns>
    private static string Detail(string[] details, int index) => details != null && index < details.Length ? details[index] : string.Empty;
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
        _openSequence?.Kill();
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
    /// <summary>画面全体をFadeし、内容を短く滑り込ませて区画を順に表示する。入力は最初から受け付ける。</summary>
    private void PlayOpen()
    {
        _openSequence?.Kill();
        _openSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _panel.alpha = 0f;
        _openSequence.Insert(0f, _panel.DOFade(1f, _openDuration * 0.6f).SetEase(Ease.OutQuad));
        if (_content != null)
        {
            _content.anchoredPosition = _contentPosition + _openOffset;
            _openSequence.Insert(0f, _content.DOAnchorPos(_contentPosition, _openDuration).SetEase(Ease.OutCubic));
        }
        if (_revealGroups == null) return;
        for (int i = 0; i < _revealGroups.Length; i++)
        {
            if (_revealGroups[i] == null) continue;
            _revealGroups[i].alpha = 0f;
            _openSequence.Insert(_revealStagger * (i + 1), _revealGroups[i].DOFade(1f, _openDuration).SetEase(Ease.OutCubic));
        }
    }
    /// <summary>Toggleの装備状態を演出なしで表示へ反映する。</summary>
    private static void SyncSelected(Toggle[] options, RunSetupSelectableEffect[] views)
    {
        // Editor上のGetComponentは未装着でも偽nullを返すため、?.ではなくUnityの比較で除外する。
        for (int i = 0; i < views.Length; i++) if (views[i] != null) views[i].ShowSelected(options[i].isOn, false);
    }
    /// <summary>各Toggleに付いた表示演出を取得する。</summary>
    /// <returns>Toggleと同じ順番の表示演出。付いていない要素はnull。</returns>
    private static RunSetupSelectableEffect[] EffectViews(Toggle[] options)
    {
        var views = new RunSetupSelectableEffect[options.Length];
        for (int i = 0; i < options.Length; i++) views[i] = options[i].GetComponent<RunSetupSelectableEffect>();
        return views;
    }
    /// <summary>破棄時に表示開始の演出を停止する。</summary>
    private void OnDestroy() => _openSequence?.Kill();
}
