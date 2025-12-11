using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera : MonoBehaviour
{
    public bool IsLookOn { get; private set; }
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineRotationComposer _composer;
    [SerializeField] private Transform _playerPosition;
    [SerializeField] private Transform _enemyPosition;
    [SerializeField] private CameraConfig _cameraConfig;
    private float _targetOffsetY;


    [ContextMenu("LookOn")]
    public void OnLockOn()
    {
        if (!IsLookOn)
        {
            IsLookOn = true;
            _camera.Target.TrackingTarget = _enemyPosition;
            _composer.TargetOffset.y = _cameraConfig.LookScreeEnemyY;
        }
        else
        {
            IsLookOn = false;
            _camera.Target.TrackingTarget = _playerPosition;
            _composer.TargetOffset.y= _cameraConfig.LookScreeEnemyY;
        }
    }

    private void Start()
    {
        if (_camera.Target.TrackingTarget == null)
        {
            _camera.Target.TrackingTarget = _playerPosition;
        }
        _composer.TargetOffset.y = _cameraConfig.LookScreenPlayerY;
    }
}
