using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class LookOnCamera : MonoBehaviour
{
    public bool IsLockOn { get; private set; }
    
    public void LockOn()
    {
        IsLockOn = true;
    }

    public void UnLockOn()
    {
        IsLockOn = false;
    }
}
