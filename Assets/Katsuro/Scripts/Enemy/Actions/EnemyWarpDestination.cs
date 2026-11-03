using UnityEngine;
using UnityEngine.AI;

/// <summary>プレイヤー背後の同じ床上に、敵Capsule全体が収まるワープ先だけを許可する。</summary>
public static class EnemyWarpDestination
{
    public static Vector3 DesiredPoint(Transform player, float distance, float angle)
    {
        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (forward.sqrMagnitude < .001f) return new Vector3(float.NaN, 0, 0);
        return player.position + Quaternion.AngleAxis(angle, Vector3.up) * -forward.normalized * distance;
    }

    public static bool TryFind(Transform player, CapsuleCollider body, NavMeshAgent agent,
        EnemyAttackPattern pattern, out Vector3 destination)
    {
        destination = default;
        if (player == null || body == null || !body.enabled || body.direction != 1 || agent == null ||
            pattern == null || !pattern.IsValid) return false;
        Vector3 desired = DesiredPoint(player, pattern.TeleportDistance, pattern.TeleportAngleOffset);
        if (!float.IsFinite(desired.x) || !float.IsFinite(desired.z)) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(desired, out var hit, .35f, filter) || Mathf.Abs(hit.position.y - desired.y) > .35f) return false;
        if (!NavMesh.SamplePosition(player.position, out var playerFloor, .5f, filter) ||
            NavMesh.Raycast(playerFloor.position, hit.position, out _, filter)) return false;
        // Bakeに未反映の壁でも、Playerとの間を遮っている地点へは移動しない。
        Vector3 corridor = hit.position - playerFloor.position;
        foreach (var obstruction in Physics.RaycastAll(playerFloor.position + Vector3.up * .8f,
                     corridor.normalized, corridor.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (obstruction.collider.transform.IsChildOf(body.transform) || obstruction.collider.transform.IsChildOf(player)) continue;
            return false;
        }
        Vector3 scale = body.transform.lossyScale;
        float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) + .05f;
        if (!NavMesh.FindClosestEdge(hit.position, out var edge, filter) || edge.distance < radius) return false;
        Vector3 face = Vector3.ProjectOnPlane(player.position - hit.position, Vector3.up);
        if (face.sqrMagnitude < .01f) return false;
        var rotation = Quaternion.LookRotation(face);
        Vector3 center = hit.position + rotation * Vector3.Scale(body.center, scale);
        float half = Mathf.Max(0, body.height * Mathf.Abs(scale.y) * .5f - (radius - .05f));
        foreach (var collider in Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                     radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform.IsChildOf(body.transform)) continue;
            return false;
        }
        destination = hit.position;
        return true;
    }
}
