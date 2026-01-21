using UnityEngine;

public class PlayerAttacker
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName, PlayerWeapon plaerWeapon)
    {
        _animController = animController;
        _animName = animName;
        _weapon = plaerWeapon;
    }

    public bool IsDrawingSword { get; private set; } = false;
    private PlayerAnimationController _animController;
    private AnimationName _animName;
    private PlayerWeapon _weapon;

    public void DrawSword()
    {
        if (IsDrawingSword) return;

        IsDrawingSword = true;
        if (!string.IsNullOrEmpty(_animName?.IsDrawingSword))
            _animController?.PlayTrriger(_animName.IsDrawingSword);
    }

    public void PlayLightAttack(bool enableWeaponHitbox)
        => PlayAttack(_animName?.LightAttack, enableWeaponHitbox);
    public void PlayStrongAttack(bool enableWeaponHitbox)
        => PlayAttack(_animName?.StrongAttack, enableWeaponHitbox);
    public void PlayJustAvoidAttack(bool enableWeaponHitbox)
        => PlayAttack(_animName?.JustAvoidAttack, enableWeaponHitbox);

    public void EndAttack()
    {
        _weapon?.DisableHitbox();
    }

    private void PlayAttack(string triggerName, bool enableWeaponHitbox)
    {
        if (!string.IsNullOrEmpty(triggerName))
        {
            _animController?.PlayTrriger(triggerName);
        }

        if (enableWeaponHitbox)
        {
            _weapon?.EnableHitbox();
        }
        else
        {
            _weapon?.DisableHitbox();
        }
    }
}
