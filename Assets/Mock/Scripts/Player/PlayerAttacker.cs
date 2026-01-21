using UnityEngine;

public class PlayerAttacker
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName)
    {
        _animController = animController;
        _animName = animName;
    }
    public bool IsDrawingSword { get; private set; } = false;
    private PlayerAnimationController _animController;
    private AnimationName _animName;

    public void DrawSword()
    {
        if (IsDrawingSword) return;

        IsDrawingSword = true;
        _animController?.PlayTrriger(_animName.IsDrawingSword);
    }

    private void PlayAttack(string triggerName,bool enableWeaponHitbox)
    {

    }
}
