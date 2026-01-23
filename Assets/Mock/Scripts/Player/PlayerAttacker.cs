using UnityEngine;

public class PlayerAttacker
{
    public PlayerAttacker(PlayerAnimationController animController, AnimationName animName, PlayerWeapon playerWeapon)
    {
        _animController = animController;
        _animName = animName;
        _weapon = playerWeapon;
    }

    public bool IsSwordReady => _isSwordReady;
    public bool IsDrawingSword => _isDrawingSword;

    private readonly PlayerAnimationController _animController;
    private readonly AnimationName _animName;
    private readonly PlayerWeapon _weapon;
    private bool _isSwordReady;
    private bool _isDrawingSword;

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

    public void CompleteDrawSword()
    {
        if (!_isDrawingSword)
        {
            return;
        }

        _isDrawingSword = false;
        _isSwordReady = true;
    }

    public void PlayLightAttack() => PlayTrigger(_animName?.LightAttack);
    public void PlayStrongAttack() => PlayTrigger(_animName?.StrongAttack);

    public void EndAttack()
    {
        _weapon?.DisableHitbox();
    }

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
