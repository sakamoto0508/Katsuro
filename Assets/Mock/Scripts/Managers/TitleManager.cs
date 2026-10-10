using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;



/// <summary>
/// タイトル画面の管理を行うクラス。
/// </summary>
public class TitleManager : MonoBehaviour
{
    /// <summary>タイトルBGMと開始SEの登録名設定。</summary>
    [UnityEngine.Tooltip("タイトルBGMと開始SEの登録名設定。")]
    [SerializeField] private AudioConfig _audioConfig;
    /// <summary>開始後にロードする戦闘Scene名の設定。</summary>
    [UnityEngine.Tooltip("開始後にロードする戦闘Scene名の設定。")]
    [SerializeField] private SceneNameConfig _sceneNameConfig;
    /// <summary>開始操作からScene切り替えまでの待機時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("開始操作からScene切り替えまでの待機時間（実時間の秒）。")]
    [SerializeField] private float _transitionDelay = 2f;

    private bool _isTransitioning = false;
    /// <summary>名前・装備選択を開くRun設定画面の参照。</summary>
    [UnityEngine.Tooltip("名前・装備選択を開くRun設定画面の参照。")]
    [SerializeField] private RunSetupUI _setup;
    /// <summary>タイトル専用の音量設定画面。</summary>
    [SerializeField, Tooltip("タイトル専用の音量設定画面。")] private TitleAudioSettingsUI _audioSettings;
    /// <summary>準備画面やScene遷移と競合せずに音量設定を開けるか。</summary>
    public bool CanOpenAudioSettings => !_isTransitioning && !(_setup != null && _setup.IsOpen) && !(_fader != null && _fader.IsTransitioning);

    /// <summary>配置済みのTitle用依存先とBGMを初期化する。</summary>
    private void Start() => Init();

    private readonly SceneInitialization _scene = new SceneInitialization();
    private AudioManager _audio;
    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>共有Managerとラン設定画面を接続し、Titleの表示・音声を一度だけ準備する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _scene.Init();
        _audio = _scene.Audio;
        _fader = _scene.Fader;
        _scene.InitTitleTexts(this);
        if (_audioSettings != null) _audioSettings.Init(this, _audio);
        // タイトルBGM再生（null ガード）
        if (_audioConfig != null && _audio != null)
        {
            _audio.PlayBGM(_audioConfig.TitleBGM, 0.5f);
        }
    }

    /// <summary>遷移中・設定画面表示中を除き、キーボードなどの開始入力を受け付ける。</summary>
    private void Update()
    {
        if (_isTransitioning || (_setup != null && _setup.IsOpen)) return;
        if (_audioSettings != null && (_audioSettings.ConsumeTitleNavigation() || _audioSettings.BlocksStart)) return;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            OnPressStart();
            return;
        }

        // ゲームパッド
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            OnPressStart();
        }
    }

    /// <summary>
    /// タイトル画面でスタートボタンが押されたときの処理。
    /// </summary>
    public void OnPressStart()
    {
        if (_audioSettings != null && _audioSettings.BlocksStart) return;
        if (_isTransitioning || (_fader != null && _fader.IsTransitioning)) return;
        if (_setup != null && _setup.IsOpen) return;
        if (_setup == null) { Debug.LogError("Assign the scene RunSetupUI to TitleManager.", this); return; }
        if (_audioSettings != null) _audioSettings.SetTitleAvailable(false);
        _setup.Open(this);
    }

    /// <summary>
    /// ゲームシーンへの遷移を開始する。
    /// </summary>
    public void BeginConfiguredGame()
    {
        BeginConfiguredGame(false);
    }

    /// <summary>準備画面の行き先を確定し、正式Runを開始せずにFade遷移を要求する。</summary>
    public void BeginConfiguredGame(bool tutorial)
    {
        if (_isTransitioning || (_audioSettings != null && _audioSettings.BlocksStart)) return;
        _isTransitioning = true;

        // SE再生
        if (_audioConfig != null && _audio != null)
        {
            _audio.PlaySE(_audioConfig.StartSE);
        }

        LoadGameScene(tutorial).Forget();
    }

    /// <summary>
    /// ゲームシーンへの遷移を行う。
    /// </summary>
    /// <returns></returns>
    private async UniTask LoadGameScene(bool tutorial)
    {
        try
        {
            await UniTask.Delay((int)(Mathf.Max(0f, _transitionDelay) * 1000), ignoreTimeScale: true,
                cancellationToken: this.GetCancellationTokenOnDestroy());
            if (_sceneNameConfig == null) throw new System.InvalidOperationException("SceneNameConfig is required.");
            var sceneName = tutorial ? _sceneNameConfig.TutorialScene : _sceneNameConfig.GameScene;
            await _fader.FadeToScene(sceneName);
        }
        catch (System.OperationCanceledException) { }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            if (this != null && _setup != null) _setup.Open(this);
        }
        finally { _isTransitioning = false; }
    }
}
