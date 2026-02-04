using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDecisionConfig", menuName = "ScriptableObjects/Enemy/DecisionConfig")]
public class EnemyDecisionConfig : ScriptableObject
{
    [Header("Distance thresholds")]
    public float FarDistance = 12f;
    public float NearDistance = 3f;
    public float ObserveSeconds = 1.5f;
    public float ReconsiderInterval = 0.5f;

    [Header("Weights")]
    public float WeightWarpAttack = 10f;
    public float WeightApproach = 30f;
    public float WeightRush = 30f;
    public float WeightObserve = 20f;
    public float WeightSlash = 50f;
    public float WeightBackstep = 20f;
}
