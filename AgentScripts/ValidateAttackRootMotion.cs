using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
public static class ValidateAttackRootMotion
{
    static void Check(bool b,string message){if(!b)throw new Exception(message);}
    public static string Validate()
    {
        Check(!EditorApplication.isPlaying,"Edit Mode required");
        var scene=EditorSceneManager.NewPreviewScene();
        GameObject body=null,wall=null,floor=null,enemy=null;NavMeshData data=null;NavMeshDataInstance instance=default;
        try
        {
            Vector3 origin=new Vector3(5000,1,5000);
            body=new GameObject("Root motion test body",typeof(Rigidbody),typeof(CapsuleCollider));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(body,scene);body.transform.position=origin;
            var rb=body.GetComponent<Rigidbody>();rb.useGravity=false;rb.constraints=RigidbodyConstraints.FreezeRotation;
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.position=origin+new Vector3(0,-1.05f,0);floor.transform.localScale=new Vector3(20,.1f,20);
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,scene);wall.transform.position=origin+new Vector3(0,0,2);wall.transform.localScale=new Vector3(3,3,.1f);
            Physics.SyncTransforms();
            var walk=AttackRootMotionPhysics.LimitDisplacement(rb,new Vector3(0,9,.2f));
            Check(walk.y==0f&&walk.z>.19f,"Ground contact blocks horizontal root movement");
            var stopped=AttackRootMotionPhysics.LimitDisplacement(rb,new Vector3(0,0,4));
            Check(stopped.z>0f&&stopped.z<1.5f,"Solid wall sweep must limit displacement");
            var mover=new PlayerMover(null,rb,body.transform,null,null,null,.25f);
            rb.linearVelocity=new Vector3(0,-2,0);mover.BeginAttackRootMotion();mover.QueueAttackRootMotion(new Vector3(.04f,99,0),true);mover.FixedUpdateAttackRootMotion(true);
            Check(Mathf.Abs(rb.linearVelocity.x-.01f/Time.fixedDeltaTime)<.001f&&rb.linearVelocity.y==-2f,"Player displacement/scale/Y integration incorrect");
            mover.QueueAttackRootMotion(Vector3.right,true);mover.FixedUpdateAttackRootMotion(false);Check(rb.linearVelocity.x==0,"HitStop must clear movement");
            mover.FixedUpdateAttackRootMotion(true);Check(rb.linearVelocity.x==0,"HitStop release must not replay pending delta");mover.EndAttackRootMotion();Check(!mover.IsUsingAttackRootMotion,"Player exit flag retained");
            var pc=body.AddComponent<PlayerController>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PlayerController).GetField("_playerResource",flags).SetValue(pc,new PlayerResource(null,null));
            pc.BeginAnimationAttackRootMotion(101);pc.BeginAnimationAttackRootMotion(202);pc.EndAnimationAttackRootMotion(101);
            Check((int)typeof(PlayerController).GetField("_attackAnimationHash",flags).GetValue(pc)==202,"Outgoing Player combo cancelled incoming owner");
            pc.EndAnimationAttackRootMotion(202);Check((int)typeof(PlayerController).GetField("_attackAnimationHash",flags).GetValue(pc)==0,"Player final state exit retains owner");

            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab");
            enemy=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemy,scene);
            foreach(var c in enemy.GetComponentsInChildren<MonoBehaviour>(true))c.enabled=false;
            var agent=enemy.GetComponent<NavMeshAgent>();agent.enabled=false;
            var build=NavMesh.GetSettingsByID(agent.agentTypeID);
            var sources=new List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(20,.1f,20),transform=Matrix4x4.TRS(new Vector3(0,-.05f,0),Quaternion.identity,Vector3.one),area=0}};
            Vector3 navOrigin=new Vector3(5100,0,5100);
            data=NavMeshBuilder.BuildNavMeshData(build,sources,new Bounds(Vector3.zero,new Vector3(24,8,24)),Vector3.zero,Quaternion.identity);Check(data!=null,"Temporary NavMesh build failed");instance=NavMesh.AddNavMeshData(data,navOrigin,Quaternion.identity);
            var er=enemy.GetComponent<Rigidbody>();er.position=navOrigin;enemy.transform.position=navOrigin;agent.enabled=true;
            bool navBound=agent.Warp(navOrigin);
            var so=new SerializedObject(enemy.GetComponent<EnemyController>());var status=so.FindProperty("_enemyStuts").objectReferenceValue as EnemyStuts;var names=so.FindProperty("_animName").objectReferenceValue as AnimationName;
            var em=new EnemyMover(status,enemy.transform,body.transform,null,er,navBound?agent:null,enemy.GetComponent<Animator>(),names,.18f);
            bool wasKinematic=er.isKinematic;
            em.HoldMovementForAttack();em.BeginAttackRootMotion();Check(em.IsUsingAttackRootMotion&&(!navBound||!agent.enabled),"Enemy attack hold not exclusive");
            em.HoldMovementForReaction();Check(!em.IsUsingAttackRootMotion,"Reaction must cancel attack root immediately");em.ReleaseMovementAfterReaction();
            Vector3 endpoint=navOrigin+new Vector3(.4f,0,.3f);er.position=endpoint;enemy.transform.position=endpoint;
            em.ReleaseMovementAfterAttack();Check(!em.IsUsingAttackRootMotion&&er.isKinematic==wasKinematic,"Enemy attack mode/physics state not restored");
            if(navBound)Check(agent.enabled&&Vector3.Distance(enemy.transform.position,endpoint)<.05f&&Vector3.Distance(agent.nextPosition,endpoint)<.05f,"Enemy NavMesh final position not synchronized");
            return "Edit Mode checks passed: floor movement, wall sweep, Player XZ/scale/gravity, HitStop queue discard/no replay, combo owner retention/final state exit, Enemy immediate reaction cancellation and exit flags. "+(navBound?"NavMesh final-position synchronization passed.":"NavMeshAgent could not bind in Preview Scene: runtime Warp synchronization is untested; inspected saved settings/code only.")+" Play Mode not used.";
        }
        finally
        {
            if(enemy!=null)UnityEngine.Object.DestroyImmediate(enemy);if(instance.valid)instance.Remove();if(data!=null)UnityEngine.Object.DestroyImmediate(data);
            if(body!=null)UnityEngine.Object.DestroyImmediate(body);if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);if(floor!=null)UnityEngine.Object.DestroyImmediate(floor);EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
