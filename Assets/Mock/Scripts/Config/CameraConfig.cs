using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    public float LookScreenPlayerY => _lockScreenPlayerY;
    public float LookScreeEnemyY => _lockScreenPlayerY;
    public float LerpSpeed => _lerpSpeed;

    [Header("LookON Settings")]
    [SerializeField] private float _lockScreenPlayerY = 1.6f;
    [SerializeField] private float _lookScreenEnemyY = 2.5f;
    [SerializeField] private float _lerpSpeed = 6.0f;
}
