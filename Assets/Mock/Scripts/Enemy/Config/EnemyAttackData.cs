using UnityEngine;

/// <summary>Enemy行動ごとのAnimator名、攻撃力・武器などをInspectorで定義する。</summary>
[CreateAssetMenu(menuName = "Enemy/AttackData", fileName = "EnemyAttackData")]
public class EnemyAttackData : ScriptableObject
{
    /// <summary>この攻撃設定を使うEnemy行動の種類。</summary>
    public EnemyActionType ActionType => _actionType;
    /// <summary>この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。</summary>
    public string AnimatorTrigger => _animatorTrigger;
    /// <summary>この行動の基準ダメージ。攻撃開始時に武器へ渡す。</summary>
    public float Damage => _damage;
    /// <summary>行動の想定射程（Unity単位）。現在の攻撃実行処理では直接参照しない設定。</summary>
    public float Range => _range;
    /// <summary>有効にする武器Colliderの番号。現在の攻撃実行処理では直接参照しない設定。</summary>
    public int HitboxIndex => _hitboxIndex;
    /// <summary>Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    public float HitboxEnableDelay => _hitboxEnableDelay;
    /// <summary>Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    public float HitboxDisableDelay => _hitboxDisableDelay;

    /// <summary>この攻撃設定を使うEnemy行動の種類。</summary>
    [UnityEngine.Tooltip("この攻撃設定を使うEnemy行動の種類。")]
    [SerializeField] private EnemyActionType _actionType;
    /// <summary>この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。</summary>
    [UnityEngine.Tooltip("この行動を開始するAnimator Trigger名。Controllerのパラメータと一致させる。")]
    [SerializeField] private string _animatorTrigger;
    /// <summary>この行動の基準ダメージ。攻撃開始時に武器へ渡す。</summary>
    [UnityEngine.Tooltip("この行動の基準ダメージ。攻撃開始時に武器へ渡す。")]
    [SerializeField] private float _damage;
    /// <summary>行動の想定射程（Unity単位）。現在の攻撃実行処理では直接参照しない設定。</summary>
    [UnityEngine.Tooltip("行動の想定射程（Unity単位）。現在の攻撃実行処理では直接参照しない設定。")]
    [SerializeField] private float _range;
    /// <summary>有効にする武器Colliderの番号。現在の攻撃実行処理では直接参照しない設定。</summary>
    [UnityEngine.Tooltip("有効にする武器Colliderの番号。現在の攻撃実行処理では直接参照しない設定。")]
    [SerializeField] private int _hitboxIndex;
    /// <summary>Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    [UnityEngine.Tooltip("Hitbox有効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。")]
    [SerializeField] private float _hitboxEnableDelay;
    /// <summary>Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。</summary>
    [UnityEngine.Tooltip("Hitbox無効化までの遅延設定（秒）。現行ではAnimation EventがON/OFFを管理する。")]
    [SerializeField] private float _hitboxDisableDelay;
}
