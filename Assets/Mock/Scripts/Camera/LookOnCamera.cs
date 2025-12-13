using Unity.Cinemachine;
using UnityEngine;

public class LookOnCamera : MonoBehaviour, ICinemachineTargetGroup
{
    public Transform PlayerTarget => _playerTarget;
    public Transform EnemyTarget => _enemyTarget;

    [NoSaveDuringPlay]
    [SerializeField] private Transform _playerTarget;
    [NoSaveDuringPlay]
    [SerializeField] private Transform _enemyTarget;
    [SerializeField, Tooltip("各ターゲットの重み")]
    private float _playerWeight = 1.0f;
    [SerializeField] private float _enemyWeight = 1.0f;
    [SerializeField, Tooltip("バウンディングボックスの余白")]
    private float _boundsPadding = 1.0f;

    /// <summary>
    /// ターゲットグループが有効かどうか
    /// 両方のターゲットが設定されている場合のみtrue
    /// </summary>
    public bool IsValid => _playerTarget && _enemyTarget != null;

    /// <summary>
    /// このターゲットグループのTransform
    /// Cinemachineが基準点として使用
    /// </summary>
    public Transform Transform => this.transform;

    /// <summary>
    /// プレイヤーと敵の両方を含む軸平行境界ボックス（AABB）
    /// カメラがこの領域全体を画面に収めるために使用
    /// </summary>
    public Bounds BoundingBox
    {
        get
        {
            // ターゲットが無効な場合は、このオブジェクトの位置にサイズ0のボックスを返す
            if (!IsValid) return new Bounds(transform.position, Vector3.zero);

            // プレイヤーの位置を中心としたサイズ0のボックスから開始
            var bounds = new Bounds(_playerTarget.position, Vector3.zero);
            // 敵の位置も含むようにボックスを拡張
            bounds.Encapsulate(_enemyTarget.position);
            // 指定された余白分だけボックスを全方向に拡大
            // これにより、ターゲットが画面端ギリギリにならないようにする
            bounds.Expand(_boundsPadding);
            return bounds;
        }
    }

    /// <summary>
    /// プレイヤーと敵の両方を含む境界球
    /// BoundingBoxより計算が軽量で、カメラの距離調整に使用
    /// </summary>
    public BoundingSphere Sphere
    {
        get
        {
            // ターゲットが無効な場合は、このオブジェクトの位置に半径0の球を返す
            if (!IsValid) return new BoundingSphere(transform.position, 0);

            // 2つのターゲットの中点を球の中心とする
            var center = (_playerTarget.position + _enemyTarget.position) * 0.5f;
            // 中心から最も遠いターゲットまでの距離を半径とし、余白を加える
            var radius = Vector3.Distance(_playerTarget.position, _enemyTarget.position) * 0.5f + _boundsPadding;
            return new BoundingSphere(center, radius);
        }
    }

    /// <summary>
    /// ターゲットグループが空（無効）かどうか
    /// IsValidの逆
    /// </summary>
    public bool IsEmpty => !IsValid;

    /// <summary>
    /// カメラ視点から見たターゲットの角度範囲とZ距離範囲を計算
    /// Cinemachineがカメラの視野角（FOV）を動的に調整するために使用
    /// </summary>
    public void GetViewSpaceAngularBounds(Matrix4x4 observer, out Vector2 minAngles, out Vector2 maxAngles, out Vector2 zRange)
    {
        // ターゲットが無効な場合は全て0を返す
        if (!IsValid)
        {
            minAngles = maxAngles = Vector2.zero;
            zRange = Vector2.zero;
            return;
        }
        // ワールド座標をカメラ空間（ビュー空間）に変換
        // カメラを原点とし、Z軸がカメラの前方向となる座標系
        var p1 = observer.MultiplyPoint3x4(_playerTarget.position);
        var p2 = observer.MultiplyPoint3x4(_enemyTarget.position);
        // カメラから見た各ターゲットの角度を計算
        minAngles = new Vector2(
            Mathf.Min(Mathf.Atan2(p1.x, p1.z), Mathf.Atan2(p2.x, p2.z)) * Mathf.Rad2Deg,
            Mathf.Min(Mathf.Atan2(p1.y, p1.z), Mathf.Atan2(p2.y, p2.z)) * Mathf.Rad2Deg
        );
        maxAngles = new Vector2(
            Mathf.Max(Mathf.Atan2(p1.x, p1.z), Mathf.Atan2(p2.x, p2.z)) * Mathf.Rad2Deg,
            Mathf.Max(Mathf.Atan2(p1.y, p1.z), Mathf.Atan2(p2.y, p2.z)) * Mathf.Rad2Deg
        );

        zRange = new Vector2(Mathf.Min(p1.z, p2.z), Mathf.Max(p1.z, p2.z));
    }

    /// <summary>
    /// カメラ空間での境界ボックスを取得
    /// カメラのフラスタムカリング（視錐台の外側のオブジェクトを除外）に使用
    /// </summary>
    public Bounds GetViewSpaceBoundingBox(Matrix4x4 observer, bool includeBehind)
    {
        // ターゲットが無効な場合は空のボックスを返す
        if (!IsValid) return new Bounds(Vector3.zero, Vector3.zero);
        // ワールド座標をカメラ空間に変換
        var p1 = observer.MultiplyPoint3x4(_playerTarget.position);
        var p2 = observer.MultiplyPoint3x4(_enemyTarget.position);
        // カメラの後ろにあるターゲットを除外するオプション
        // z < 0 はカメラの後ろにあることを意味する
        if (!includeBehind && p1.z < 0 && p2.z < 0)
            return new Bounds(Vector3.zero, Vector3.zero);
        // カメラ空間で境界ボックスを作成
        var bounds = new Bounds(p1, Vector3.zero);
        bounds.Encapsulate(p2);
        bounds.Expand(_boundsPadding);
        return bounds;
    }
}