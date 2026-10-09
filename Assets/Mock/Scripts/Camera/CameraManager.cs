using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>入力・Lock-On・Cinemachineと戦闘時の一時的な画面補正を接続し、勝利中は専用カメラへ制御を渡す。</summary>
public class CameraManager : MonoBehaviour
{
    /// <summary>描画補正とHDRP Passの対象になるSceneの出力カメラ。</summary>
    public Camera OutputCamera => _outputCamera;
    /// <summary>命中時の揺れを発生させるCinemachine Impulse Source。</summary>
    [UnityEngine.Tooltip("命中時の揺れを発生させるCinemachine Impulse Source。")]
    [SerializeField] private CinemachineImpulseSource _combatImpulse;
    /// <summary>Final Blow中に加える画角の差（度）。負数で画角を狭めて寄りを強調する。</summary>
    [UnityEngine.Tooltip("Final Blow中に加える画角の差（度）。負数で画角を狭めて寄りを強調する。")]
    [Header("Final Blow")]
    [SerializeField, Range(-6f, -3f)] private float _finalBlowFOVOffset = -4f;
    /// <summary>Final Blowの画角が寄るまでの時間（秒）。</summary>
    [UnityEngine.Tooltip("Final Blowの画角が寄るまでの時間（秒）。")]
    [SerializeField, Range(.3f, .6f)] private float _finalBlowPushDuration = .42f;
    private InputBuffer _inputBuffer;
    private LockOnCamera _lookOnCamera;
    private LockOnCameraMover _lockOnCameraMover;
    private Camera _outputCamera;
    private CameraConfig _config;
    private float _fovStarted = float.NegativeInfinity;
    private bool _finalBlowPlaying;
    private float _finalBlowStarted;
    private Vector3 _savedPosition;
    private float _savedFov;
    private bool _renderOffsetApplied;
    private bool _subscribed;
    private bool _initialized;
    private VictoryCameraRig _victoryRig;
    /// <summary>入力イベントとLock-On移動、出力カメラ、勝利カメラの依存先を一度だけ接続する。</summary>
    public void Init(InputBuffer inputBuffer, Transform playerPosition
        , Transform enemyPosition, CameraConfig config, LockOnCamera lockOnCamera, CinemachineCamera camera, Camera outputCamera = null)
    {
        if (_initialized) return;
        _initialized = true;
        _outputCamera = outputCamera;
        _victoryRig = GetComponent<VictoryCameraRig>();
        _victoryRig?.Init(playerPosition, outputCamera);
        _config = config;
        if (_combatImpulse == null) _combatImpulse = GetComponent<CinemachineImpulseSource>();
        if (_combatImpulse != null)
        {
            _combatImpulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _combatImpulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            _combatImpulse.ImpulseDefinition.ImpulseChannel = 2;
        }
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        SubscribeRendering();
        _inputBuffer = inputBuffer;
        InputEventRegistry(_inputBuffer);
        _lookOnCamera = lockOnCamera;
        _lockOnCameraMover = new LockOnCameraMover(lockOnCamera, playerPosition
            , enemyPosition, camera, config);
    }

    /// <summary>描画イベントと入力購読を解除し、勝利カメラの一時状態を解放する。</summary>
    private void OnDestroy()
    {
        UnsubscribeRendering();
        _victoryRig?.Stop();
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
    }

    /// <summary>攻撃種別に応じた短いCinemachine Impulseを命中位置から発生させる。</summary>
    public void PlayHitFeedback(bool heavy, Vector3 contactPoint)
    {
        if (!_initialized || !isActiveAndEnabled || _combatImpulse == null || _config == null) return;
        float strength = heavy ? _config.HeavyHitCameraStrength : _config.LightHitCameraStrength;
        _combatImpulse.ImpulseDefinition.ImpulseDuration = _config.CameraDuration;
        _combatImpulse.GenerateImpulseAtPositionWithVelocity(contactPoint, Vector3.down * strength);
    }
    /// <summary>Final Blowが優先中でなければ、ジャスト回避のFOV補正時計を開始する。</summary>
    public void PlayJustAvoidFeedback()
    {
        if (_initialized && isActiveAndEnabled && !_finalBlowPlaying) _fovStarted = Time.unscaledTime;
    }
    /// <summary>回避FOVを解除して勝利FOVと専用構図を開始する。寄り始めは指定秒数だけ遅らせる。</summary>
    public void PlayFinalBlowFeedback(float delay = 0f)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        _fovStarted = float.NegativeInfinity;
        _finalBlowStarted = Time.unscaledTime + Mathf.Max(0f, delay);
        _finalBlowPlaying = true;
        _victoryRig?.Begin(delay);
    }
    /// <summary>勝利補正を解除し、出力カメラとCinemachineの通常制御を復元する。</summary>
    public void StopFinalBlowFeedback() { _finalBlowPlaying = false; RestoreCamera(); _victoryRig?.Stop(); }
    /// <summary>初期化済みなら描画前後の補正イベントを再登録する。</summary>
    private void OnEnable() { if (_initialized) SubscribeRendering(); }
    /// <summary>描画購読を解除し、回避・勝利の一時カメラ補正を消去する。</summary>
    private void OnDisable() { UnsubscribeRendering(); _fovStarted = float.NegativeInfinity; StopFinalBlowFeedback(); }
    /// <summary>出力カメラの描画前後で一時補正を適用・解除するイベントを重複なく登録する。</summary>
    private void SubscribeRendering()
    {
        if (_subscribed) return;
        _subscribed = true;
        RenderPipelineManager.beginCameraRendering += BeginRendering;
        RenderPipelineManager.endCameraRendering += EndRendering;
    }
    /// <summary>描画イベントを解除し、適用中の一時補正が残らないよう復元する。</summary>
    private void UnsubscribeRendering()
    {
        if (!_subscribed) return;
        _subscribed = false;
        RenderPipelineManager.beginCameraRendering -= BeginRendering;
        RenderPipelineManager.endCameraRendering -= EndRendering;
        RestoreCamera();
    }
    // Cinemachineと手動ロックオンの更新後、描画する瞬間だけ小さな補正を重ねる。
    /// <summary>Cinemachine更新後の出力構図を保存し、その描画だけImpulseとFOV補正を重ねる。</summary>
    private void BeginRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _outputCamera || _config == null) return;
        RestoreCamera();
        _savedPosition = camera.transform.position;
        _savedFov = camera.fieldOfView;
        _renderOffsetApplied = true;
        if (_combatImpulse != null && CinemachineImpulseManager.Instance.GetImpulseAt(
            _savedPosition, false, 2, out var offset, out var rotation))
            camera.transform.position += Vector3.ClampMagnitude(offset, .15f);
        float progress = (Time.unscaledTime - _fovStarted) / Mathf.Max(.01f, _config.JustAvoidFOVDuration);
        if (_finalBlowPlaying)
        {
            float push = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - _finalBlowStarted) / Mathf.Max(.01f, _finalBlowPushDuration)));
            camera.fieldOfView = Mathf.Clamp(_savedFov + _finalBlowFOVOffset * push, 1f, 179f);
        }
        else if (progress >= 0f && progress < 1f)
            camera.fieldOfView = Mathf.Clamp(_savedFov + _config.JustAvoidFOVOffset * Mathf.Sin(progress * Mathf.PI), 1f, 179f);
    }
    /// <summary>出力カメラの描画終了後に元の位置とFOVを戻し、次のカメラ更新に補正を持ち越さない。</summary>
    private void EndRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _outputCamera) RestoreCamera();
    }
    /// <summary>描画用に保存した位置とFOVを一度だけ復元する。</summary>
    private void RestoreCamera()
    {
        if (!_renderOffsetApplied || _outputCamera == null) return;
        _outputCamera.transform.position = _savedPosition;
        _outputCamera.fieldOfView = _savedFov;
        _renderOffsetApplied = false;
    }

    /// <summary>勝利中は専用仮想カメラを進行し、それ以外はLock-Onの手動追従を更新する。</summary>
    private void LateUpdate()
    {
        if (_finalBlowPlaying) { _victoryRig?.Tick(Time.unscaledTime); return; }
        _lockOnCameraMover?.LateUpdate();
    }

    /// <summary>Lock-On開始入力をこのカメラ管理へ接続する。</summary>
    private void InputEventRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.LookOnAction.started += OnLookOnAction;
    }

    /// <summary>Lock-On入力の購読を解除して破棄後の呼び出しを防ぐ。</summary>
    private void InputEventUnRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.LookOnAction.started -= OnLookOnAction;
    }

    /// <summary>現在のLock-On状態に応じて対象の固定と解除を切り替える。</summary>
    private void OnLookOnAction(InputAction.CallbackContext context)
    {
        if (_lookOnCamera.IsLockOn == false)
        {
            _lookOnCamera?.LockOn();
        }
        else
        {
            _lookOnCamera?.UnLockOn();
        }
    }
}
