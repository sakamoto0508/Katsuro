using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

/// <summary>戦闘で操作可能になってから操作指南を一度表示し、Fade・一覧再表示・機器切り替えを管理する。戦闘入力は変更しない。</summary>
public sealed class ControlGuidePresenter : MonoBehaviour
{
    /// <summary>操作名とBinding表示先を対応付ける。将来の操作別表示・強調にも使用する。</summary>
    [Serializable]
    public sealed class Row
    {
        /// <summary>InputBufferと対応するInputAction名。ボタン割当そのものはActionから読み取る。</summary>
        [Tooltip("InputBufferと対応するInputAction名。キーやボタンはBindingから取得する。")]
        public string ActionName;
        /// <summary>この操作の表示領域。</summary>
        [Tooltip("操作の行全体。操作別表示の切り替えに使用する。")]
        public RectTransform Root;
        /// <summary>日本語の操作名を表示する文字。</summary>
        [Tooltip("日本語の操作名を表示するTMP文字。")]
        public TMP_Text Label;
        /// <summary>取得したBinding名を表示する文字。</summary>
        [Tooltip("実際のBindingからキー・ボタン名を表示するTMP文字。")]
        public TMP_Text Binding;
        /// <summary>キーキャップ・パッドボタンの背景。</summary>
        [Tooltip("キーキャップ・パッドボタンの背景Image。入力を遮らない。")]
        public UnityEngine.UI.Image Badge;
    }

    /// <summary>既存の戦闘Actionを読み取るInputBuffer参照。</summary>
    [SerializeField, Tooltip("GameSceneのInputBuffer。既存Actionを読み取るだけで有効状態やBindingは変更しない。")]
    private InputBuffer _inputBuffer;
    /// <summary>Fadeさせる操作指南パネル。Raycastは常に無効。</summary>
    [SerializeField, Tooltip("操作指南パネルのCanvasGroup。Alphaで表示し、Raycastは常に無効。")]
    private CanvasGroup _panel;
    /// <summary>非表示後に再表示方法を知らせる小さなヒント。</summary>
    [SerializeField, Tooltip("自動消去後に再表示方法を案内する小さな表示のCanvasGroup。")]
    private CanvasGroup _launcher;
    /// <summary>8操作の表示先。Inspector上の順番はレイアウトの順番と合わせる。</summary>
    [SerializeField, Tooltip("8操作の表示先。Action名はInputBufferと対応させる。")]
    private Row[] _rows = Array.Empty<Row>();
    /// <summary>完全表示を保持する操作可能時間（実時間の秒）。</summary>
    [SerializeField, Min(0), Tooltip("操作可能になってから完全表示を保持する時間（秒）。Pause・ロード中は消費しない。")]
    private float _holdDuration = 5.5f;
    /// <summary>自動表示を透明にする時間（実時間の秒）。</summary>
    [SerializeField, Min(.01f), Tooltip("保持後に自然にFade Outする時間（実時間の秒）。")]
    private float _fadeDuration = .35f;
    /// <summary>パッドの割当済み操作の背景に使う円形Sprite。</summary>
    [SerializeField, Tooltip("ゲームパッドの割当済みボタン・スティックを表示する円形の背景。")]
    private Sprite _gamepadBadge;
    /// <summary>現在の入力機器を表示する文字。</summary>
    [SerializeField, Tooltip("現在表示している入力機器名のTMP文字。")]
    private TMP_Text _deviceLabel;
    /// <summary>Just Avoidの補足または未割当の案内。</summary>
    [SerializeField, Tooltip("回避の補足や未割当の案内を表示するTMP文字。")]
    private TMP_Text _note;
    /// <summary>一覧の固定表示・閉じ方を知らせる文字。</summary>
    [SerializeField, Tooltip("一覧の固定表示または閉じる方法を表示するTMP文字。")]
    private TMP_Text _shortcut;
    /// <summary>一覧を再表示するBinding名を知らせる文字。</summary>
    [SerializeField, Tooltip("自動消去後の小さな再表示ヒントのTMP文字。")]
    private TMP_Text _launcherLabel;
    /// <summary>既存戦闘Actionとは独立した、操作指南だけを開閉する入力。</summary>
    [SerializeField, Tooltip("操作指南専用の開閉Action。既存の戦闘Bindingと競合しないキーを割り当てる。")]
    private InputAction _toggleGuide = new InputAction("ToggleControlGuide", InputActionType.Button);
    /// <summary>固定表示した一覧だけを閉じる入力。</summary>
    [SerializeField, Tooltip("固定表示した操作一覧を閉じる専用Action。戦闘入力の有効状態は変更しない。")]
    private InputAction _closeGuide = new InputAction("CloseControlGuide", InputActionType.Button);

    private GameManager _game;
    private GlobalFader _fader;
    private PlayerInput _playerInput;
    private InputDevice _device;
    private GameManager.GameState _state;
    private bool _initialized, _subscribed, _allowed, _started, _showing, _persistent, _dirty = true;
    private float _elapsed;

    /// <summary>依存先を接続し、既存入力に触れず専用UI入力と状態通知を購読する。</summary>
    public void Init(GameManager game, GlobalFader fader)
    {
        if (_initialized) return;
        _initialized = true;
        _game = game;
        _fader = fader;
        _state = game != null && game.IsCombatActive ? GameManager.GameState.InGame : GameManager.GameState.Title;
        _playerInput = _inputBuffer != null ? _inputBuffer.GetComponent<PlayerInput>() : null;
        _device = _playerInput != null && _playerInput.devices.Count > 0 ? _playerInput.devices[0] : Keyboard.current;
        ResetForNewRun();
        if (isActiveAndEnabled) Subscribe();
    }

    /// <summary>有効化時は初期化済みの場合だけ専用通知を再購読する。</summary>
    private void OnEnable() { if (_initialized) Subscribe(); }

    /// <summary>専用UI Actionだけを有効化し、戦闘Actionには書き込まない。</summary>
    private void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;
        GameManager.OnGameStateChanged += StateChanged;
        InputSystem.onEvent += InputEvent;
        InputSystem.onActionChange += ActionChanged;
        InputSystem.onDeviceChange += DeviceChanged;
        _toggleGuide.performed += ToggleRequested;
        _closeGuide.performed += CloseRequested;
        _toggleGuide.Enable();
        _closeGuide.Enable();
    }

    /// <summary>入力機器・Binding表示を必要時だけ更新し、操作可能な時間だけ表示時計を進める。</summary>
    private void LateUpdate()
    {
        if (!_initialized) return;
        if (_dirty) RefreshBindings();
        Advance(Time.unscaledDeltaTime, CanInteract());
    }

    /// <summary>InGame・生存中のPlayer・既存入力受付・Scene Fade・Final Blowを確認し、ガイドを出せる状況か判定する。</summary>
    private bool CanInteract() => _game != null && _game.IsCombatActive && _state == GameManager.GameState.InGame
        && _game.Player != null && _game.Player.isActiveAndEnabled && _game.Player.Hp > 0
        && _inputBuffer != null && _inputBuffer.isActiveAndEnabled && _inputBuffer.MoveAction != null && _inputBuffer.MoveAction.enabled
        && _playerInput != null && _playerInput.inputIsActive && !(_fader != null && _fader.IsTransitioning)
        && !(FinalBlowManager.Instance != null && FinalBlowManager.Instance.IsPlaying);

    /// <summary>実経過秒で表示を進める。非操作中は非表示にして時計を止め、固定表示には自動Fadeを適用しない。</summary>
    public void Advance(float unscaledDeltaTime, bool canInteract)
    {
        _allowed = canInteract;
        if (!canInteract) { Render(0, false); return; }
        if (!_started) { _started = true; _showing = true; _elapsed = 0; }
        else if (_showing && !_persistent) _elapsed += Mathf.Max(0, unscaledDeltaTime);
        float alpha = _showing ? 1 : 0;
        if (_showing && !_persistent)
        {
            float t = Mathf.Clamp01((_elapsed - _holdDuration) / Mathf.Max(.01f, _fadeDuration));
            alpha = 1 - Mathf.SmoothStep(0, 1, t);
            if (t >= 1) { _showing = false; alpha = 0; }
        }
        Render(alpha, !_showing);
    }

    /// <summary>操作指南と小さな再表示ヒントを描画する。すべてのGraphicを通じて戦闘のRaycastを遮らない。</summary>
    private void Render(float alpha, bool launcher)
    {
        if (_panel != null) { _panel.alpha = alpha; _panel.interactable = false; _panel.blocksRaycasts = false; }
        if (_launcher != null) { _launcher.alpha = launcher ? 1 : 0; _launcher.interactable = false; _launcher.blocksRaycasts = false; }
    }

    /// <summary>現在操作可能なら、プレイヤーが閉じるまで一覧を固定表示する。ゲームをPauseにはしない。</summary>
    public void ShowPersistent()
    {
        if (!_allowed) return;
        _started = _showing = _persistent = true;
        _dirty = true;
        Render(1, false);
    }

    /// <summary>一覧を閉じ、操作可能時には再表示ヒントだけ残す。自動表示を再開始しない。</summary>
    public void HideGuide() { _started = true; _showing = _persistent = false; _dirty = true; Render(0, _allowed); }

    /// <summary>新しいRun用に一度だけの自動表示を待機させる。Pauseからの復帰では呼ばない。</summary>
    public void ResetForNewRun() { _started = _showing = _persistent = _allowed = false; _elapsed = 0; _dirty = true; Render(0, false); }

    /// <summary>勝利・敗北・Titleでは即座に消し、新しい戦闘だけ自動表示を再待機する。Pauseは表示時計を保持する。</summary>
    private void StateChanged(GameManager.GameState state)
    {
        var previous = _state;
        _state = state;
        if (state == GameManager.GameState.InGame && previous != GameManager.GameState.InGame && previous != GameManager.GameState.Pause) ResetForNewRun();
        else if (state != GameManager.GameState.InGame)
        {
            _allowed = false;
            if (state != GameManager.GameState.Pause) { _started = true; _showing = _persistent = false; }
            Render(0, false);
        }
    }

    /// <summary>専用入力で自動表示を固定一覧へ切り替え、固定一覧をもう一度押すと閉じる。</summary>
    private void ToggleRequested(InputAction.CallbackContext context)
    {
        if (!CanInteract()) return;
        _allowed = true;
        if (_persistent) HideGuide(); else ShowPersistent();
    }

    /// <summary>固定表示中だけ閉じる要求を受け付ける。</summary>
    private void CloseRequested(InputAction.CallbackContext context) { if (_persistent) HideGuide(); }

    /// <summary>機器の有意な入力だけを表示切り替えに使う。パッドの微小なスティックノイズは除外する。</summary>
    private void InputEvent(InputEventPtr pointer, InputDevice device)
    {
        if (!(device is Keyboard) && !(device is Mouse) && !(device is Gamepad)) return;
        if (!pointer.IsA<StateEvent>() && !pointer.IsA<DeltaStateEvent>()) return;
        foreach (var control in pointer.EnumerateChangedControls(device, .25f))
        {
            if (device is Mouse && !(control is ButtonControl)) continue;
            if (_device == device) return;
            _device = device;
            _dirty = true;
            return;
        }
    }

    /// <summary>Rebindなどで有効Bindingが変わったときだけ表示を更新する。</summary>
    private void ActionChanged(object target, InputActionChange change) { if (change == InputActionChange.BoundControlsChanged) _dirty = true; }

    /// <summary>表示中の機器が切断された場合はキーボード表示へ戻す。</summary>
    private void DeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (_device == device && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)) _device = Keyboard.current;
        _dirty = true;
    }

    /// <summary>InputBufferの実際のActionを優先して取得する。未初期化のEditor確認時だけPlayerInputから参照する。</summary>
    private InputAction Resolve(string name)
    {
        InputAction action = null;
        if (_inputBuffer != null)
        {
            switch (name)
            {
                case "Move": action = _inputBuffer.MoveAction; break;
                case "LightAttack": action = _inputBuffer.LightAttackAction; break;
                case "StrongAttack": action = _inputBuffer.StrongAttackAction; break;
                case "Evasion": action = _inputBuffer.GhostAction; break;
                case "LookOn": action = _inputBuffer.LookOnAction; break;
                case "Buff": action = _inputBuffer.BuffAction; break;
                case "Heal": action = _inputBuffer.HealAction; break;
                case "Sprint": action = _inputBuffer.SprintAction; break;
            }
        }
        return action ?? _playerInput?.actions?.FindAction("Player/" + name, false);
    }

    /// <summary>操作名の割当表示と開閉ヒントを現在の機器で再構築する。ゲームパッドの未割当を明示する。</summary>
    public void RefreshBindings()
    {
        bool gamepad = _device is Gamepad;
        bool missing = false;
        foreach (var row in _rows)
        {
            string text = ControlGuideBindingDisplay.GetDisplay(Resolve(row.ActionName), gamepad, _device);
            missing |= text == "未割当";
            if (row.Binding != null) row.Binding.text = text;
            if (row.Badge != null) { row.Badge.sprite = gamepad && text != "未割当" ? _gamepadBadge : null; row.Badge.preserveAspect = gamepad; }
        }
        string toggle = ControlGuideBindingDisplay.GetDisplay(_toggleGuide, gamepad, _device);
        string close = ControlGuideBindingDisplay.GetDisplay(_closeGuide, gamepad, _device);
        if (_deviceLabel != null) _deviceLabel.text = gamepad ? "ゲームパッド" : "キーボード・マウス";
        if (_note != null) _note.text = missing ? "未割当の操作は Input Actions で設定が必要" : "タイミングよく回避するとジャスト回避";
        if (_shortcut != null) _shortcut.text = _persistent ? toggle + (close == "未割当" ? "" : " / " + close) + "  閉じる" : toggle + "  操作一覧を固定表示";
        if (_launcherLabel != null) _launcherLabel.text = toggle + "  操作一覧";
        _dirty = false;
    }

    /// <summary>将来のチュートリアル用に指定Actionの行だけ表示する。nullなら全操作へ戻す。</summary>
    public void ShowOnly(string actionName) { foreach (var row in _rows) if (row.Root != null) row.Root.gameObject.SetActive(string.IsNullOrEmpty(actionName) || row.ActionName == actionName); }

    /// <summary>将来のチュートリアル用に指定Actionの操作名を金色で強調する。nullなら全操作を通常色へ戻す。</summary>
    public void Highlight(string actionName) { foreach (var row in _rows) if (row.Label != null) row.Label.color = row.ActionName == actionName ? new Color(.78f, .66f, .4f) : new Color(.93f, .9f, .82f); }

    /// <summary>専用UI入力と購読だけを解除し、Sceneを離れるときにガイドを残さない。</summary>
    private void OnDisable()
    {
        if (_subscribed)
        {
            _subscribed = false;
            GameManager.OnGameStateChanged -= StateChanged;
            InputSystem.onEvent -= InputEvent;
            InputSystem.onActionChange -= ActionChanged;
            InputSystem.onDeviceChange -= DeviceChanged;
            _toggleGuide.performed -= ToggleRequested;
            _closeGuide.performed -= CloseRequested;
            _toggleGuide.Disable();
            _closeGuide.Disable();
        }
        _allowed = false;
        Render(0, false);
    }

    /// <summary>所有する専用UI Actionのリソースを破棄する。戦闘Actionは破棄しない。</summary>
    private void OnDestroy() { _toggleGuide.Dispose(); _closeGuide.Dispose(); }
}
