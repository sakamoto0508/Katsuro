using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent, RequireComponent(typeof(CameraManager))]
public sealed class JustAvoidContrast : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [SerializeField, Min(0)] private float _enter = .02f;
    [SerializeField, Min(0)] private float _hold = .08f;
    [SerializeField, Min(0)] private float _restore = .20f;
    private JustAvoidEnvelope _envelope;
    private AudioManager _audio;
    private bool _blocked;
    public void Play(AudioManager audio)
    {
        if (!isActiveAndEnabled || _blocked || (FinalBlowManager.Instance != null && FinalBlowManager.Instance.IsPlaying)) return;
        _audio = audio;
        _envelope.Evaluate(Time.unscaledTime, _enter, _hold, _restore);
        _envelope.Begin(Time.unscaledTime);
        _audio?.PlayJustAvoidDuck();
        Update();
    }
    private void Update()
    {
        if (_volume != null) _volume.weight = _envelope.Evaluate(Time.unscaledTime, _enter, _hold, _restore);
    }
    public void Cancel()
    {
        _envelope.Reset();
        if (_volume != null) _volume.weight = 0;
        _audio?.CancelJustAvoidDuck();
    }
    private void SceneChanged(Scene previous, Scene next) => Cancel();
    private void SceneUnloaded(Scene scene) => Cancel();
    private void GameStateChanged(GameManager.GameState state)
    {
        _blocked = state != GameManager.GameState.InGame && state != GameManager.GameState.Pause;
        if (_blocked) Cancel();
    }
    private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; SceneManager.sceneUnloaded += SceneUnloaded; GameManager.OnGameStateChanged += GameStateChanged; }
    private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; SceneManager.sceneUnloaded -= SceneUnloaded; GameManager.OnGameStateChanged -= GameStateChanged; Cancel(); }
    private void OnDestroy() => Cancel();
}
