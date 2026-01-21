using UnityEngine;

public sealed class PlayerWeapon
{
    public PlayerWeapon(Collider[] coliders)
    {
        _weaponColliders = coliders;
    }

    private Collider[] _weaponColliders;

    private void Awake()
    {
        SetHitboxActive(false);
    }

    private void OnDisable()
    {
        SetHitboxActive(false);
    }

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

    //private void Reset()
    //{
    //    _weaponColliders = GetComponentsInChildren<Collider>(true);
    //}
}
