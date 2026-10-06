using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    public float CameraDistance => _cameraDistance;
    public float CameraHeight => _cameraHeight;
    public float PositionSmooth => _positionSmooth;
    public float RotationSmooth => _rotationSmooth;
    public float LookAtHeight => _lookAtHeight;
    public float CameraCollisionRadius => _cameraCollisionRadius;

    [Header("戦闘フィードバック")]
    [SerializeField, Range(0f, .15f)] private float _lightHitCameraStrength = .025f;
    [SerializeField, Range(0f, .15f)] private float _heavyHitCameraStrength = .055f;
    [SerializeField, Min(.01f)] private float _cameraDuration = .12f;
    [SerializeField, Range(-3f, 3f)] private float _justAvoidFOVOffset = 1.5f;
    [SerializeField, Min(.01f)] private float _justAvoidFOVDuration = .18f;
    public float LightHitCameraStrength => _lightHitCameraStrength;
    public float HeavyHitCameraStrength => _heavyHitCameraStrength;
    public float CameraDuration => _cameraDuration;
    public float JustAvoidFOVOffset => _justAvoidFOVOffset;
    public float JustAvoidFOVDuration => _justAvoidFOVDuration;

    [Header("Lockon Camera Settings")]
    [SerializeField] private float _cameraDistance = 4.5f;
    [SerializeField] private float _cameraHeight = 2.0f;
    [SerializeField] private float _positionSmooth = 10f;
    [SerializeField] private float _rotationSmooth = 12f;
    [SerializeField] private float _lookAtHeight = 1.2f;

    [Header("obstacle")]
    [SerializeField] private float _cameraCollisionRadius = 0.3f;
}
