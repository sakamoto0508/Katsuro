using Unity.Cinemachine;
using UnityEngine;

public class LockOnCameraMover
{
    public LockOnCameraMover(LockOnCamera lockOnCamera, Transform playerPosition
        , Transform enemyPosition, CinemachineCamera camera, CameraConfig config)
    {
        _lockOnCamera = lockOnCamera;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _cameraConfig = config;
    }

    private LockOnCamera _lockOnCamera;
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CameraConfig _cameraConfig;

    public void LateUpdate()
    {
        if (_lockOnCamera.IsLockOn == false) return;
        UpdateLockOnCamera();
    }

    private void UpdateLockOnCamera()
    {
        //敵の方向ベクトルを求める。メソッドを使わないのはy成分を0にしているため。
        Vector3 toEnemy = _enemyPosition.position - _playerPosition.position;
        toEnemy.y = 0;
        //if (toEnemy.sqrMagnitude < 0.01f)
        //{
        //    _lockOnCamera.UnLockOn();
        //    return;
        //}
        Vector3 forward = toEnemy.normalized;
        //プレイヤーの後ろにカメラを移動させる。
        Vector3 desiredPos = _playerPosition.position - forward
            * _cameraConfig.CameraDistance + Vector3.up * _cameraConfig.CameraHeight;
        //スムーズに移動させる。
        Transform camTransfrom = _camera.transform;
        camTransfrom.position = Vector3.Lerp(camTransfrom.position, desiredPos
            , Time.deltaTime * _cameraConfig.PositionSmooth);
        //カメラを敵に向ける。
        Vector3 lookDir = (_enemyPosition.position + Vector3.up * _cameraConfig.LookAtHeight)
            - camTransfrom.position;
        //スムーズに回転させる。
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
        camTransfrom.rotation = Quaternion.Slerp(camTransfrom.rotation, targetRot
            , Time.deltaTime * _cameraConfig.RotationSmooth);
    }
}
