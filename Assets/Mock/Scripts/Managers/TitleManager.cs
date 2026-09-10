using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;



/// <summary>
/// タイトル画面の管理を行うクラス。
/// </summary>
public class TitleManager : MonoBehaviour
{
    [SerializeField] private AudioConfig _audioConfig;
    [SerializeField] private SceneNameConfig _sceneNameConfig;
    [SerializeField] private float _transitionDelay = 2f;

    private bool _isTransitioning = false;
    [SerializeField] private RunSetupUI _setup;

    private void Start() => Init();

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        SceneInitialization.Init();
        // タイトルBGM再生（null ガード）
        if (_audioConfig != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(_audioConfig.TitleBGM, 0.5f);
        }
    }

    private void Update()
    {
        if (_isTransitioning || (_setup != null && _setup.IsOpen)) return;
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
        if (_isTransitioning || (GlobalFader.Instance != null && GlobalFader.Instance.IsTransitioning)) return;
        if (_setup != null && _setup.IsOpen) return;
        if (_setup == null) { Debug.LogError("Assign the scene RunSetupUI to TitleManager.", this); return; }
        _setup.Open(this);
    }

    /// <summary>
    /// ゲームシーンへの遷移を開始する。
    /// </summary>
    public void BeginConfiguredGame()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        // SE再生
        if (_audioConfig != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(_audioConfig.StartSE);
        }

        LoadGameScene().Forget();
    }

    /// <summary>
    /// ゲームシーンへの遷移を行う。
    /// </summary>
    /// <returns></returns>
    private async UniTask LoadGameScene()
    {
        try
        {
            await UniTask.Delay((int)(Mathf.Max(0f, _transitionDelay) * 1000), ignoreTimeScale: true,
                cancellationToken: this.GetCancellationTokenOnDestroy());
            var sceneName = _sceneNameConfig != null ? _sceneNameConfig.GameScene : "GameScene";
            await GlobalFader.EnsureInstance().FadeToScene(sceneName);
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