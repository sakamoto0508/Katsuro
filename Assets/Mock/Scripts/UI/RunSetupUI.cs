using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scene-authored uGUI view. Layout, labels and styles belong to the prefab.</summary>
public sealed class RunSetupUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _panel;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private Toggle[] _attackOptions;
    [SerializeField] private Toggle[] _defenseOptions;
    [SerializeField] private TMP_Text _attackDescription;
    [SerializeField] private TMP_Text _defenseDescription;
    [SerializeField] private TMP_Text _opponent;
    [SerializeField] private TMP_Text _rules;
    [SerializeField] private TMP_Text _result;
    [SerializeField] private Button _startButton;
    public bool IsOpen { get; private set; }
    private TitleManager _title;
    private int _attack, _defense, _openedFrame;
    private bool _initialized;
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
    }
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
        _rules.text = $"命は{GameplayRules.Current.StartingLives}つ。復活してもボスのHPは維持。勝利すると痕跡を更新。";
        _result.text = string.Join("\n", new[] { RunSession.Result, RunSession.SaveError }).Trim();
        _startButton.interactable = true;
        SetVisible(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_attackOptions[_attack].gameObject);
    }
    private void RefreshDescriptions()
    {
        _attackDescription.text = RunSession.AttackDescription(_attack);
        _defenseDescription.text = RunSession.DefenseDescription(_defense);
    }
    public void Confirm()
    {
        // The key that opened the panel must not also submit it.
        if (!IsOpen || _title == null || Time.frameCount == _openedFrame) return;
        RunSession.Begin(_nameInput.text, _attack, _defense);
        IsOpen = false;
        _startButton.interactable = false;
        _panel.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        _title.BeginConfiguredGame();
    }
    private void SetVisible(bool value)
    {
        _panel.alpha = value ? 1f : 0f;
        _panel.interactable = value;
        _panel.blocksRaycasts = value;
    }
}
