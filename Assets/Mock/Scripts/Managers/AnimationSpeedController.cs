using UnityEngine;

//animator の再生速度を制御するための補助コンポーネント。
/// <summary>基準速度・状態効果・実時間で期限切れになる一時倍率を合成し、Animator速度を更新する。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class AnimationSpeedController : MonoBehaviour
{
    private Animator animator;
    private float baseSpeed;
    private float status = 1f, temporary = 1f, expires;



    private bool _initialized;
    /// <summary>Animatorとその基準速度を一度だけ取得する。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true; 
        animator = GetComponent<Animator>(); baseSpeed = animator.speed; 
    }

    // 状態効果と一時演出の速度は別々に保持する。
    /// <summary>状態効果の持続倍率を更新し、基準速度と一時倍率を掛けてAnimatorへ反映する。</summary>
    public void SetStatus(float multiplier)
    { 
        status = Mathf.Max(0f, multiplier); Refresh(); 
    }

    // HitStopの期限は実時間で管理する。
    /// <summary>HitStopなどの一時速度と実時間の期限を設定し、現在の状態効果と合成する。</summary>
    /// <param name="multiplier">基準速度と状態倍率へ掛ける一時倍率。</param>
    /// <param name="seconds">実時間で計測する有効秒数。</param>
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
    /// <summary>実時間の期限を判定し、一時倍率の自動解除と合成速度の反映を行う。</summary>
    private void Update() => Refresh();

    /// <summary>アニメーションの再生速度を更新します。</summary>
    private void Refresh()
    {
        if (Time.realtimeSinceStartup >= expires) temporary = 1f;
        if (animator != null) animator.speed = baseSpeed * status * temporary;
    }

    /// <summary>持続・一時倍率と期限を解除し、Animatorを保存した基準速度へ戻す。</summary>
    private void OnDisable()
    {
        status = temporary = 1f;
        expires = 0f;
        if (animator != null) animator.speed = baseSpeed;
    }
}
