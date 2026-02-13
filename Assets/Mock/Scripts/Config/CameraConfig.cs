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

    [Header("Lockon Camera Settings")]
    [SerializeField] private float _cameraDistance = 4.5f;
    [SerializeField] private float _cameraHeight = 2.0f;
    [SerializeField] private float _positionSmooth = 10f;
    [SerializeField] private float _rotationSmooth = 12f;
    [SerializeField] private float _lookAtHeight = 1.2f;

    [Header("obstacle")]
    [SerializeField] private float _cameraCollisionRadius = 0.3f;
}
