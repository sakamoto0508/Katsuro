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
    private JustAvoidEnvelope _justAvoidEnvelope;
    private Transform _playerPosition;
    private System.Func<bool> _justAvoidSustain;
    private float _justAvoidSustainStarted = float.NegativeInfinity;
    private bool _finalBlowPlaying;
    private float _finalBlowStarted;
    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private Vector3 _lastLockPosition, _releasePosition;
    private Quaternion _lastLockRotation, _releaseRotation;
    private bool _wasLocked;
    private float _releaseStarted = float.NegativeInfinity;
    private float _savedFov;
    private Vector3 _appliedPosition;
    private Quaternion _appliedRotation;
    private float _appliedFov;
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
        _playerPosition = playerPosition;
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
    public void PlayHitFeedback(bool heavy, Vector3 contactPoint, bool counter = false, bool counterFirst = false)
    {
        if (!_initialized || !isActiveAndEnabled || _finalBlowPlaying || _combatImpulse == null || _config == null) return;
        float strength = _config.GetHitCameraStrength(heavy, counter, counterFirst);
        _combatImpulse.ImpulseDefinition.ImpulseDuration = _config.GetHitCameraDuration(heavy, counter, counterFirst);
        _combatImpulse.GenerateImpulseAtPositionWithVelocity(contactPoint, Vector3.down * strength);
    }
    /// <summary>Final Blowが優先中でなければ、ジャスト回避の寄り・ローアングル・FOV補正の時計を開始する。</summary>
    public void PlayJustAvoidFeedback()
    {
        if (!_initialized || !isActiveAndEnabled || _finalBlowPlaying) return;
        _justAvoidEnvelope.Begin(Time.unscaledTime);
        _justAvoidSustainStarted = float.NegativeInfinity;
    }
    /// <summary>ジャスト回避のカメラ演出を最大のまま維持する条件を登録する。trueの間は復帰を始めない。</summary>
    public void SetJustAvoidSustain(System.Func<bool> sustain) => _justAvoidSustain = sustain;
    /// <summary>回避FOVを解除して勝利FOVと専用構図を開始する。寄り始めは指定秒数だけ遅らせる。</summary>
    public void PlayFinalBlowFeedback(float delay = 0f)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        _justAvoidEnvelope.Reset();
        _finalBlowStarted = Time.unscaledTime + Mathf.Max(0f, delay);
        _finalBlowPlaying = true;
        _victoryRig?.Begin(delay);
    }
    /// <summary>勝利補正を解除し、出力カメラとCinemachineの通常制御を復元する。</summary>
    public void StopFinalBlowFeedback() { _finalBlowPlaying = false; RestoreCamera(); _victoryRig?.Stop(); }
    /// <summary>初期化済みなら描画前後の補正イベントを再登録する。</summary>
    private void OnEnable() { if (_initialized) SubscribeRendering(); }
    /// <summary>描画購読を解除し、回避・勝利の一時カメラ補正を消去する。</summary>
    private void OnDisable() { UnsubscribeRendering(); _justAvoidEnvelope.Reset(); _releaseStarted = float.NegativeInfinity; _wasLocked = false; StopFinalBlowFeedback(); }
    /// <summary>出力カメラの描画前後で一時補正を適用・解除するイベントを重複なく登録する。</summary>
    private void SubscribeRendering()
    {
        if (_subscribed) return;
        _subscribed = true;
        Application.onBeforeRender += BeforeRender;
        RenderPipelineManager.endCameraRendering += EndRendering;
    }
    /// <summary>描画イベントを解除し、適用中の一時補正が残らないよう復元する。</summary>
    private void UnsubscribeRendering()
    {
        if (!_subscribed) return;
        _subscribed = false;
        Application.onBeforeRender -= BeforeRender;
        RenderPipelineManager.endCameraRendering -= EndRendering;
        RestoreCamera();
    }
    // HDRPはbeginCameraRenderingより前にView/Projection行列を確定するため、
    // CinemachineのLateUpdate後かつ描画前のonBeforeRenderで補正を重ねる。
    /// <summary>Cinemachine更新後の構図を保存し、その描画だけ解除時の補間・Impulse・FOV補正を重ねる。</summary>
    private void BeforeRender()
    {
        var camera = _outputCamera;
        if (camera == null || _config == null) return;
        RestoreCamera();
        _savedPosition = camera.transform.position;
        _savedRotation = camera.transform.rotation;
        _savedFov = camera.fieldOfView;
        _renderOffsetApplied = true;
        // Brainは通常どおり復帰させ、出力する構図だけ直前のロックオン姿勢から短く繋ぐ。
        if (!_finalBlowPlaying && _lookOnCamera != null && !_lookOnCamera.IsLockOn)
        {
            float release = (Time.unscaledTime - _releaseStarted) / _config.LockOnReleaseBlendDuration;
            if (release >= 0f && release < 1f)
            {
                float blend = Mathf.SmoothStep(0f, 1f, release);
                camera.transform.position = Vector3.Lerp(_releasePosition, _savedPosition, blend);
                camera.transform.rotation = Quaternion.Slerp(_releaseRotation, _savedRotation, blend);
            }
        }
        if (!_finalBlowPlaying && _combatImpulse != null && CinemachineImpulseManager.Instance.GetImpulseAt(
            _savedPosition, false, 2, out var offset, out var rotation))
            camera.transform.position += Vector3.ClampMagnitude(offset, .15f);
        if (_finalBlowPlaying)
        {
            float push = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - _finalBlowStarted) / Mathf.Max(.01f, _finalBlowPushDuration)));
            camera.fieldOfView = Mathf.Clamp(_savedFov + _finalBlowFOVOffset * push, 1f, 179f);
        }
        else ApplyJustAvoid(camera);
        _appliedPosition = camera.transform.position;
        _appliedRotation = camera.transform.rotation;
        _appliedFov = camera.fieldOfView;
    }
    /// <summary>ジャスト回避の包絡線に応じ、Playerの注視点を軸に下へ回り込みながら寄せ、FOVを補正する。</summary>
    private void ApplyJustAvoid(Camera camera)
    {
        float now = Time.unscaledTime;
        if (_justAvoidEnvelope.Playing && IsJustAvoidSustained(now)) _justAvoidEnvelope.Sustain(now, _config.JustAvoidCameraEnter);
        float weight = _justAvoidEnvelope.Evaluate(now, _config.JustAvoidCameraEnter
            , _config.JustAvoidCameraHold, _config.JustAvoidCameraRestore);
        if (weight <= 0f) return;
        camera.fieldOfView = Mathf.Clamp(camera.fieldOfView + _config.JustAvoidFOVOffset * weight, 1f, 179f);
        if (_playerPosition == null) return;
        Vector3 pivot = _playerPosition.position + Vector3.up * _config.LookAtHeight;
        // 注視点を軸に剛体回転させるため、Playerの画面上の位置を保ったまま見上げる構図になる。
        Quaternion orbit = Quaternion.AngleAxis(-_config.JustAvoidLowAngle * weight, camera.transform.right);
        Vector3 offset = orbit * (camera.transform.position - pivot);
        float distance = offset.magnitude;
        if (distance > .01f) offset *= Mathf.Max(.3f, distance - _config.JustAvoidPushIn * weight) / distance;
        Vector3 position = pivot + offset;
        position.y = Mathf.Max(position.y, Mathf.Min(camera.transform.position.y, _playerPosition.position.y + _config.JustAvoidMinHeight));
        camera.transform.SetPositionAndRotation(position, orbit * camera.transform.rotation);
    }
    /// <summary>ジャスト回避フラグまたは追撃中で、維持の上限時間内ならtrue。</summary>
    /// <returns>カメラ演出を最大のまま維持する場合はtrue。</returns>
    private bool IsJustAvoidSustained(float now)
    {
        if (_justAvoidSustain == null || !_justAvoidSustain())
        {
            _justAvoidSustainStarted = float.NegativeInfinity;
            return false;
        }
        if (float.IsNegativeInfinity(_justAvoidSustainStarted)) _justAvoidSustainStarted = now;
        return now - _justAvoidSustainStarted < _config.JustAvoidCameraMaxSustain;
    }
    /// <summary>描画終了後に位置・回転・FOVを戻し、次のCinemachine更新に補正を持ち越さない。</summary>
    private void EndRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _outputCamera) RestoreCamera();
    }
    /// <summary>描画用に保存した位置・回転・FOVを一度だけ復元する。補正後に他から上書きされていれば新しい値を優先する。</summary>
    private void RestoreCamera()
    {
        if (!_renderOffsetApplied || _outputCamera == null) return;
        _renderOffsetApplied = false;
        var cameraTransform = _outputCamera.transform;
        // 描画されないフレームでCinemachineが新しい構図を書き込んだ場合、古い保存値で戻さない。
        if (cameraTransform.position != _appliedPosition || cameraTransform.rotation != _appliedRotation
            || !Mathf.Approximately(_outputCamera.fieldOfView, _appliedFov)) return;
        cameraTransform.SetPositionAndRotation(_savedPosition, _savedRotation);
        _outputCamera.fieldOfView = _savedFov;
    }

    /// <summary>勝利中は専用仮想カメラを進行し、それ以外はLock-Onの手動追従を更新する。</summary>
    private void LateUpdate()
    {
        if (_finalBlowPlaying) { _releaseStarted = float.NegativeInfinity; _wasLocked = false; _victoryRig?.Tick(Time.unscaledTime); return; }
        _lockOnCameraMover?.LateUpdate();
        bool locked = _lookOnCamera != null && _lookOnCamera.IsLockOn;
        if (_wasLocked && !locked)
        {
            _releaseStarted = Time.unscaledTime;
            _releasePosition = _lastLockPosition;
            _releaseRotation = _lastLockRotation;
        }
        if (locked && _outputCamera != null)
        {
            _lastLockPosition = _outputCamera.transform.position;
            _lastLockRotation = _outputCamera.transform.rotation;
        }
        _wasLocked = locked;
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
