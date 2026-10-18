/// <summary>Enemyが選択可能な接近・待機・攻撃・後退などの行動を識別する。</summary>
public enum EnemyActionType
{
    //移動系
    Approach = 0,
    StepBack = 1,
    WaitWalk = 2,
    //攻撃系
    Slash = 3,
    Slash2 = 4,
    HeavySlash = 5,
    HeavySlash2 = 6,
    Thrust = 7,
    WarpAttack = 8,
    //様子見
    Wait = 9,
}
