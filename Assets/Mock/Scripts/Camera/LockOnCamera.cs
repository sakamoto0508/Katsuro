using Unity.Cinemachine;
using UnityEditor.PackageManager;
using UnityEngine;

public class LockOnCamera
{
    public LockOnCamera(Transform playerPosition, Transform enemyPosition, CinemachineCamera camera
        , CinemachineCamera lockOnCamera, PlayerAnimationController animController, AnimationName animationName)
    {
        IsLockOn = false;
        _playerPosition = playerPosition;
        _enemyPosition = enemyPosition;
        _camera = camera;
        _lockOnCamera = lockOnCamera;
        _animController = animController;
        _animationName = animationName;

        Debug.Log($"LockOnCamera: initialized. player={_playerPosition?.name ?? "null"}, enemy={_enemyPosition?.name ?? "null"}, camera={_camera?.name ?? "null"}, lockOnCam={_lockOnCamera?.name ?? "null"}");

        // try to find the CinemachineBrain on the main camera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            _cinemachineBrain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            Debug.Log($"LockOnCamera: found CinemachineBrain = {(_cinemachineBrain != null ? "yes" : "no")}");
        }
    }

    public bool IsLockOn { get; private set; }
    private Transform _playerPosition;
    private Transform _enemyPosition;
    private CinemachineCamera _camera;
    private CinemachineCamera _lockOnCamera;
    private PlayerAnimationController _animController;
    private AnimationName _animationName;
    private Unity.Cinemachine.CinemachineBrain _cinemachineBrain;
    private bool _brainDisabledByLockOn = false;

    public void SetTarget(Transform t)
    {
        _enemyPosition = t;
        Debug.Log($"LockOnCamera: SetTarget -> {t?.name ?? "null"}");
    }

    public bool HasValidTarget()
    {
        bool ok = _enemyPosition != null && _enemyPosition.gameObject.activeInHierarchy;
        Debug.Log($"LockOnCamera: HasValidTarget -> {ok} (target={_enemyPosition?.name ?? "null"})");
        return ok;
    }

    public void LockOn()
    {
        IsLockOn = true;
        Debug.Log($"LockOnCamera: LockOn called. target={_enemyPosition?.name ?? "null"}");

        if (_camera != null && _lockOnCamera != null)
        {
            _camera.Priority = 0;
            _lockOnCamera.Priority = 10;
        }
        // disable CinemachineBrain so we can control main camera transform directly
        if (_cinemachineBrain != null && _cinemachineBrain.enabled)
        {
            _cinemachineBrain.enabled = false;
            _brainDisabledByLockOn = true;
            Debug.Log("LockOnCamera: CinemachineBrain disabled for manual camera control.");
        }
        if (_animController != null && _animationName != null)
        {
            _animController.PlayBool(_animationName.IsLockOn, IsLockOn);
        }
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;
        Debug.Log("LockOnCamera: UnLockOn called.");

        IsLockOn = false;
        if (_camera != null && _lockOnCamera != null)
        {
            _camera.Priority = 10;
            _lockOnCamera.Priority = 0;
        }
        // re-enable brain if we disabled it
        if (_cinemachineBrain != null && _brainDisabledByLockOn)
        {
            _cinemachineBrain.enabled = true;
            _brainDisabledByLockOn = false;
            Debug.Log("LockOnCamera: CinemachineBrain re-enabled.");
        }
        if (_animController != null && _animationName != null)
        {
            _animController.PlayBool(_animationName.IsLockOn, IsLockOn);
        }
    }

    /// <summary>
    /// プレイヤー用：敵を見る方向（移動・回転用）
    /// </summary>
    /// <returns></returns>
    public Vector3 ReturnLockOnDirection()
    {
        if (!IsLockOn) return Vector3.zero;
        if (_enemyPosition == null || _playerPosition == null)
        {
            Debug.LogWarning("LockOnCamera: ReturnLockOnDirection - missing positions");
            return Vector3.zero;
        }

        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        var dir = direction.normalized;
        Debug.Log($"LockOnCamera: ReturnLockOnDirection target={_enemyPosition.name} targetPos={_enemyPosition.position} playerPos={_playerPosition.position} dir={dir}");
        return dir;
    }
}
