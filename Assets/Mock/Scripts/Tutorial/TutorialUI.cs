using TMPro;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>修練課題と中断・完了・スキップ確認を表示する。戦闘の成功条件はDirectorに任せる。</summary>
public sealed class TutorialUI : MonoBehaviour
{
    /// <summary>現在の段階を表示する文字。</summary>
    [SerializeField, Tooltip("現在の修練段階。")] private TMP_Text _progress;
    /// <summary>達成すべき課題。</summary>
    [SerializeField, Tooltip("現在の課題。")] private TMP_Text _instruction;
    /// <summary>実Bindingから作った操作と補足。</summary>
    [SerializeField, Tooltip("操作と補足の表示。")] private TMP_Text _hint;
    /// <summary>敵の予備動作・再試行の短い補足。</summary>
    [SerializeField, Tooltip("敵の攻撃や再試行の補足文字。")] private TMP_Text _cue;
    /// <summary>中断・完了メニュー。</summary>
    [SerializeField, Tooltip("中断・完了メニューの排他表示。")] private CanvasGroup _menu;
    /// <summary>メニューの見出し。</summary>
    [SerializeField, Tooltip("修練中断または修練完了の見出し。")] private TMP_Text _menuTitle;
    /// <summary>画面の中断ボタン。</summary>
    [SerializeField, Tooltip("修練メニューを開くボタン。")] private Button _pause;
    /// <summary>修練へ戻るボタン。</summary>
    [SerializeField, Tooltip("修練を再開するボタン。")] private Button _resume;
    /// <summary>ボス戦へ進むボタン。</summary>
    [SerializeField, Tooltip("完了時は本編へ、中断時はスキップ確認へ進むボタン。")] private Button _boss;
    /// <summary>Sceneを再読込するボタン。</summary>
    [SerializeField, Tooltip("修練を初めからやり直すボタン。")] private Button _retry;
    /// <summary>タイトルへ戻るボタン。</summary>
    [SerializeField, Tooltip("敗北を記録せずタイトルへ戻るボタン。")] private Button _title;
    /// <summary>スキップ確認のグループ。</summary>
    [SerializeField, Tooltip("修練スキップ確認。")] private CanvasGroup _confirmation;
    /// <summary>スキップを確定するボタン。</summary>
    [SerializeField, Tooltip("ボス戦へ進む確認ボタン。")] private Button _confirm;
    /// <summary>スキップ確認を閉じるボタン。</summary>
    [SerializeField, Tooltip("スキップ確認を取り消すボタン。")] private Button _cancel;
    private TutorialDirector _director;
    /// <summary>課題本文を完了時に自然に消すグループ。</summary>
    [SerializeField, Tooltip("課題文だけのフェード対象。")] private CanvasGroup _task;
    /// <summary>短い達成表示。戦闘時間や進行判定には影響しない。</summary>
    [SerializeField, Tooltip("生成り色の達成文字。")] private TMP_Text _achievement;
    /// <summary>完了メニューの副題。</summary>
    [SerializeField, Tooltip("完了時だけ表示する副題。")] private TMP_Text _menuSubtitle;
    /// <summary>修練Sceneの敵ゲージだけを表示制御する。</summary>
    [SerializeField, Tooltip("本編Prefabは変更せず、修練SceneのEnemyStatusを指定。")] private CanvasGroup _enemyGauge;
    private Func<string> _hintFactory;
    private float _achievementAt = -10f;
    private bool _hasStep, _fadingMenu;
    private TutorialDirector.TutorialStep _step;
    /// <summary>最後に操作された入力機器に応じて操作案内を選ぶ。</summary>
    public bool UsesGamepad { get; private set; }
    private bool _complete, _initialized;
    /// <summary>メニュー操作中は課題の成功を受け付けない。</summary>
    public bool IsMenuOpen => _menu != null && _menu.alpha > 0;
    /// <summary>ボタン通知を一度接続し、通常の課題表示へ切り替える。</summary>
    public void Init(TutorialDirector director)
    {
        if (_initialized) return;
        _initialized = true; _director = director;
        UsesGamepad = PlayerInput.all.Count > 0 && PlayerInput.all[0].currentControlScheme == "Gamepad";
        InputSystem.onEvent += ObserveDevice;
        _pause.onClick.AddListener(() => OpenMenu(false)); _resume.onClick.AddListener(CloseMenu);
        _boss.onClick.AddListener(() => { if (_complete) _director.GoToBoss(); else ShowConfirmation(); });
        _retry.onClick.AddListener(_director.Retry); _title.onClick.AddListener(_director.ReturnToTitle);
        _confirm.onClick.AddListener(_director.GoToBoss); _cancel.onClick.AddListener(HideConfirmation);
        Group(_menu, false); Group(_confirmation, false);
    }
    /// <summary>課題・進捗・操作説明を変更時に更新する。</summary>
    public void SetInstruction(string progress, string instruction, Func<string> hint)
    { _progress.text = progress; _instruction.text = instruction; _hintFactory = hint; _hint.text = hint(); }
    /// <summary>既存の段階変更を表示へ反映する。成功条件や進行時間は変更しない。</summary>
    public void PresentStep(TutorialDirector.TutorialStep step)
    {
        if (_hasStep && step != _step && _step != TutorialDirector.TutorialStep.JustAvoid) _achievementAt = Time.unscaledTime;
        _hasStep = true; _step = step;
        if (_enemyGauge != null) _enemyGauge.alpha = step == TutorialDirector.TutorialStep.Light || step == TutorialDirector.TutorialStep.Combo || step == TutorialDirector.TutorialStep.Heavy || step == TutorialDirector.TutorialStep.Counter ? 1 : 0;
    }
    /// <summary>有意な入力イベントだけで機器を切り替え、実Bindingの表示を再取得する。</summary>
    private void ObserveDevice(InputEventPtr pointer, InputDevice device)
    {
        if (!pointer.IsA<StateEvent>() && !pointer.IsA<DeltaStateEvent>()) return;
        bool changed = false;
        foreach (var control in pointer.EnumerateChangedControls(device, .2f)) { changed = true; break; }
        if (!changed || (!(device is Gamepad) && !(device is Keyboard) && !(device is Mouse))) return;
        bool pad = device is Gamepad;
        if (pad == UsesGamepad) return;
        UsesGamepad = pad;
        if (_hintFactory != null) _hint.text = _hintFactory();
    }
    /// <summary>課題文を保ったまま敵の反復状況を表示する。</summary>
    public void SetCue(string cue) { if (_cue != null) _cue.text = cue; }
    /// <summary>中断または完了メニューを開いて戦闘入力を止める。</summary>
    public void OpenMenu(bool complete)
    {
        _complete = complete; _director.SetPaused(true); Group(_menu, true); Group(_confirmation, false);
        _menuTitle.text = complete ? "修練完了" : "修練を中断";
        if (_menuSubtitle != null) _menuSubtitle.text = complete ? "剣の道を学んだ。いざ、実戦へ。" : "一息ついて、再び構えよう。";
        _fadingMenu = true; _menu.alpha = .01f;
        _resume.gameObject.SetActive(!complete); _pause.interactable = false;
        _boss.GetComponentInChildren<TMP_Text>().text = complete ? "ボス戦へ" : "修練をスキップしてボス戦へ";
        EventSystem.current?.SetSelectedGameObject(complete ? _boss.gameObject : _resume.gameObject);
    }
    /// <summary>完了前の修練を再開し、カーソルと時間を戻す。</summary>
    public void CloseMenu()
    {
        if (_complete) return;
        Group(_menu, false); Group(_confirmation, false); _pause.interactable = true;
        _fadingMenu = false;
        EventSystem.current?.SetSelectedGameObject(null); _director.SetPaused(false);
    }
    /// <summary>スキップを即実行せず確認画面へ進む。</summary>
    private void ShowConfirmation() { _menu.interactable = false; _menu.blocksRaycasts = false; Group(_confirmation, true); EventSystem.current?.SetSelectedGameObject(_cancel.gameObject); }
    /// <summary>スキップを取り消して中断メニューへ戻す。</summary>
    private void HideConfirmation() { Group(_confirmation, false); _menu.interactable = _menu.blocksRaycasts = true; EventSystem.current?.SetSelectedGameObject(_boss.gameObject); }
    /// <summary>Esc・パッドStartで中断し、キャンセルは確認画面から順に閉じる。</summary>
    private void Update()
    {
        if (!_initialized || _director.IsTransitioning) return;
        if (_task != null) _task.alpha = Mathf.MoveTowards(_task.alpha, _complete ? 0 : 1, Time.unscaledDeltaTime / .25f);
        if (_complete && _pause != null) _pause.gameObject.SetActive(false);
        if (_fadingMenu) { _menu.alpha = Mathf.MoveTowards(_menu.alpha, 1, Time.unscaledDeltaTime / .2f); if (_menu.alpha >= 1) _fadingMenu = false; }
        if (_achievement != null) { float t = (Time.unscaledTime - _achievementAt) / .65f; _achievement.text = "達成"; _achievement.alpha = t < 1 && !IsMenuOpen ? Mathf.Min(Mathf.Clamp01(t / .15f), Mathf.Clamp01((1 - t) / .35f)) : 0; }
        bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        bool pause = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (!cancel && !pause) return;
        if (_confirmation.alpha > 0) HideConfirmation();
        else if (IsMenuOpen) CloseMenu();
        else if (pause || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)) OpenMenu(false);
    }
    /// <summary>表示・操作・Raycastをまとめて切り替える。</summary>
    private static void Group(CanvasGroup group, bool show) { group.alpha = show ? 1 : 0; group.interactable = group.blocksRaycasts = show; }
    /// <summary>Scene破棄後に入力通知を残さない。</summary>
    private void OnDestroy() { InputSystem.onEvent -= ObserveDevice; }
}
