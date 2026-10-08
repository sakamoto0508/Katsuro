using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
public static class AuditAttackPoses
{
    static System.Collections.Generic.IEnumerable<AnimatorState> States(AnimatorStateMachine sm) {foreach(var s in sm.states)yield return s.state;foreach(var sub in sm.stateMachines)foreach(var s in States(sub.stateMachine))yield return s;}
    static bool Normal(string who,AnimatorState s) {string p=AssetDatabase.GetAssetPath(s.motion);return who=="Enemy"?p.Contains("/Enemy/")&&p.Contains("Attack/"):p.Contains("/Player/")&&(p.Contains("/LightAttack/")||p.Contains("/UnLockLightAttack/")||p.Contains("/StrongAttack/"));}
    public static string Before() => Capture("Before");
    public static string After() => Capture("After");
    static string Capture(string suffix)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var b=new StringBuilder();var summary=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"}) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab");
            var clips=((AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController).layers.SelectMany(l=>States(l.stateMachine)).Where(s=>Normal(who,s)).Select(s=>(AnimationClip)s.motion).Distinct();
            foreach(var c in clips) {
                string path=AssetDatabase.GetAssetPath(c);using(var sha=SHA256.Create())b.AppendLine("HASH "+path+" "+BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))));
                b.AppendLine("SETTINGS "+JsonUtility.ToJson(AnimationUtility.GetAnimationClipSettings(c)));
                foreach(var e in AnimationUtility.GetAnimationEvents(c))b.AppendLine("EVENT "+JsonUtility.ToJson(e));
                var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var graph=PlayableGraph.Create("Attack pose audit");
                try {
                    foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;
                    foreach(var rb in root.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
                    var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)nav.enabled=false;
                    var a=root.GetComponent<Animator>();a.fireEvents=false;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,c);p.SetApplyFootIK(false);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(p);graph.Play();
                    Transform tip=null,bladeBase=null;
                    var trail=root.GetComponentsInChildren<SwordTrail>(true).FirstOrDefault(x=>new SerializedObject(x).FindProperty("_tip").objectReferenceValue!=null);
                    if(trail!=null){var so=new SerializedObject(trail);tip=so.FindProperty("_tip").objectReferenceValue as Transform;bladeBase=so.FindProperty("_bladeBase").objectReferenceValue as Transform;}
                    if(tip==null)tip=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="BladeTip"||x.name=="Tip");
                    if(tip==null)tip=a.GetBoneTransform(HumanBodyBones.RightHand);
                    if(bladeBase==null)bladeBase=a.GetBoneTransform(HumanBodyBones.RightHand);
                    var times=Enumerable.Range(0,Mathf.CeilToInt(c.length*60)+1).Select(i=>Mathf.Min(i/60f,c.length)).Concat(AnimationUtility.GetAnimationEvents(c).Select(e=>Mathf.Clamp(e.time,0,c.length))).Distinct().OrderBy(t=>t);
                    Vector3 previous=Vector3.zero;float previousTime=0,maxSpeed=0,peak=0;
                    foreach(float t in times) {
                        p.SetTime(t);graph.Evaluate(0);Vector3 v=root.transform.InverseTransformPoint(tip.position);float speed=t>previousTime?(v-previous).magnitude/(t-previousTime):0;
                        if(speed>maxSpeed){maxSpeed=speed;peak=t;} previous=v;previousTime=t;
                        b.AppendLine("POSE "+c.name+" "+t.ToString("F6")+" tip="+v.ToString("F6")+" base="+root.transform.InverseTransformPoint(bladeBase.position).ToString("F6")+" hips="+a.GetBoneTransform(HumanBodyBones.Hips).localRotation.ToString("F6")+" chest="+a.GetBoneTransform(HumanBodyBones.Chest).localRotation.ToString("F6"));
                        if(AnimationUtility.GetAnimationEvents(c).Any(e=>Mathf.Abs(e.time-t)<.00001f))summary.AppendLine(c.name+" event@"+t.ToString("F3")+" tip="+v.ToString("F3")+" speed="+speed.ToString("F2"));
                    }
                    summary.AppendLine(c.name+" peak tip/hand speed="+maxSpeed.ToString("F2")+" at="+peak.ToString("F3")+" marker="+tip.name);
                } finally {graph.Destroy();UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
            }
        }
        System.IO.File.WriteAllText("AgentScripts/AttackPoses"+suffix+".txt",b.ToString());System.IO.File.WriteAllText("AgentScripts/AttackPoseSummary"+suffix+".txt",summary.ToString());
        if(suffix=="After"&&System.IO.File.ReadAllText("AgentScripts/AttackPosesBefore.txt")!=b.ToString())throw new Exception("Clip/event/pose audit differs");
        return summary.ToString()+"\n"+(suffix=="After"?"All clip bytes, settings, events and sampled blade/hand/bone poses match Before.":"Before snapshot saved.");
    }
}
