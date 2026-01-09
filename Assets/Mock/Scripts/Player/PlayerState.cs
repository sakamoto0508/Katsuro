using UnityEngine;

public enum PlayerStateType
{
    Idle = 0,
    LockOn = 1 << 0,
    Sprint = 1 << 1,
    Ghost = 1 << 2,
    JustAvoid = 1 << 3,
}

public enum PlayerLifeState
{
    Alive,
    DamageTaking,
    Down,
    Dead
}

public class PlayerState : MonoBehaviour
{

}
