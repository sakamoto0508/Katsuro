using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    public Transform PlayerPosition => _playerPosition;
    public Transform EnemyPosition => _enemyPosition;

    [SerializeField] private Transform _playerPosition;
    [SerializeField] protected Transform _enemyPosition;
}
