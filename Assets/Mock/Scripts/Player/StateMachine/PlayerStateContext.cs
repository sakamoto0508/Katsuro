/// <summary>
/// プレイヤーステート間で共有する依存オブジェクトや状態をまとめたコンテキスト。
/// 各ステートはこのコンテキスト経由でコントローラーや移動系コンポーネントへアクセスする。
/// </summary>
public sealed class PlayerStateContext
{
    public PlayerStateContext(PlayerController controller, PlayerStatus status,
        PlayerMover mover, PlayerSprint sprint, LockOnCamera lockOnCamera)
    {
        Controller = controller;
        Status = status;
        Mover = mover;
        Sprint = sprint;
        LockOnCamera = lockOnCamera;
    }

    /// <summary>プレイヤー本体のコントローラー。</summary>
    public PlayerController Controller { get; }

    /// <summary>プレイヤーのステータス（移動速度や加速度など）。</summary>
    public PlayerStatus Status { get; }

    /// <summary>移動・回転計算を担うコンポーネント。</summary>
    public PlayerMover Mover { get; }

    /// <summary>スキルゲージを消費・管理するスプリント制御。</summary>
    public PlayerSprint Sprint { get; }

    /// <summary>ロックオン状態の参照先。</summary>
    public LockOnCamera LockOnCamera { get; }

    /// <summary>現在ロックオン中かどうかを示すショートカット。</summary>
    public bool IsLockOn => LockOnCamera != null && LockOnCamera.IsLockOn;
}