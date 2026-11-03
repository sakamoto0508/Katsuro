using System;
using UnityEngine;

/// <summary>修練の目的地への実際の接触を通知する。入力や距離の毎フレーム監視を使わない。</summary>
[RequireComponent(typeof(Collider))]
public sealed class TutorialGoal : MonoBehaviour
{
    /// <summary>目的地に入ったPlayerの通知。</summary>
    public event Action<PlayerController> Reached;
    /// <summary>Trigger内へ入ったPlayerを通知する。</summary>
    private void OnTriggerEnter(Collider other) => Notify(other);
    /// <summary>課題を有効にした時点で既に範囲内にいるPlayerも認識する。</summary>
    private void OnTriggerStay(Collider other) => Notify(other);
    /// <summary>武器やEnemyを除き、接触したPlayerの所有者を解決する。</summary>
    private void Notify(Collider other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) Reached?.Invoke(player);
    }
}
