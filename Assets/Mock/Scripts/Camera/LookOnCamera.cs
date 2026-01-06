using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera
{
    public LookOnCamera(Transform playerPosition, Transform enemyPosition
        , CinemachineCamera camera, CameraConfig config)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _cameraConfig = config;
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CameraConfig _cameraConfig;

    public void Update()
    {
        if (!IsLockOn) return;

        Vector3 toEnemy = _enemyPosition.position - _playerPosition.position;
        toEnemy.y = 0;
        if (toEnemy.sqrMagnitude < 0.01f)
        {
            UnLockOn();
            return;
        }
        Vector3 forward = toEnemy.normalized;
        //プレイヤーの後ろにカメラを移動させる
        Vector3 desiredPos = _playerPosition.position - forward
            * _cameraConfig.CameraDistance + Vector3.up * _cameraConfig.CameraHeight;
        //スムーズに移動させる
        Transform camTransfrom = _camera.transform;
        camTransfrom.position = Vector3.Lerp(camTransfrom.position, desiredPos
            , Time.deltaTime * _cameraConfig.PositionSmooth);
        //カメラを敵に向ける
        Vector3 lookDir = (_enemyPosition.position + Vector3.up * _cameraConfig.LookAtHeight)
            - camTransfrom.position;
        //スムーズに回転させる
        Quaternion targetRot =Quaternion.LookRotation(lookDir.normalized);
        camTransfrom.rotation = Quaternion.Slerp(camTransfrom.rotation, targetRot
            , Time.deltaTime * _cameraConfig.RotationSmooth);
    }

    public void LockOn()
    {
        IsLockOn = true;
        // Cinemachine による制御を止める
        _camera.enabled = false;
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;

        IsLockOn = false;
        // Cinemachine を戻す
        _camera.enabled = true;
    }

    /// <summary>
    /// プレイヤー用：敵を見る方向（移動・回転用）
    /// </summary>
    /// <returns></returns>
    public Vector3 ReturnLockOnDirection()
    {
        if (!IsLockOn) return Vector3.zero;

        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        return direction.normalized;
    }
}
