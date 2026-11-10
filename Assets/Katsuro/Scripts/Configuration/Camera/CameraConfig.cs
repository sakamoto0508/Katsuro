using UnityEngine;

/// <summary>通常追従・Lock-On・命中・ジャスト回避のカメラ距離と補正量をまとめる設定。</summary>
[CreateAssetMenu(fileName = "CameraConfig", menuName = "ScriptableObjects/Camera/CameraConfig", order = 1)]
public class CameraConfig : ScriptableObject
{
    /// <summary>追撃1撃目の命中時にカメラへ与えるインパルスの強さ。</summary>
    [Header("Just Avoid Counter / First contact")]
    [SerializeField, Range(0f, .15f)] private float _counterFirstCameraStrength = .018f;
    /// <summary>追撃1撃目の命中時のカメラ演出の持続時間（秒）。</summary>
    [SerializeField, Min(.01f)] private float _counterFirstCameraDuration = .055f;
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

    /// <summary>ロックオン構図を横へずらす距離。ゼロで従来の中央構図。</summary>
    public float LockOnSideOffset => _lockOnSideOffset;
    /// <summary>敵を優先する注視割合。1で敵の上半身を直接注視する。</summary>
    public float LockOnTargetWeight => Mathf.Clamp(_lockOnTargetWeight, .75f, 1f);
    /// <summary>環境とのカメラ衝突判定に使用するレイヤー。</summary>
    public LayerMask LockOnObstacleMask => _lockOnObstacleMask;
    /// <summary>ロックオン時の横方向の距離。画角の急変を避けるため位置補間を通して適用する。</summary>
    [SerializeField, Range(-2f, 2f), Tooltip("ロックオン構図の横オフセット（m）。ゼロで中央。")]
    private float _lockOnSideOffset;
    /// <summary>敵の予備動作を優先する注視割合。カメラ位置はPlayer基準のまま維持する。</summary>
    [SerializeField, Range(.75f, 1f), Tooltip("敵への注視割合。1で敵を直接注視。0.75でも敵寄りを維持します。")]
    private float _lockOnTargetWeight = 1f;
    /// <summary>ロックオン解除時に現在の構図から通常カメラへ戻す描画上の補間時間。</summary>
    [SerializeField, Range(.1f, .4f), Tooltip("ロックオン解除時の構図復帰時間（実時間秒）。")]
    private float _lockOnReleaseBlendDuration = .22f;
    /// <summary>通常カメラへ復帰する構図の補間時間。</summary>
    public float LockOnReleaseBlendDuration => Mathf.Max(.01f, _lockOnReleaseBlendDuration);
    /// <summary>カメラを遮る環境レイヤー。キャラクター自身とTriggerは判定から除外する。</summary>
    [SerializeField, Tooltip("ロックオンカメラの障害物レイヤー。Player・Enemy階層は自動除外。")]
    private LayerMask _lockOnObstacleMask = Physics.DefaultRaycastLayers;

    /// <summary>Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    [UnityEngine.Tooltip("Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。")]
    [Header("戦闘フィードバック")]
    [SerializeField, Range(0f, .15f)] private float _lightHitCameraStrength = .025f;
    /// <summary>Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    [UnityEngine.Tooltip("Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。")]
    [SerializeField, Range(0f, .15f)] private float _heavyHitCameraStrength = .055f;
    /// <summary>Just Avoid追撃のカメラ衝撃。通常Heavyと別に設定する。</summary>
    [SerializeField, Range(0f, .15f), Tooltip("追撃命中のImpulse強度。追従位置や注視点は変更しません。")]
    private float _counterHitCameraStrength = .075f;
    /// <summary>命中衝撃の継続時間。X=Light、Y=Heavy、Z=追撃。</summary>
    [SerializeField, Tooltip("Camera Impulse時間（秒）。X=Light、Y=Heavy、Z=追撃。")]
    private Vector3 _hitCameraDurations = new Vector3(.065f, .095f, .12f);
    /// <summary>命中種別のカメラ衝撃の強さを取得する。</summary>
    public float GetHitCameraStrength(bool heavy, bool counter, bool counterFirst = false) => counter && counterFirst ? _counterFirstCameraStrength : counter ? _counterHitCameraStrength : heavy ? _heavyHitCameraStrength : _lightHitCameraStrength;
    /// <summary>命中種別の短い衝撃時間を取得する。</summary>
    public float GetHitCameraDuration(bool heavy, bool counter, bool counterFirst = false) => counter && counterFirst ? _counterFirstCameraDuration : Mathf.Max(.01f, _hitCameraDurations[counter ? 2 : heavy ? 1 : 0]);
    /// <summary>命中時カメラ衝撃の継続時間（秒）。</summary>
    [UnityEngine.Tooltip("命中時カメラ衝撃の継続時間（秒）。")]
    [SerializeField, Min(.01f)] private float _cameraDuration = .12f;
    /// <summary>Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時に加える画角の差（度）。負数で画角を狭めて寄り、正数で広くなる。")]
    [Header("Just Avoid カメラ演出")]
    [SerializeField, Range(-20f, 20f)] private float _justAvoidFOVOffset = -4f;
    /// <summary>Just Avoid成功時にPlayerの注視点へカメラを近づける距離（Unity単位）。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時にPlayerの注視点へカメラを近づける距離（Unity単位）。0で寄せない。")]
    [SerializeField, Range(0f, 3f)] private float _justAvoidPushIn = .4f;
    /// <summary>Just Avoid成功時にPlayerの注視点を軸にカメラを下へ回り込ませる角度（度）。</summary>
    [UnityEngine.Tooltip("Just Avoid成功時にPlayerの注視点を軸にカメラを下へ回り込ませる角度（度）。大きいほど下から見上げる。負数で上から見下ろす。")]
    [SerializeField, Range(-30f, 45f)] private float _justAvoidLowAngle = 8f;
    /// <summary>ローアングル時にPlayerの足元から最低限確保するカメラの高さ（Unity単位）。</summary>
    [UnityEngine.Tooltip("ローアングル時にPlayerの足元から最低限確保するカメラの高さ（Unity単位）。地面へのめり込みを防ぐ。")]
    [SerializeField, Min(0f)] private float _justAvoidMinHeight = .25f;
    /// <summary>Just Avoidのカメラ演出が最大へ達するまでの時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Just Avoidのカメラ演出が最大へ達するまでの時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _justAvoidCameraEnter = .12f;
    /// <summary>Just Avoidのカメラ演出を最大のまま保持する時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Just Avoidのカメラ演出を最大のまま保持する時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _justAvoidCameraHold = .35f;
    /// <summary>Just Avoidのカメラ演出から通常構図へ戻る時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("Just Avoidのカメラ演出から通常構図へ戻る時間（実時間の秒）。")]
    [SerializeField, Min(0f)] private float _justAvoidCameraRestore = .3f;
    /// <summary>ジャスト回避フラグや追撃中にカメラ演出を最大のまま維持できる上限時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("ジャスト回避フラグがtrue、または追撃攻撃中はカメラ演出を最大のまま維持する。その維持の上限時間（実時間の秒）。フラグの解除漏れ対策。")]
    [SerializeField, Min(0f)] private float _justAvoidCameraMaxSustain = 3f;
    /// <summary>Light命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    public float LightHitCameraStrength => _lightHitCameraStrength;
    /// <summary>Heavy命中時のカメラ衝撃の強さ。大きいほど揺れが強くなる。</summary>
    public float HeavyHitCameraStrength => _heavyHitCameraStrength;
    /// <summary>命中時カメラ衝撃の継続時間（秒）。</summary>
    public float CameraDuration => _cameraDuration;
    /// <summary>Just Avoid成功時に加える画角の差（度）。正数で広く、負数で狭くなる。</summary>
    public float JustAvoidFOVOffset => _justAvoidFOVOffset;
    /// <summary>Just Avoid成功時にPlayerの注視点へカメラを近づける距離（Unity単位）。</summary>
    public float JustAvoidPushIn => _justAvoidPushIn;
    /// <summary>Just Avoid成功時にカメラを下へ回り込ませる角度（度）。</summary>
    public float JustAvoidLowAngle => _justAvoidLowAngle;
    /// <summary>ローアングル時にPlayerの足元から確保するカメラの最低高さ（Unity単位）。</summary>
    public float JustAvoidMinHeight => _justAvoidMinHeight;
    /// <summary>Just Avoidのカメラ演出が最大へ達するまでの時間（実時間の秒）。</summary>
    public float JustAvoidCameraEnter => _justAvoidCameraEnter;
    /// <summary>Just Avoidのカメラ演出を最大のまま保持する時間（実時間の秒）。</summary>
    public float JustAvoidCameraHold => _justAvoidCameraHold;
    /// <summary>Just Avoidのカメラ演出から通常構図へ戻る時間（実時間の秒）。</summary>
    public float JustAvoidCameraRestore => _justAvoidCameraRestore;
    /// <summary>ジャスト回避フラグや追撃中にカメラ演出を最大のまま維持できる上限時間（実時間の秒）。</summary>
    public float JustAvoidCameraMaxSustain => _justAvoidCameraMaxSustain;

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
