using UnityEngine;

public class PlayerAttacker
{
    public bool IsDrawingSword { get; private set; } = false;

    public void DrawSword()
    {
        if (IsDrawingSword) return;

        IsDrawingSword = true;

    }
}
