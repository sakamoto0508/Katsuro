using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


/// <summary>
/// タイトル画面の管理を行うクラス。
/// </summary>
public class TitleManager : MonoBehaviour
{
    [SerializeField] private AudioConfig _audioConfig;
    [SerializeField] private SceneNameConfig _sceneNameConfig;
    [SerializeField] private int _transitionDelay = 1000;

    private bool _isTransitioning = false;

    private void Start()
    {
        // タイトルBGM再生（null ガード）
        if (_audioConfig != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(_audioConfig.TitleBGM);
        }
    }

    private void Update()
    {
        if (_isTransitioning) return;
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

    public void OnPressStart()
    {
        _isTransitioning = true;

        // SE再生
        if (_audioConfig != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(_audioConfig.StartSE);
        }

        LoadGameScene().Forget();
    }

    private async UniTaskVoid LoadGameScene()
    {
        // SE が鳴り終わるまで待機（1秒）
        await UniTask.Delay(_transitionDelay);

        var sceneName = _sceneNameConfig != null ? _sceneNameConfig.GameScene : "GameScene";

        if (LoadSceneManager.Instance != null)
        {
            // 非同期読み込みをトリガー
            LoadSceneManager.Instance.LoadScene(sceneName);
        }
        else
        {
            // フォールバック
            SceneManager.LoadScene(sceneName);
        }
    }
}
