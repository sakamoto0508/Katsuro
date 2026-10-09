/// <summary>Enemyの待機・追跡・攻撃などの行動状態を識別する。</summary>
public enum EnemyState
{
    Idle,
    Chase,
    Observe,
    Backstep,
    Attack,
    Recovery,
    Stagger,
    Dead,
}
