using UnityEngine;

/// <summary>PlayerとEnemyが使用するAnimatorパラメータ名をInspectorで共有する設定。</summary>
[CreateAssetMenu(fileName = "AnimName", menuName = "ScriptableObjects/Player/AnimName")]
public class AnimationName : ScriptableObject
{
    /// <summary>移動速度をAnimatorへ渡すFloatパラメータ名。</summary>
    public string MoveVelocity => _moveVelocity;
    /// <summary>移動方向の左右成分をAnimatorへ渡すFloatパラメータ名。</summary>
    public string MoveVectorX => _moveVectorX;
    /// <summary>移動方向の前後成分をAnimatorへ渡すFloatパラメータ名。</summary>
    public string MoveVectorY => _moveVectorY;
    /// <summary>抜刀中の状態をAnimatorへ伝えるパラメータ名。Controller側と一致させる。</summary>
    public string IsDrawingSword => _isDrawingSword;
    /// <summary>Lock-On状態をAnimatorへ伝えるBoolパラメータ名。</summary>
    public string IsLockOn => _isLockOn;
    /// <summary>通常Light攻撃を開始するAnimator Trigger名。</summary>
    public string LightAttack => _lightAttack;
    /// <summary>通常Heavy攻撃を開始するAnimator Trigger名。</summary>
    public string StrongAttack => _strongAttack;
    /// <summary>Just Avoid成功後の追撃を開始するAnimator Trigger名。</summary>
    public string JustAvoidAttack => _justAvoidAttack;
    /// <summary>攻撃のコンボ段階を指定するAnimator Intパラメータ名。</summary>
    public string ComboStep => _comboStep;
    /// <summary>バックステップを開始するAnimator Trigger名。</summary>
    public string BackStep => _backStep;
    /// <summary>刀を抜いている状態をAnimatorへ伝えるBoolパラメータ名。</summary>
    public string IsSwordDrawn => _isSwordDrawn;
    /// <summary>Just Avoid成功演出を開始するAnimator Trigger名。</summary>
    public string JustAvoid => _justAvoid;
    /// <summary>Just Avoid受付状態をAnimatorへ伝えるパラメータ名。</summary>
    public string JustAvoidWindow => _justAvoidWindow;
    /// <summary>Enemy死亡アニメーションを開始するAnimator Trigger名。</summary>
    public string EnemyDead => _enemyDead;
    /// <summary>Player死亡アニメーションを開始するAnimator Trigger名。</summary>
    public string PlayerDead => _playerDead;
    /// <summary>勝利時の納刀アニメーションを開始するAnimator Trigger名。</summary>
    public string SwordSheathing => _swordSheathing;

    /// <summary>移動速度をAnimatorへ渡すFloatパラメータ名。</summary>
    [UnityEngine.Tooltip("移動速度をAnimatorへ渡すFloatパラメータ名。")]
    [SerializeField] private string _moveVelocity = "MoveVelocity";
    /// <summary>移動方向の左右成分をAnimatorへ渡すFloatパラメータ名。</summary>
    [UnityEngine.Tooltip("移動方向の左右成分をAnimatorへ渡すFloatパラメータ名。")]
    [SerializeField] private string _moveVectorX = "MoveVectorX";
    /// <summary>移動方向の前後成分をAnimatorへ渡すFloatパラメータ名。</summary>
    [UnityEngine.Tooltip("移動方向の前後成分をAnimatorへ渡すFloatパラメータ名。")]
    [SerializeField] private string _moveVectorY = "MoveVectorY";
    /// <summary>抜刀中の状態をAnimatorへ伝えるパラメータ名。Controller側と一致させる。</summary>
    [UnityEngine.Tooltip("抜刀中の状態をAnimatorへ伝えるパラメータ名。Controller側と一致させる。")]
    [SerializeField] private string _isDrawingSword = "";
    /// <summary>Lock-On状態をAnimatorへ伝えるBoolパラメータ名。</summary>
    [UnityEngine.Tooltip("Lock-On状態をAnimatorへ伝えるBoolパラメータ名。")]
    [SerializeField] private string _isLockOn = "";
    /// <summary>通常Light攻撃を開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("通常Light攻撃を開始するAnimator Trigger名。")]
    [SerializeField] private string _lightAttack = "";
    /// <summary>通常Heavy攻撃を開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("通常Heavy攻撃を開始するAnimator Trigger名。")]
    [SerializeField] private string _strongAttack = "";
    /// <summary>Just Avoid成功後の追撃を開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("Just Avoid成功後の追撃を開始するAnimator Trigger名。")]
    [SerializeField] private string _justAvoidAttack = "";
    /// <summary>攻撃のコンボ段階を指定するAnimator Intパラメータ名。</summary>
    [UnityEngine.Tooltip("攻撃のコンボ段階を指定するAnimator Intパラメータ名。")]
    [SerializeField] private string _comboStep = "ComboStep";
    /// <summary>バックステップを開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("バックステップを開始するAnimator Trigger名。")]
    [SerializeField] private string _backStep = "BackStep";
    /// <summary>刀を抜いている状態をAnimatorへ伝えるBoolパラメータ名。</summary>
    [UnityEngine.Tooltip("刀を抜いている状態をAnimatorへ伝えるBoolパラメータ名。")]
    [SerializeField] private string _isSwordDrawn = "IsSwordDrawn";
    /// <summary>Just Avoid成功演出を開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("Just Avoid成功演出を開始するAnimator Trigger名。")]
    [SerializeField] private string _justAvoid = "JustAvoid";
    /// <summary>Just Avoid受付状態をAnimatorへ伝えるパラメータ名。</summary>
    [UnityEngine.Tooltip("Just Avoid受付状態をAnimatorへ伝えるパラメータ名。")]
    [SerializeField] private string _justAvoidWindow = "JustAvoidWindow";
    /// <summary>Enemy死亡アニメーションを開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("Enemy死亡アニメーションを開始するAnimator Trigger名。")]
    [SerializeField] private string _enemyDead = "EnemyDead";
    /// <summary>Player死亡アニメーションを開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("Player死亡アニメーションを開始するAnimator Trigger名。")]
    [SerializeField] private string _playerDead = "PlayerDead";
    /// <summary>勝利時の納刀アニメーションを開始するAnimator Trigger名。</summary>
    [UnityEngine.Tooltip("勝利時の納刀アニメーションを開始するAnimator Trigger名。")]
    [SerializeField] private string _swordSheathing = "SwordSheathing";
}
