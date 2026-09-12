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

    [Header("Player")]
    [SerializeField] private InputBuffer _inputBuffer;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerAnimationController _playerAnimationController;

    [Header("Config")]
    [SerializeField] private AnimationName _animationName;
    [SerializeField] private CameraConfig _cameraConfig;
    [SerializeField] private AudioConfig _audioConfig;

    [Header("Enemy")]
    [SerializeField] private Transform _enemyPosition;
    [SerializeField] private EnemyController _enemyController;

    [Header("Camera")]
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private CinemachineCamera _cinemachineLockOncamera;

    [Header("Scene")]
    [SerializeField] private LoadSceneManager _loadSceneManager;
    [SerializeField] private DamageNumbers _damageNumbers;

    [SerializeField] private float _soundVolume = 0.3f;


    [SerializeField] private GameState _state = GameState.Title;
    private LockOnCamera _lockOnCamera;

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

    private void Start()
    {
        Init();
        SetGameState(GameState.InGame);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public PlayerController Player => _playerController;
    public bool IsCombatActive => _state == GameState.InGame && RunSession.Active;

    private readonly SceneInitialization _scene = new SceneInitialization();
    private AudioManager _audio;
    private GlobalFader _fader;
    private bool _initialized;
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
        _enemyController?.Init(_playerPosition, _damageNumbers, this, _audio, _scene.HitStop, _scene.FinalBlow, _scene.Loader);
        _cameraManager?.Init(_inputBuffer, _playerPosition
            , _enemyPosition, _cameraConfig, _lockOnCamera, _cinemachineCamera);
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

    public void StartGame()
    {
        SetGameState(GameState.InGame);
    }

    public void WinGame()
    {
        if (!RunSession.Active) return;
        RunSession.Complete(true);
        SetGameState(GameState.Victory);
    }

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

