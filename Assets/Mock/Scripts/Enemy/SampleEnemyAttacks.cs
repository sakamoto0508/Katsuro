using UnityEngine;

// インスペクターで簡単に設定できるよう、攻撃データのサンプル参照を保持する補助コンポーネント。
/// <summary>InspectorでEnemyAttackDataのサンプル参照一覧を保持する補助コンポーネント。攻撃の実行処理は持たない。</summary>
public class SampleEnemyAttacks　: MonoBehaviour 
{
    /// <summary>Enemy攻撃設定のサンプル参照一覧。各要素にEnemyAttackDataを割り当てる。</summary>
    [UnityEngine.Tooltip("Enemy攻撃設定のサンプル参照一覧。各要素にEnemyAttackDataを割り当てる。")]
    public EnemyAttackData[] attacks;
}
