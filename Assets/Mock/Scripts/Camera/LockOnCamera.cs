using Unity.Cinemachine;
using UnityEngine;

// ロックオン状態を管理するユーティリティクラス
// - プレイヤーとターゲット（敵）の Transform を受け取り、ロックオン状態の管理を行う
// - Cinemachine の VirtualCamera 優先度制御や、必要に応じて CinemachineBrain を無効化して
//   手動でカメラの Transform を制御するためのラッパです
public class LockOnCamera
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="playerPosition">プレイヤーの Transform（参照）</param>
    /// <param name="enemyPosition">初期ターゲットの Transform（参照）</param>
    /// <param name="camera">通常時に使う CinemachineCamera（参照）</param>
    /// <param name="lockOnCamera">ロックオン用の CinemachineCamera（参照）</param>
    /// <param name="animController">プレイヤーのアニメーション制御ラッパ</param>
    /// <param name="animationName">アニメーション名定義</param>
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

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            _cinemachineBrain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        }
    }

    /// <summary>現在ロックオン中かどうか</summary>
    public bool IsLockOn { get; private set; }

    // --- 参照項目 ---
    /// <summary>プレイヤーの Transform（参照保持）</summary>
    private Transform _playerPosition;
    /// <summary>現在のロック対象（敵）の Transform（参照保持）</summary>
    private Transform _enemyPosition;
    /// <summary>通常用の CinemachineCamera（参照）</summary>
    private CinemachineCamera _camera;
    /// <summary>ロックオン時に切り替える CinemachineCamera（参照）</summary>
    private CinemachineCamera _lockOnCamera;
    /// <summary>アニメーション制御ラッパ</summary>
    private PlayerAnimationController _animController;
    /// <summary>アニメーション名定義</summary>
    private AnimationName _animationName;

    // --- Cinemachine 制御用 ---
    /// <summary>Main Camera の CinemachineBrain（あれば取得）</summary>
    private Unity.Cinemachine.CinemachineBrain _cinemachineBrain;
    /// <summary>LockOn により一時的に Brain を無効化したかのフラグ</summary>
    private bool _brainDisabledByLockOn = false;

    /// <summary>
    /// ロック対象を明示的に設定します。
    /// </summary>
    public void SetTarget(Transform t)
    {
        _enemyPosition = t;
    }

    /// <summary>
    /// 現在有効なロック対象が設定されているかを返します。
    /// </summary>
    public bool HasValidTarget()
    {
        return _enemyPosition != null && _enemyPosition.gameObject.activeInHierarchy;
    }

    public void LockOn()
    {
        IsLockOn = true;
        // ロックオン開始：VirtualCamera の優先度を切り替え、必要なら CinemachineBrain を無効化して
        // 手動でカメラ制御を行えるようにします。

        if (_camera != null && _lockOnCamera != null)
        {
            _camera.Priority = 0;
            _lockOnCamera.Priority = 10;
        }
        if (_cinemachineBrain != null && _cinemachineBrain.enabled)
        {
            _cinemachineBrain.enabled = false;
            _brainDisabledByLockOn = true;
        }
        if (_animController != null && _animationName != null)
        {
            _animController.PlayBool(_animationName.IsLockOn, IsLockOn);
        }
    }

    public void UnLockOn()
    {
        if (!IsLockOn) return;
        // ロック解除：優先度を元に戻し、Brain を再有効化します（無効化した場合）。

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

        // プレイヤーからターゲットへの水平（Y成分無し）方向ベクトルを返します。
        Vector3 direction = _enemyPosition.position - _playerPosition.position;
        direction.y = 0;
        var dir = direction.normalized;
        return dir;
    }
}
