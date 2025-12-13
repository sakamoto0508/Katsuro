using Unity.Cinemachine;
using UnityEngine;

public class CinemachineLockOnBody : CinemachineComponentBase
{
    [SerializeField, Tooltip("中心位置のオフセット")]
    private Vector3 _centerOffset;
    [SerializeField, Tooltip("タゲからみてどこにカメラを置くか")]
    private Vector3 _offset;

    private LockOnTargetGroup _targetGroup;

    /// <summary>
    /// このコンポーネントがカメラのどの要素を制御するか
    /// Bodyは位置（Position）を制御
    /// </summary>
    public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

    public override void MutateCameraState(ref CameraState curState, float deltaTime)
    {
        // フォロー対象が設定されていない場合は何もしない
        if (FollowTarget == null) return;
        // ターゲットグループを取得
        _targetGroup = FollowTarget.GetComponent<LockOnTargetGroup>();
        if (_targetGroup == null || !_targetGroup.IsValid) return;
        // 基準位置を計算（フォロー対象の位置 + オフセット）
        var basePos = FollowTargetPosition + _centerOffset;
        // 敵からプレイヤーへの方向ベクトルを計算
        var dir = _targetGroup.PlayerTarget.position - _targetGroup.EnemyTarget.position;
        // カメラの位置を計算
        var newPos = basePos + Quaternion.LookRotation(-dir) * _offset;
        // カメラの位置を更新
        curState.RawPosition = newPos;
    }

    /// <summary>
    /// カメラが現在有効な状態か
    /// </summary>
    public override bool IsValid => enabled && FollowTarget != null;
}
