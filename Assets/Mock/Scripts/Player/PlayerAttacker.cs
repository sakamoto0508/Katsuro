using UnityEngine;

public class PlayerAttacker
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName, PlayerWeapon playerWeapon)
    {
        _animController = animController;
        _animName = animName;
        _weapon = playerWeapon;
    }

    public bool IsDrawingSword { get; private set; }

    private readonly PlayerAnimationController _animController;
    private readonly AnimationName _animName;
    private readonly PlayerWeapon _weapon;

    public void DrawSword()
    {
        if (IsDrawingSword)
        {
            return;
        }

        IsDrawingSword = true;

        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
        {
            _animController?.PlayTrriger(_animName.IsDrawingSword);
        }
    }

    public void PlayLightAttack() => PlayAttack(_animName?.LightAttack);
    public void PlayStrongAttack() => PlayAttack(_animName?.StrongAttack);

    public void EndAttack()
    {
        _weapon?.DisableHitbox();
    }

    private void PlayAttack(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            Debug.LogWarning("PlayerAttacker: Attack trigger is not assigned.");
            return;
        }

        _animController?.PlayTrriger(triggerName);
    }
}
