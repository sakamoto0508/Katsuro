using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>タイトルのオーディオ設定と入力の排他制御を管理する。</summary>
public class TitleAudioSettingsUI : MonoBehaviour
{
    /// <summary>タイトル左上の設定ボタン。</summary>
    [SerializeField, Tooltip("タイトル左上の設定ボタン。")] private Button _gear;
    /// <summary>設定ボタンの表示・操作を切り替えるグループ。</summary>
    [SerializeField, Tooltip("設定ボタンの表示・操作を切り替えるグループ。")] private CanvasGroup _gearGroup;
    /// <summary>設定画面の表示・操作を切り替えるグループ。</summary>
    [SerializeField, Tooltip("設定画面の表示・操作を切り替えるグループ。")] private CanvasGroup _modal;
    /// <summary>Master・BGM・SEの順に並ぶ音量スライダー。</summary>
    [SerializeField, Tooltip("Master・BGM・SEの順に並ぶ音量スライダー。")] private Slider[] _sliders;
    /// <summary>各音量のパーセント表示。</summary>
    [SerializeField, Tooltip("各音量のパーセント表示。")] private TMP_Text[] _percentages;
    /// <summary>ユーザー音量を初期値へ戻すボタン。</summary>
    [SerializeField, Tooltip("ユーザー音量を初期値へ戻すボタン。")] private Button _defaults;
    /// <summary>設定を保存して閉じるボタン。</summary>
    [SerializeField, Tooltip("設定を保存して閉じるボタン。")] private Button _back;
    /// <summary>設定表示中に隠すタイトル開始案内。Scene側で接続する。</summary>
    [SerializeField, Tooltip("設定表示中に隠すタイトル開始案内。Scene側で接続する。")] private CanvasGroup _titlePrompt;
    private AudioManager _audio;
    private TitleManager _title;
    private bool _initialized, _titleAvailable = true, _awaitRelease;
    private int _closedFrame = -1;
    /// <summary>設定が表示中かを返す。</summary>
    public bool IsOpen { get; private set; }
    /// <summary>設定表示・終了入力・設定ボタン選択中の開始を遮断する。</summary>
    public bool BlocksStart => IsOpen || _awaitRelease || Time.frameCount <= _closedFrame || GearSelected;
    private bool GearSelected => _gear != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _gear.gameObject;

    /// <summary>共有音声とタイトルを接続し、UIイベントを一度だけ登録する。</summary>
    public void Init(TitleManager title, AudioManager audio)
    {
        if (_initialized) return;
        _initialized = true; _title = title; _audio = audio;
        _gear.onClick.AddListener(Open); _back.onClick.AddListener(Close);
        _defaults.onClick.AddListener(RestoreDefaults);
        foreach (var slider in _sliders) slider.onValueChanged.AddListener(OnVolumeChanged);
        SyncValues(); SetGroup(_modal, false); SetGroup(_gearGroup, true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    /// <summary>タイトルのTabまたはパッドSelectを設定ボタンへのフォーカスとして受け取る。</summary>
    public bool ConsumeTitleNavigation()
    {
        if (!_titleAvailable || IsOpen) return false;
        if ((Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame))
        { EventSystem.current?.SetSelectedGameObject(_gear.gameObject); return true; }
        return GearSelected;
    }
    /// <summary>設定画面だけを操作可能にし、最初の音量項目を選択する。</summary>
    public void Open()
    {
        if (!_initialized || !_titleAvailable || !_title.CanOpenAudioSettings) return;
        IsOpen = true; SyncValues(); SetGroup(_modal, true); SetGroup(_gearGroup, false);
        SetGroup(_titlePrompt, false);
        EventSystem.current?.SetSelectedGameObject(_sliders[0].gameObject);
    }
    /// <summary>設定を保存してタイトルへ戻し、閉じる入力が離されるまで開始を抑止する。</summary>
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false; _audio?.SaveUserVolumes(); _closedFrame = Time.frameCount; _awaitRelease = true;
        SetGroup(_modal, false); SetGroup(_gearGroup, _titleAvailable); SetGroup(_titlePrompt, _titleAvailable);
        EventSystem.current?.SetSelectedGameObject(null);
    }
    /// <summary>準備画面・遷移中は設定ボタンの表示と操作を停止する。</summary>
    public void SetTitleAvailable(bool available) { _titleAvailable = available; SetGroup(_gearGroup, available && !IsOpen); }
    /// <summary>キャンセル入力で設定を閉じ、開始抑止は全開始入力の解放後に解除する。</summary>
    private void Update()
    {
        var keyboard = Keyboard.current; var pad = Gamepad.current;
        bool cancel = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
        if (IsOpen && cancel) Close();
        else if (!IsOpen && GearSelected && cancel) { EventSystem.current.SetSelectedGameObject(null); _closedFrame = Time.frameCount; _awaitRelease = true; }
        if (_awaitRelease && Time.frameCount > _closedFrame &&
            !(keyboard != null && keyboard.anyKey.isPressed) && !(pad != null && (pad.buttonSouth.isPressed || pad.buttonEast.isPressed))) _awaitRelease = false;
    }
    /// <summary>三つのユーザー倍率と表示値を即時更新する。</summary>
    private void OnVolumeChanged(float unused)
    {
        _audio?.SetUserVolumes(_sliders[0].value / 100f, _sliders[1].value / 100f, _sliders[2].value / 100f);
        for (int i = 0; i < 3; i++) _percentages[i].text = Mathf.RoundToInt(_sliders[i].value) + "%";
    }
    /// <summary>三項目を100%へ戻し、スライダーとラベルに反映する。</summary>
    private void RestoreDefaults() { _audio?.ResetUserVolumes(); SyncValues(); }
    /// <summary>イベントを発火せずに現在の音量からUIを復元する。</summary>
    private void SyncValues()
    {
        float[] values = { _audio != null ? _audio.MasterVolume : 1f, _audio != null ? _audio.BGMVolume : 1f, _audio != null ? _audio.SEVolume : 1f };
        for (int i = 0; i < 3; i++) { _sliders[i].SetValueWithoutNotify(Mathf.RoundToInt(values[i] * 100)); _percentages[i].text = Mathf.RoundToInt(values[i] * 100) + "%"; }
    }
    /// <summary>グループの可視状態と操作・Raycastを同時に切り替える。</summary>
    private static void SetGroup(CanvasGroup group, bool visible)
    { if (group == null) return; group.alpha = visible ? 1f : 0f; group.interactable = visible; group.blocksRaycasts = visible; }
}
