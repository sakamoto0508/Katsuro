using UnityEngine;

/// <summary>
/// プレイヤーの攻撃アニメーションや武器ヒット判定を統括するクラス。
/// 抜刀状態の管理や、アニメーション用トリガー発行を担当する。
/// </summary>
public class PlayerAttacker
{
    /// <summary>
    /// 攻撃制御に必要な依存コンポーネントを受け取り、初期化する。
    /// </summary>
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName, PlayerWeapon playerWeapon)
    {
        _animController = animController;
        _animName = animName;
        _weapon = playerWeapon;
    }

    /// <summary>抜刀済みで攻撃可能なら true。</summary>
    public bool IsSwordReady => _isSwordReady;

    /// <summary>現在抜刀モーション中なら true。</summary>
    public bool IsDrawingSword => _isDrawingSword;

    private readonly PlayerAnimationController _animController;
    private readonly AnimationName _animName;
    private readonly PlayerWeapon _weapon;
    private bool _isSwordReady;
    private bool _isDrawingSword;

    /// <summary>
    /// 抜刀可能なら対応アニメーションを再生する。
    /// 既に抜刀済み／モーション中の場合は何もしない。
    /// </summary>
    public void DrawSword()
    {
        if (_isSwordReady || _isDrawingSword)
        {
            return;
        }

        _isDrawingSword = true;

        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
        {
            _animController?.PlayTrriger(_animName.IsDrawingSword);
        }
    }

    /// <summary>
    /// 抜刀アニメーション完了時に呼び出し、抜刀状態を更新する。
    /// </summary>
    public void CompleteDrawSword()
    {
        if (!_isDrawingSword)
        {
            return;
        }

        _isDrawingSword = false;
        _isSwordReady = true;
    }

    /// <summary>ライト攻撃アニメーションを再生する。</summary>
    public void PlayLightAttack() => PlayTrigger(_animName?.LightAttack);

    /// <summary>強攻撃アニメーションを再生する。</summary>
    public void PlayStrongAttack() => PlayTrigger(_animName?.StrongAttack);

    /// <summary>
    /// 攻撃終了時に武器のヒットボックスを無効化する。
    /// </summary>
    public void EndAttack()
    {
        _weapon?.DisableHitbox();
    }

    /// <summary>
    /// 指定したトリガー名を Animator に送信する。
    /// 未設定の場合は警告を出して終了する。
    /// </summary>
    private void PlayTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning("PlayerAttacker: Attack trigger is not assigned.");
            return;
        }

        _animController?.PlayTrriger(triggerName);
    }
}
