/// <summary>Enemyが選択可能な接近・待機・攻撃・後退などの行動を識別する。</summary>
public enum EnemyActionType
{
    //移動系
    Approach,
    StepBack,
    WaitWalk,
    //攻撃系
    Slash,
    Slash2,
    HeavySlash,
    HeavySlash2,
    Thrust,
    WarpAttack,
    //様子見
    Wait,
}
