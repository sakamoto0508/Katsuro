/// <summary>Playerの移動・攻撃・Dash・能力状態をステートマシン内で識別する。</summary>
public enum PlayerStateId
{
    Locomotion,
    Dash,
    LightAttack,
    StrongAttack,
    Ghost,
    SelfSacrifice,
    Heal,
}
