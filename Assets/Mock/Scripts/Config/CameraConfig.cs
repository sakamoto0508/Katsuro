using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    public float LookScreenX => _lockScreenX;
    public float LookScreenY => _lockScreenY;
    public float LerpSpeed => _lerpSpeed;

    [Header("LookON Settings")]
    [SerializeField] private float _lockScreenX = 0.55f;
    [SerializeField] private float _lockScreenY = 0.50f;
    [SerializeField] private float _lerpSpeed = 6.0f;
}
