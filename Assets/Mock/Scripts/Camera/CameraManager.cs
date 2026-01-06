using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private LockOnCameraMover _lockOnCamera;
    public void Init()
    {
        _lockOnCamera = new LockOnCameraMover();
    }
}
