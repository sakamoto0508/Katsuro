using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CameraManager : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    private LockOnCamera _lookOnCamera;
    private LockOnCameraMover _lockOnCameraMover;
    [SerializeField] private CinemachineImpulseSource _combatImpulse;
    private Camera _outputCamera;
    public Camera OutputCamera => _outputCamera;
    private CameraConfig _config;
    private float _fovStarted = float.NegativeInfinity;
    [Header("Final Blow")]
    [SerializeField, Range(-6f, -3f)] private float _finalBlowFOVOffset = -4f;
    [SerializeField, Range(.3f, .6f)] private float _finalBlowPushDuration = .42f;
    private bool _finalBlowPlaying;
    private float _finalBlowStarted;
    private Vector3 _savedPosition;
    private float _savedFov;
    private bool _renderOffsetApplied;
    private bool _subscribed;
    private bool _initialized;
    private VictoryCameraRig _victoryRig;
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

    private void OnDestroy()
    {
        UnsubscribeRendering();
        _victoryRig?.Stop();
        if (_inputBuffer != null)
        {
            InputEventUnRegistry(_inputBuffer);
        }
    }

    public void PlayHitFeedback(bool heavy, Vector3 contactPoint)
    {
        if (!_initialized || !isActiveAndEnabled || _combatImpulse == null || _config == null) return;
        float strength = heavy ? _config.HeavyHitCameraStrength : _config.LightHitCameraStrength;
        _combatImpulse.ImpulseDefinition.ImpulseDuration = _config.CameraDuration;
        _combatImpulse.GenerateImpulseAtPositionWithVelocity(contactPoint, Vector3.down * strength);
    }
    public void PlayJustAvoidFeedback()
    {
        if (_initialized && isActiveAndEnabled && !_finalBlowPlaying) _fovStarted = Time.unscaledTime;
    }
    public void PlayFinalBlowFeedback(float delay = 0f)
    {
        if (!_initialized || !isActiveAndEnabled) return;
        _fovStarted = float.NegativeInfinity;
        _finalBlowStarted = Time.unscaledTime + Mathf.Max(0f, delay);
        _finalBlowPlaying = true;
        _victoryRig?.Begin(delay);
    }
    public void StopFinalBlowFeedback() { _finalBlowPlaying = false; RestoreCamera(); _victoryRig?.Stop(); }
    private void OnEnable() { if (_initialized) SubscribeRendering(); }
    private void OnDisable() { UnsubscribeRendering(); _fovStarted = float.NegativeInfinity; StopFinalBlowFeedback(); }
    private void SubscribeRendering()
    {
        if (_subscribed) return;
        _subscribed = true;
        RenderPipelineManager.beginCameraRendering += BeginRendering;
        RenderPipelineManager.endCameraRendering += EndRendering;
    }
    private void UnsubscribeRendering()
    {
        if (!_subscribed) return;
        _subscribed = false;
        RenderPipelineManager.beginCameraRendering -= BeginRendering;
        RenderPipelineManager.endCameraRendering -= EndRendering;
        RestoreCamera();
    }
    // Cinemachineと手動ロックオンの更新後、描画する瞬間だけ小さな補正を重ねる。
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
    private void EndRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _outputCamera) RestoreCamera();
    }
    private void RestoreCamera()
    {
        if (!_renderOffsetApplied || _outputCamera == null) return;
        _outputCamera.transform.position = _savedPosition;
        _outputCamera.fieldOfView = _savedFov;
        _renderOffsetApplied = false;
    }

    private void LateUpdate()
    {
        if (_finalBlowPlaying) { _victoryRig?.Tick(Time.unscaledTime); return; }
        _lockOnCameraMover?.LateUpdate();
    }

    private void InputEventRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.LookOnAction.started += OnLookOnAction;
    }

    private void InputEventUnRegistry(InputBuffer inputBuffer)
    {
        inputBuffer.LookOnAction.started -= OnLookOnAction;
    }

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
