using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// ゲーム全体を統括するオーケストレーター。各サブシステムの初期化、ゲーム状態管理、
/// シーン遷移、オーディオ／UI の切り替えなどを担当します。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Title・戦闘・勝利・敗北・Pauseの進行段階を識別し、入力と音声の切り替えに使用する。</summary>
    public enum GameState
    {
        Title,
        InGame,
        Victory,
        Defeat,
        Pause
    }

    // ゲーム状態遷移時に購読できるイベント
    public static event Action<GameState> OnGameStateChanged;

    /// <summary>Player入力通知と戦闘中の入力受付を管理する参照。</summary>
    [UnityEngine.Tooltip("Player入力通知と戦闘中の入力受付を管理する参照。")]
    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    /// <summary>初期化時にカメラとEnemyへ渡すPlayerの位置参照。</summary>
    [UnityEngine.Tooltip("初期化時にカメラとEnemyへ渡すPlayerの位置参照。")]
    [SerializeField] private Transform _playerPosition;
    /// <summary>Playerの初期化・入力・戦闘状態を管理する参照。</summary>
    [UnityEngine.Tooltip("Playerの初期化・入力・戦闘状態を管理する参照。")]
    [SerializeField] private PlayerController _playerController;
    /// <summary>PlayerのAnimation Event・パラメータ制御の参照。</summary>
    [UnityEngine.Tooltip("PlayerのAnimation Event・パラメータ制御の参照。")]
    [SerializeField] private PlayerAnimationController _playerAnimationController;

    /// <summary>Playerと関連処理で使うAnimatorパラメータ名の設定。</summary>
    [UnityEngine.Tooltip("Playerと関連処理で使うAnimatorパラメータ名の設定。")]
    [Header("Config")]
    [SerializeField] private AnimationName _animationName;
    /// <summary>通常追従・Lock-On・命中カメラの調整値。</summary>
    [UnityEngine.Tooltip("通常追従・Lock-On・命中カメラの調整値。")]
    [SerializeField] private CameraConfig _cameraConfig;
    /// <summary>タイトル・戦闘・効果音の登録名設定。</summary>
    [UnityEngine.Tooltip("タイトル・戦闘・効果音の登録名設定。")]
    [SerializeField] private AudioConfig _audioConfig;

    /// <summary>Lock-Onカメラが注視するEnemyの位置参照。</summary>
    [UnityEngine.Tooltip("Lock-Onカメラが注視するEnemyの位置参照。")]
    [Header("Enemy")]
    [SerializeField] private Transform _enemyPosition;
    /// <summary>Enemyの初期化と戦闘終了処理を管理する参照。</summary>
    [UnityEngine.Tooltip("Enemyの初期化と戦闘終了処理を管理する参照。")]
    [SerializeField] private EnemyController _enemyController;

    /// <summary>通常・Lock-On・勝利カメラの制御を接続する参照。</summary>
    [UnityEngine.Tooltip("通常・Lock-On・勝利カメラの制御を接続する参照。")]
    [Header("Camera")]
    [SerializeField] private CameraManager _cameraManager;
    /// <summary>画面に描画する出力Camera。入力の向きやUI投影にも使用する。</summary>
    [UnityEngine.Tooltip("画面に描画する出力Camera。入力の向きやUI投影にも使用する。")]
    [SerializeField] private Camera _camera;
    /// <summary>通常戦闘のCinemachineCamera参照。</summary>
    [UnityEngine.Tooltip("通常戦闘のCinemachineCamera参照。")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    /// <summary>Lock-On用CinemachineCameraの設定参照。</summary>
    [UnityEngine.Tooltip("Lock-On用CinemachineCameraの設定参照。")]
    [SerializeField] private CinemachineCamera _cinemachineLockOncamera;

    /// <summary>戦闘終了時などのSceneロードを接続する参照。</summary>
    [UnityEngine.Tooltip("戦闘終了時などのSceneロードを接続する参照。")]
    [Header("Scene")]
    [SerializeField] private LoadSceneManager _loadSceneManager;
    /// <summary>命中時ダメージ数字を表示するコンポーネント参照。</summary>
    [UnityEngine.Tooltip("命中時ダメージ数字を表示するコンポーネント参照。")]
    [SerializeField] private DamageNumbers _damageNumbers;

    /// <summary>GameManagerがBGM再生を要求する際の基準音量。0で無音、1で最大。</summary>
    [UnityEngine.Tooltip("GameManagerがBGM再生を要求する際の基準音量。0で無音、1で最大。")]
    [SerializeField] private float _soundVolume = 0.3f;


    /// <summary>現在のゲーム進行状態。タイトル・戦闘・勝利・敗北の入力受付と演出を分ける。</summary>
    [UnityEngine.Tooltip("現在のゲーム進行状態。タイトル・戦闘・勝利・敗北の入力受付と演出を分ける。")]
    [SerializeField] private GameState _state = GameState.Title;
    private LockOnCamera _lockOnCamera;

    /// <summary>ゲーム進行管理の共有インスタンスを確立し、重複を破棄してカーソルを戦闘向けに固定する。</summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // ゲーム中はカーソルを固定し、非表示にする。
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Sceneの依存先を接続し、既存の戦闘開始状態へ切り替える。</summary>
    private void Start()
    {
        Init();
        SetGameState(GameState.InGame);
    }

    /// <summary>自身が共有インスタンスの場合に参照を解除する。</summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Playerの初期化・入力・戦闘状態を管理する参照。</summary>
    public PlayerController Player => _playerController;
    /// <summary>戦闘状態と有効なランが両方成立している場合だけtrue。死亡後の入力・AI更新を遮断する。</summary>
    public bool IsCombatActive => _state == GameState.InGame && RunSession.Active;

    private readonly SceneInitialization _scene = new SceneInitialization();
    private AudioManager _audio;
    private GlobalFader _fader;
    private bool _initialized;
    /// <summary>Sceneの共有ManagerとPlayer・Enemy・カメラの依存先を一度だけ組み立てる。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        _scene.Init(this);
        _audio = _scene.Audio;
        _fader = _scene.Fader;
        RunSession.EnsureRun();
        if (_damageNumbers != null) _damageNumbers.Init(_camera);
        else Debug.LogError("GameManagerにDamageNumbersを割り当ててください。", this);
        _lockOnCamera = new LockOnCamera(_playerPosition, _enemyPosition
            , _cinemachineCamera, _cinemachineLockOncamera, _playerAnimationController, _animationName);
        _playerController?.Init(_inputBuffer, _enemyPosition, _camera
            , _cameraManager, _lockOnCamera, this, _audio, _scene.HitStop, _scene.PlayerDead, _scene.Loader);
        _enemyController?.Init(_playerPosition, _damageNumbers, this, _audio, _scene.HitStop, _scene.FinalBlow, _scene.Loader, _cameraManager, _playerController != null ? _playerController.FeedbackConfig : null);
        _cameraManager?.Init(_inputBuffer, _playerPosition
            , _enemyPosition, _cameraConfig, _lockOnCamera, _cinemachineCamera, _camera);
    }

    /// <summary>
    /// ゲーム状態を変更します。副作用として入力の有効/無効、タイムスケール、BGM、UI を切り替えます。
    /// </summary>
    public void SetGameState(GameState newState)
    {
        _state = newState;
        // 入力
        if (_inputBuffer != null)
        {
            var enabled = newState == GameState.InGame;
            _inputBuffer.enabled = enabled;
        }

        // Audio: タイトル画面ならタイトル BGM を再生
        if (_audioConfig != null && _audio != null)
        {
            if (newState == GameState.Title)
            {
                _audio.StopAllBGMs();
                _audio.PlayBGM(_audioConfig.TitleBGM, 0.5f);
            }
            else if (newState == GameState.InGame)
            {
                _audio.StopAllBGMs();
                _audio.PlayBGM(_audioConfig.InGameBGM, 0.5f);
                _audio.PlayBGM(_audioConfig.TitleBGM, 1, _soundVolume);
            }
        }
        // イベント発行
        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>既存のゲーム状態切り替えを通して入力とBGMを戦闘用へ切り替える。</summary>
    public void StartGame()
    {
        SetGameState(GameState.InGame);
    }

    /// <summary>進行中のランだけを勝利として完了し、Victory状態とその通知を確定する。</summary>
    public void WinGame()
    {
        if (!RunSession.Active) return;
        RunSession.Complete(true);
        SetGameState(GameState.Victory);
    }

    /// <summary>進行中のランだけを敗北として完了し、Defeat状態とその通知を確定する。</summary>
    public void LoseGame()
    {
        if (!RunSession.Active) return;
        RunSession.Complete(false);
        SetGameState(GameState.Defeat);
    }

    /// <summary>
    /// LoadSceneManager を経由してシーン切り替えを行います。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (_loadSceneManager != null)
        {
            _loadSceneManager.LoadScene(sceneName);
        }
        else
        {
            _fader.FadeToScene(sceneName).Forget();
        }
    }
}

