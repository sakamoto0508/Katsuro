using UnityEngine;

/// <summary>
/// プレイヤーの攻撃アニメーションや武器ヒット判定を統括するクラス。
/// 抜刀状態の管理や、アニメーション用トリガー発行を担当する。
/// </summary>
public class PlayerAttacker
{
    /// <summary>
    /// 必要なコンポーネント参照を受け取って攻撃制御を初期化する。
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
    /// 抜刀アニメ完了イベントで呼び、抜刀状態を更新する。
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

    /// <summary>ライト攻撃（0 段目）を再生するショートカット。</summary>
    public void PlayLightAttack() => PlayLightAttack(0, false);

    /// <summary>指定段のライト攻撃アニメーションを再生する。</summary>
    public void PlayLightAttack(int comboStep) => PlayLightAttack(comboStep, false);

    /// <summary>ロックオン状態を考慮しつつライト攻撃を再生する。</summary>
    public void PlayLightAttack(int comboStep, bool isLockOnVariant)
    {
        ApplyLockOnFlag(isLockOnVariant);
        PlayAttackTrigger(_animName?.LightAttack, comboStep);
    }

    /// <summary>強攻撃（0 段目）を再生するショートカット。</summary>
    public void PlayStrongAttack() => PlayStrongAttack(0);

    /// <summary>指定段の強攻撃アニメーションを再生する。</summary>
    public void PlayStrongAttack(int comboStep) => PlayAttackTrigger(_animName?.StrongAttack, comboStep);

    /// <summary>
    /// 攻撃終了時にヒットボックスを明示的に無効化する。
    /// </summary>
    public void EndAttack()
    {
        _weapon?.DisableHitbox();
    }

    /// <summary>
    /// コンボ段数を Animator に渡した上で指定トリガーを実行する。
    /// </summary>
    private void PlayAttackTrigger(string triggerName, int comboStep)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning("PlayerAttacker: Attack trigger is not assigned.");
            return;
        }

        ApplyComboStep(comboStep);
        _animController?.PlayTrriger(triggerName);
    }

    /// <summary>
    /// コンボ段数パラメーターを Animator に反映する。
    /// </summary>
    private void ApplyComboStep(int comboStep)
    {
        if (string.IsNullOrEmpty(_animName?.ComboStep))
        {
            return;
        }

        _animController?.SetInteger(_animName.ComboStep, comboStep);
    }

    /// <summary>
    /// ロックオン状態フラグを Animator に伝える。
    /// </summary>
    private void ApplyLockOnFlag(bool isLockOn)
    {
        if (string.IsNullOrEmpty(_animName?.IsLockOn))
        {
            return;
        }

        _animController?.PlayBool(_animName.IsLockOn, isLockOn);
    }
}
