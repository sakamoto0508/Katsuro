using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class InspectCounterTempo
{
    public static string Main()
    {
        var b=new StringBuilder();
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab");
        var controller=player.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        foreach(var child in controller.layers[0].stateMachine.states.Where(s=>s.state.tag==PlayerAttacker.JustAvoidCounterTag))
        {
            var s=child.state;var clip=s.motion as AnimationClip;
            b.AppendLine(s.name+" tag="+s.tag+" speed="+s.speed+" multiplier="+s.speedParameter+" active="+s.speedParameterActive+" clip="+AssetDatabase.GetAssetPath(clip)+" length="+clip.length+" fps="+clip.frameRate);
            b.AppendLine("events="+string.Join(",",clip.events.Select(e=>e.functionName+"@"+e.time+" frame="+e.time*clip.frameRate)));
            foreach(var t in s.transitions)b.AppendLine("transition duration="+t.duration+" exit="+t.hasExitTime+" exitTime="+t.exitTime+" offset="+t.offset);
            var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(player,preview);var a=root.GetComponent<Animator>();
                var hand=a.GetBoneTransform(HumanBodyBones.RightHand);var chest=a.GetBoneTransform(HumanBodyBones.Chest);
                if(hand!=null&&chest!=null)
                {
                    float on=clip.events.First(e=>e.functionName=="AnimEvent_EnableWeaponHitbox").time;
                    for(float t=Mathf.Max(0,on-.12f);t<=on+.04f;t+=1/clip.frameRate)
                    {clip.SampleAnimation(root,t);b.AppendLine("sample t="+t+" rightHand/chest="+(hand.position-chest.position).ToString("F3"));}
                }
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
        }
        var config=AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");b.AppendLine("VFX="+EditorJsonUtility.ToJson(config));
        foreach(var p in config.HitVFX.GetComponentsInChildren<ParticleSystem>(true))b.AppendLine("Blood "+p.name+" burst="+p.emission.burstCount+" speed="+p.main.startSpeed.constant+" size="+p.main.startSize.constant);
        foreach(var path in new[]{"Assets/Mock/Prefabs/Enemy.prefab","Assets/Mock/Prefabs/Player.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);b.AppendLine(path+" feedback="+EditorJsonUtility.ToJson(root.GetComponent<CombatFeedback>()));
            foreach(var t in root.GetComponentsInChildren<SwordTrail>(true))b.AppendLine("Trail="+EditorJsonUtility.ToJson(t));
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Mock/Prefabs"}))
        {var path=AssetDatabase.GUIDToAssetPath(guid);var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var h in root.GetComponentsInChildren<HitStopManager>(true))b.AppendLine("HitStop "+path+" "+EditorJsonUtility.ToJson(h));}
        return b.ToString();
    }
}
