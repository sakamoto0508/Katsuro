using UnityEngine;

// インスペクターで簡単に設定できるよう、攻撃データのサンプル参照を保持する補助コンポーネント。
/// <summary>InspectorでEnemyAttackDataのサンプル参照一覧を保持する補助コンポーネント。攻撃の実行処理は持たない。</summary>
public class SampleEnemyAttacks　: MonoBehaviour 
{
    public EnemyAttackData[] attacks;
}
