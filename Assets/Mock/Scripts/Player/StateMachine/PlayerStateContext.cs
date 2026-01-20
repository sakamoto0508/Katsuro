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

    public PlayerController Controller { get; }
    public PlayerStatus Status { get; }
    public PlayerMover Mover { get; }
    public PlayerSprint Sprint { get; }
    public LockOnCamera LockOnCamera { get; }

    public bool IsLockOn => LockOnCamera != null && LockOnCamera.IsLockOn;
}