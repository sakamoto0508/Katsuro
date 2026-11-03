using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>修練専用の進行とScene遷移を所有する。正式Runの時計・残機・勝者保存は使用しない。</summary>
[DefaultExecutionOrder(100)]
public sealed class TutorialDirector : MonoBehaviour
{
    /// <summary>七つの主課題を、個別に実績を確認する小段階へ分ける。</summary>
    public enum TutorialStep { Move, LockOn, Light, Combo, Heavy, Evade, JustAvoid, Counter, Buff, Heal, Complete }
    /// <summary>現在の課題。成功イベントでのみ次へ進む。</summary>
    public TutorialStep Step { get; private set; }
    /// <summary>修練モードに設定された共有戦闘オーケストレーター。</summary>
    [SerializeField, Tooltip("修練SceneのGameManager。")] private GameManager _game;
    /// <summary>タイトル・修練・本編の遷移先を共有する設定。</summary>
    [SerializeField, Tooltip("各Sceneの名前を共有する設定。")] private SceneNameConfig _scenes;
    /// <summary>課題と完了・中断メニューを表示するUI。</summary>
    [SerializeField, Tooltip("課題と修練メニューの表示担当。")] private TutorialUI _ui;
    /// <summary>課題の命中先とLock-On対象となる既存Enemy。</summary>
    [SerializeField, Tooltip("修練の相手。共有Enemyをそのまま参照します。")] private EnemyController _enemy;
    /// <summary>移動課題の目的地Trigger。</summary>
    [SerializeField, Tooltip("移動課題の目的地。")] private TutorialGoal _goal;
    /// <summary>実際のBindingとLock-On通知を取得する共有入力。</summary>
    [SerializeField, Tooltip("Playerの既存InputBuffer。")] private InputBuffer _input;
    /// <summary>回避・反撃課題だけ既存攻撃を反復する制御。</summary>
    [SerializeField, Tooltip("修練専用の攻撃反復制御。")] private TutorialEnemyController _trainingEnemy;
    /// <summary>既存Cinemachineで使っているカメラ操作Action。</summary>
    [SerializeField, Tooltip("既存カメラのLook Action。Binding表示だけに使用します。")] private InputActionReference _cameraLook;
    private bool _transitioning;
    /// <summary>暗転中はメニューも追加操作を受け付けない。</summary>
    public bool IsTransitioning => _transitioning;
    private bool _subscribed, _checkLock;
    private float _nextRecovery, _completedAt, _buffSeconds, _healed;
    private bool _hasCounterOpportunity;
    /// <summary>中断・暗転中の入力や遅れて届いた命中を課題へ数えない。</summary>
    private bool AcceptsSuccess => !_transitioning && !_ui.IsMenuOpen && _game.IsCombatActive && !(GlobalFader.Instance != null && GlobalFader.Instance.IsTransitioning);
    /// <summary>初期化済みの戦闘機能へ修練UIを接続する。</summary>
    private void Start()
    {
        if (_game == null || !_game.IsTutorial) { Debug.LogError("TutorialDirector requires a tutorial GameManager.", this); enabled = false; return; }
        _ui.Init(this);
        _trainingEnemy.Init(_ui);
        Subscribe();
        EnterStep(TutorialStep.Move);
    }
    /// <summary>実際の到達・Lock-On入力・命中結果を一度購読する。</summary>
    private void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true; _goal.Reached += OnReached; _enemy.HitReceived += OnEnemyHit;
        _input.LookOnAction.performed += OnLockInput;
        _game.Player.AvoidSucceeded += OnAvoid;
        _game.Player.JustAvoidSucceeded += OnJustAvoid;
        _game.Player.BuffSucceeded += OnBuff;
        _game.Player.HealSucceeded += OnHeal;
        _game.Player.TrainingRecovered += OnRecovery;
    }
    /// <summary>Scene破棄や無効化時に共有機能からの通知を解除する。</summary>
    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _subscribed = false;
        if (_goal != null) _goal.Reached -= OnReached;
        if (_enemy != null) _enemy.HitReceived -= OnEnemyHit;
        if (_input != null && _input.LookOnAction != null) _input.LookOnAction.performed -= OnLockInput;
        if (_game != null && _game.Player != null)
        {
            _game.Player.AvoidSucceeded -= OnAvoid; _game.Player.JustAvoidSucceeded -= OnJustAvoid;
            _game.Player.BuffSucceeded -= OnBuff; _game.Player.HealSucceeded -= OnHeal;
            _game.Player.TrainingRecovered -= OnRecovery;
        }
    }
    /// <summary>実際の目的地へ到達したPlayerだけ移動課題を達成する。</summary>
    private void OnReached(PlayerController player) { if (AcceptsSuccess && Step == TutorialStep.Move && player == _game.Player) EnterStep(TutorialStep.LockOn); }
    /// <summary>Lock-On切り替え後の結果を次の更新で一度だけ確認する。</summary>
    private void OnLockInput(InputAction.CallbackContext context) { if (Step == TutorialStep.LockOn) _checkLock = true; }
    /// <summary>入力通知後のLock-On対象だけ確認する。全入力・Animatorの継続監視はしない。</summary>
    private void LateUpdate()
    {
        if (!_checkLock) return;
        _checkLock = false;
        if (AcceptsSuccess && Step == TutorialStep.LockOn && _game.Player.IsLockedOnForTutorial(_enemy.transform)) EnterStep(TutorialStep.Light);
    }
    /// <summary>指定相手への実命中から弱攻撃・既存コンボ・強攻撃を区別する。</summary>
    private void OnEnemyHit(DamageInfo info)
    {
        if (!AcceptsSuccess || info.Instigator != _game.Player.gameObject) return;
        if (Step == TutorialStep.Counter && info.IsJustAvoidCounter && _hasCounterOpportunity) { EnterStep(TutorialStep.Buff); return; }
        if (info.IsJustAvoidCounter) return;
        if (Step == TutorialStep.Light && !info.IsHeavy) EnterStep(TutorialStep.Combo);
        else if (Step == TutorialStep.Combo && !info.IsHeavy && _game.Player.AttackComboStepForTutorial > 0) EnterStep(TutorialStep.Heavy);
        else if (Step == TutorialStep.Heavy && info.IsHeavy) EnterStep(TutorialStep.Evade);
    }
    /// <summary>現在使用中の機器に対応するActionの有効Bindingを表示する。</summary>
    private string Binding(InputAction action) => (_ui.UsesGamepad ? "Pad: " : "キー: ") + ControlGuideBindingDisplay.GetDisplay(action, _ui.UsesGamepad, Gamepad.current);
    /// <summary>既存カメラのPointer/delta割当もマウス操作として表示する。共通操作ガイドの表示処理は変更しない。</summary>
    private string CameraBinding()
    {
        var action = _cameraLook != null ? _cameraLook.action : null;
        if (action != null)
            foreach (var binding in action.bindings)
                if (binding.effectivePath == "<Pointer>/delta") return _ui.UsesGamepad ? "Pad: " + ControlGuideBindingDisplay.GetDisplay(action, true, Gamepad.current) : "マウス移動";
        return Binding(action);
    }
    /// <summary>敵の実命中を無効化できた通常回避だけを数える。</summary>
    private void OnAvoid(DamageInfo info) { if (AcceptsSuccess && Step == TutorialStep.Evade && FromTrainingEnemy(info)) EnterStep(TutorialStep.JustAvoid); }
    /// <summary>既存判定で成功したJust Avoidを課題へ反映する。一回で回避と次のJust課題を同時達成しない。</summary>
    private void OnJustAvoid(DamageInfo info)
    {
        if (!AcceptsSuccess || !FromTrainingEnemy(info)) return;
        if (Step == TutorialStep.Evade) EnterStep(TutorialStep.JustAvoid);
        else if (Step == TutorialStep.JustAvoid) { _hasCounterOpportunity = true; EnterStep(TutorialStep.Counter); }
        else if (Step == TutorialStep.Counter) _hasCounterOpportunity = true;
    }
    /// <summary>課題の相手以外からの通知を除く。</summary>
    private bool FromTrainingEnemy(DamageInfo info) => info.Instigator != null && info.Instigator.GetComponentInParent<EnemyController>() == _enemy;
    /// <summary>HPを実際に消費しながらバフが継続した時間を確認する。</summary>
    private void OnBuff()
    {
        if (!AcceptsSuccess || Step != TutorialStep.Buff) return;
        _buffSeconds += Time.deltaTime;
        if (_buffSeconds >= .8f) EnterStep(TutorialStep.Heal);
    }
    /// <summary>準備した減少HPが実際に増えた量を数える。ボタン押下だけでは完了しない。</summary>
    private void OnHeal(float actual)
    {
        if (!AcceptsSuccess || Step != TutorialStep.Heal) return;
        _healed += actual;
        if (_healed >= 5f) EnterStep(TutorialStep.Complete);
    }
    /// <summary>失敗時も同じ課題を維持し、反撃は新しいJust Avoidから試せるようにする。</summary>
    private void OnRecovery()
    {
        _hasCounterOpportunity = false;
        if (Step == TutorialStep.Heal) _game.Player.PrepareTrainingVitals(.55f);
        _ui.SetCue("失敗しても何度でも練習できる。気力を補充しました");
    }
    /// <summary>気力不足だけ定期的に救済し、完了文を短く表示してから選択メニューを開く。</summary>
    private void Update()
    {
        if (_transitioning || _ui == null || _ui.IsMenuOpen) return;
        if (Step == TutorialStep.Complete)
        {
            if (Time.unscaledTime >= _completedAt + 1.2f) _ui.OpenMenu(true);
            return;
        }
        if (_game.IsCombatActive && Time.time >= _nextRecovery)
        {
            _nextRecovery = Time.time + 2f;
            if (_game.Player.Gauge < 10f) { _game.Player.RefillTrainingGauge(); _ui.SetCue("練習用の気力を補充しました。もう一度試そう"); }
        }
    }
    /// <summary>次の小課題に切り替え、目的地表示と課題文を更新する。</summary>
    private void EnterStep(TutorialStep step)
    {
        _ui.PresentStep(step);
        Step = step; _goal.gameObject.SetActive(step == TutorialStep.Move);
        _trainingEnemy.SetPractice(step == TutorialStep.Evade || step == TutorialStep.JustAvoid || step == TutorialStep.Counter);
        _ui.SetCue("");
        switch (step)
        {
            case TutorialStep.Move: _ui.SetInstruction("修練 1 / 7　移動とカメラ", "金色の目印まで移動する", () => "移動 " + Binding(_input.MoveAction) + "\n視点 " + CameraBinding()); break;
            case TutorialStep.LockOn: _ui.SetInstruction("修練 2 / 7　ロックオン", "修練の相手をロックオンする", () => Binding(_input.LookOnAction) + "\n相手へ近づき、向きを合わせよう。"); _checkLock = true; break;
            case TutorialStep.Light: _ui.SetInstruction("修練 3 / 7　攻撃 1 / 3", "弱攻撃を相手に当てる", () => Binding(_input.LightAttackAction) + "\n最初の入力で抜刀。間合いに入って攻撃。"); break;
            case TutorialStep.Combo: _ui.SetInstruction("修練 3 / 7　攻撃 2 / 3", "弱攻撃をつなぎ、次の段を当てる", () => Binding(_input.LightAttackAction) + "\n振り終える前に再入力し、コンボをつなぐ。"); break;
            case TutorialStep.Heavy: _ui.SetInstruction("修練 3 / 7　攻撃 3 / 3", "強攻撃を相手に当てる", () => Binding(_input.StrongAttackAction) + "\n弱攻撃と強攻撃の手応えを比べよう。"); break;
            case TutorialStep.Evade: _ui.SetInstruction("修練 4 / 7　回避", "相手の攻撃を回避する", () => Binding(_input.GhostAction) + "\n構えを見て回避。攻撃が身体を通れば成功。"); _game.Player.RefillTrainingGauge(); break;
            case TutorialStep.JustAvoid: _ui.SetInstruction("修練 5 / 7　ジャスト回避", "刃が届く直前に回避する", () => Binding(_input.GhostAction) + "\n早すぎたら、次の攻撃で少し遅らせよう。"); break;
            case TutorialStep.Counter: _ui.SetInstruction("修練 6 / 7　反撃", "ジャスト回避後の追撃を当てる", () => Binding(_input.LightAttackAction) + "\n成功直後に攻撃。機会を逃したら再度ジャスト回避。"); break;
            case TutorialStep.Buff: _buffSeconds = 0; _game.Player.PrepareTrainingVitals(1f); _ui.SetInstruction("修練 7 / 7　能力 1 / 2", "バフを使い、力を高める", () => Binding(_input.BuffAction) + "\nHPと気力を消費して攻撃を強化。再入力で解除。"); break;
            case TutorialStep.Heal: _healed = 0; _game.Player.PrepareTrainingVitals(.55f); _ui.SetInstruction("修練 7 / 7　能力 2 / 2", "ヒールで減ったHPを回復する", () => Binding(_input.HealAction) + "\n気力を消費して回復。HPが増えるまで継続しよう。"); break;
            case TutorialStep.Complete: _game.Player.PrepareTrainingVitals(1f); _game.SetGameState(GameManager.GameState.Pause); _completedAt = Time.unscaledTime; _ui.SetInstruction("修練完了", "剣の基礎を身につけた", () => "準備ができたら、ボス戦へ。何度でも修練できます。"); break;
        }
    }
    /// <summary>正式Runをまだ開始せず、選択済みの名前・装備でボスSceneへ進む。</summary>
    public void GoToBoss()
    {
        if (_transitioning) return;
        RunSession.Prepare(RunSession.PlayerName, RunSession.Attack, RunSession.Defense);
        Transition(_scenes.GameScene).Forget();
    }
    /// <summary>Sceneを読み直して修練のPlayer・敵・課題を初期化する。</summary>
    public void Retry() => Transition(_scenes.TutorialScene).Forget();
    /// <summary>敗北や勝者保存を発生させずタイトルへ戻る。</summary>
    public void ReturnToTitle() => Transition(_scenes.TitleScene).Forget();
    /// <summary>中断メニューの間は戦闘入力と時間を止め、再開時に復元する。</summary>
    public void SetPaused(bool paused)
    {
        if (_transitioning) return;
        _game.SetGameState(paused ? GameManager.GameState.Pause : GameManager.GameState.InGame);
        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }
    /// <summary>二重遷移を排除し、実時間Fadeへ渡す。失敗した場合は修練メニューへ戻す。</summary>
    private async UniTask Transition(string scene)
    {
        if (_transitioning) return;
        _transitioning = true; _game.SetGameState(GameManager.GameState.Pause); Time.timeScale = 1f;
        try { await GlobalFader.Instance.FadeToScene(scene); }
        catch (System.OperationCanceledException) { }
        catch (System.Exception error) { Debug.LogException(error, this); if (this != null) { _transitioning = false; _ui.OpenMenu(false); } }
    }
    /// <summary>Scene破棄時に修練の時間停止を残さない。</summary>
    private void OnDestroy() { Unsubscribe(); if (Application.isPlaying) Time.timeScale = 1f; }
    /// <summary>無効化中に遅延した命中通知を受け付けない。</summary>
    private void OnDisable() => Unsubscribe();
    /// <summary>再有効化後は初期化済みの場合に通知を再接続する。</summary>
    private void OnEnable() { if (_game != null && _input != null && _input.LookOnAction != null) Subscribe(); }
}
