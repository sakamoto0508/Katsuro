using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputBuffer _inputBuffer;
    public void Init(InputBuffer inputBuffer)
    {
        _inputBuffer = inputBuffer;
    }
}
