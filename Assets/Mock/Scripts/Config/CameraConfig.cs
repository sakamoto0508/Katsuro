using UnityEngine;

/// <summary>通常追従・Lock-On・命中・ジャスト回避のカメラ距離と補正量をまとめる設定。</summary>
[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    /// <summary>追従カメラとPlayerの基準水平距離（Unity単位）。</summary>
    public float CameraDistance => _cameraDistance;
    /// <summary>Player位置からの追従カメラの基準高さ（Unity単位）。</summary>
    public float CameraHeight => _cameraHeight;
    /// <summary>カメラ位置の追従補間係数。大きいほど目標位置へ速く追従する。</summary>
    public float PositionSmooth => _positionSmooth;
    /// <summary>カメラ回転の追従補間係数。大きいほど目標方向へ速く追従する。</summary>
    public float RotationSmooth => _rotationSmooth;
    /// <summary>Player位置から注視点までの高さ（Unity単位）。</summary>
    public float LookAtHeight => _lookAtHeight;
    /// <summary>カメラ障害物判定のSphereCast半径（Unity単位）。</summary>
    public float CameraCollisionRadius => _cameraCollisionRadius;

    /// <summary>Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    [UnityEngine.Tooltip("Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。")]
    [Header("戦闘フィードバック")]
    [SerializeField, Range(0f, .15f)] private float _lightHitCameraStrength = .025f;
    /// <summary>Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    [UnityEngine.Tooltip("Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。")]
    [SerializeField, Range(0f, .15f)] private float _heavyHitCameraStrength = .055f;
    /// <summary>命中時カメラ衝撃の継続時間（秒）。</summary>
    [UnityEngine.Tooltip("命中時カメラ衝撃の継続時間（秒）。")]
    [SerializeField, Min(.01f)] private float _cameraDuration = .12f;
    /// <summary>Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。")]
    [SerializeField, Range(-3f, 3f)] private float _justAvoidFOVOffset = 1.5f;
    /// <summary>Just Avoidの画角演出を往復させる時間（秒）。</summary>
    [UnityEngine.Tooltip("Just Avoidの画角演出を往復させる時間（秒）。")]
    [SerializeField, Min(.01f)] private float _justAvoidFOVDuration = .18f;
    /// <summary>Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    public float LightHitCameraStrength => _lightHitCameraStrength;
    /// <summary>Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    public float HeavyHitCameraStrength => _heavyHitCameraStrength;
    /// <summary>命中時カメラ衝撃の継続時間（秒）。</summary>
    public float CameraDuration => _cameraDuration;
    /// <summary>Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。</summary>
    public float JustAvoidFOVOffset => _justAvoidFOVOffset;
    /// <summary>Just Avoidの画角演出を往復させる時間（秒）。</summary>
    public float JustAvoidFOVDuration => _justAvoidFOVDuration;

    /// <summary>追従カメラとPlayerの基準水平距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("追従カメラとPlayerの基準水平距離（Unity単位）。")]
    [Header("Lockon Camera Settings")]
    [SerializeField] private float _cameraDistance = 4.5f;
    /// <summary>Player位置からの追従カメラの基準高さ（Unity単位）。</summary>
    [UnityEngine.Tooltip("Player位置からの追従カメラの基準高さ（Unity単位）。")]
    [SerializeField] private float _cameraHeight = 2.0f;
    /// <summary>カメラ位置の追従補間係数。大きいほど目標位置へ速く追従する。</summary>
    [UnityEngine.Tooltip("カメラ位置の追従補間係数。大きいほど目標位置へ速く追従する。")]
    [SerializeField] private float _positionSmooth = 10f;
    /// <summary>カメラ回転の追従補間係数。大きいほど目標方向へ速く追従する。</summary>
    [UnityEngine.Tooltip("カメラ回転の追従補間係数。大きいほど目標方向へ速く追従する。")]
    [SerializeField] private float _rotationSmooth = 12f;
    /// <summary>Player位置から注視点までの高さ（Unity単位）。</summary>
    [UnityEngine.Tooltip("Player位置から注視点までの高さ（Unity単位）。")]
    [SerializeField] private float _lookAtHeight = 1.2f;

    /// <summary>カメラ障害物判定のSphereCast半径（Unity単位）。</summary>
    [UnityEngine.Tooltip("カメラ障害物判定のSphereCast半径（Unity単位）。")]
    [Header("obstacle")]
    [SerializeField] private float _cameraCollisionRadius = 0.3f;
}
