using UnityEngine;

/// <summary>
/// View interface for the player HUD (MVP).
/// </summary>
public interface IPlayerHUDView
{
    void SetHpNormalized(float normalized);
    void SetSkillNormalized(float normalized);
}
