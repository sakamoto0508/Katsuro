using UnityEngine;

public sealed class PlayerWeapon
{
    public PlayerWeapon(Collider[] weaponColliders)
    {
        _weaponColliders = weaponColliders;
        SetHitboxActive(false);
    }

    private readonly Collider[] _weaponColliders;

    public void EnableHitbox() => SetHitboxActive(true);

    public void DisableHitbox() => SetHitboxActive(false);

    private void SetHitboxActive(bool isActive)
    {
        if (_weaponColliders == null)
        {
            return;
        }

        foreach (var weaponCollider in _weaponColliders)
        {
            if (weaponCollider == null)
            {
                continue;
            }

            weaponCollider.enabled = isActive;
        }
    }
}
