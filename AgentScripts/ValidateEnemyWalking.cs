using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class ValidateEnemyWalking
{
    public static string Validate()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var enemy=new GameObject("Walking direction verification");var player=new GameObject("Unrelated target position");
        try
        {
            var mover=new EnemyMover(null,enemy.transform,player.transform,null,null,null,null,null);
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var direction=typeof(EnemyMover).GetMethod("GetLocalMovementDirection",flags);
            var velocity=typeof(EnemyMover).GetMethod("GetLocomotionVelocity",flags);
            Vector2[] axes={Vector2.up,Vector2.down,Vector2.right,Vector2.left,new Vector2(1,1).normalized,new Vector2(-1,1).normalized,new Vector2(1,-1).normalized,new Vector2(-1,-1).normalized};
            int checkedCases=0;
            foreach(float yaw in new[]{0f,90f,180f,-135f})
            {
                enemy.transform.rotation=Quaternion.Euler(0,yaw,0);
                foreach(var axis in axes)
                {
                    Vector3 world=enemy.transform.TransformDirection(new Vector3(axis.x,0,axis.y))*2+Vector3.up*5;
                    player.transform.position=new Vector3(100,0,-20);
                    var actual=(Vector2)direction.Invoke(mover,new object[]{world});
                    player.transform.position=new Vector3(-500,0,120);
                    var other=(Vector2)direction.Invoke(mover,new object[]{world});
                    if(Vector2.Distance(actual,axis)>.0001f||actual!=other)throw new Exception("Local velocity direction mismatch or target dependence");
                    checkedCases++;
                }
            }
            foreach(var slow in new[]{Vector3.zero,Vector3.right*.049f,Vector3.up*5})
                if((Vector2)direction.Invoke(mover,new object[]{slow})!=Vector2.zero)throw new Exception("Dead zone must select zero direction");
            if((Vector3)velocity.Invoke(mover,null)!=Vector3.zero)throw new Exception("Missing agent must return zero speed");
            mover.HoldMovementForAttack();if((Vector3)velocity.Invoke(mover,null)!=Vector3.zero)throw new Exception("Attack must have zero locomotion input");
            mover.HoldMovementForReaction();if((Vector3)velocity.Invoke(mover,null)!=Vector3.zero)throw new Exception("Reaction must have zero locomotion input");
            return "Passed "+checkedCases+" direction cases (8 directions x 4 rotations), independence from Player position, Y rejection, 0.05m/s dead zone, missing agent/attack/reaction zero input. No Play Mode or live NavMesh movement test.";
        }
        finally{UnityEngine.Object.DestroyImmediate(enemy);UnityEngine.Object.DestroyImmediate(player);}
    }
}
