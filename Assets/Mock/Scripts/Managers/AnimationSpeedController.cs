using UnityEngine;

//animator の再生速度を制御するための補助コンポーネント。
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class AnimationSpeedController : MonoBehaviour
{
    private Animator animator;
    private float baseSpeed;
    private float status = 1f, temporary = 1f, expires;

    public static AnimationSpeedController For(Animator target)
    {
        var value = target.GetComponent<AnimationSpeedController>();
        return value != null ? value : target.gameObject.AddComponent<AnimationSpeedController>();
    }

    private bool _initialized;
    public void Init()
    {
        if (_initialized) return;
        _initialized = true; 
        animator = GetComponent<Animator>(); baseSpeed = animator.speed; 
    }

    // <summary>アニメーションの再生速度を設定します。0 で停止、1 で通常速度、2 で倍速など。</summary>
    public void SetStatus(float multiplier)
    { 
        status = Mathf.Max(0f, multiplier); Refresh(); 
    }

    // <summary>一時的にアニメーションの再生速度を設定します。0 で停止、1 で通常速度、2 で倍速など。</summary>
    public void SetTemporary(float multiplier, float seconds)
    {
        temporary = Mathf.Max(0f, multiplier);
        expires = Time.realtimeSinceStartup + Mathf.Max(0f, seconds);
        Refresh();
    }

    /// <summary>一時的な再生速度の設定をクリアします。</summary>
    public void ClearTemporary() 
    { 
        temporary = 1f; expires = 0f; Refresh(); 
    
    }
    private void Update() => Refresh();

    /// <summary>アニメーションの再生速度を更新します。</summary>
    private void Refresh()
    {
        if (Time.realtimeSinceStartup >= expires) temporary = 1f;
        if (animator != null) animator.speed = baseSpeed * status * temporary;
    }

    private void OnDisable()
    {
        status = temporary = 1f;
        expires = 0f;
        if (animator != null) animator.speed = baseSpeed;
    }
}
