using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
public static class InspectNormalHitFeedback
{
    public static string Before()=>Capture("Before");
    public static string After()=>Capture("After");
    static string Capture(string suffix)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var b=new StringBuilder();var immutable=new StringBuilder();
        foreach(string who in new[]{"Player","Enemy"}) {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/"+who+".prefab");var a=root.GetComponent<Animator>();
            b.AppendLine(who+" Feedback="+EditorJsonUtility.ToJson(root.GetComponent<CombatFeedback>()));b.AppendLine(who+" Animator="+AssetDatabase.GetAssetPath(a.runtimeAnimatorController)+" root="+a.applyRootMotion+" human="+a.isHuman+" validAvatar="+a.avatar.isValid);
            foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder}){var t=a.GetBoneTransform(bone);b.AppendLine(" BONE "+bone+"="+(t==null?"missing":t.name+" parent="+t.parent?.name+" local="+t.localRotation));}
            string path=AssetDatabase.GetAssetPath(a.runtimeAnimatorController);immutable.AppendLine(Hash(path));
            foreach(var c in a.runtimeAnimatorController.animationClips.Distinct())immutable.AppendLine(Hash(AssetDatabase.GetAssetPath(c)));
            if(who=="Player")immutable.AppendLine(Hash(AssetDatabase.GetAssetPath(root)));
            var nav=root.GetComponent<UnityEngine.AI.NavMeshAgent>();if(nav!=null)b.AppendLine("NAV speed="+nav.speed+" enabled="+nav.enabled+" updatePosition="+nav.updatePosition+" rotation="+nav.updateRotation);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:VFXConfig",new[]{"Assets/Mock"})) {string p=AssetDatabase.GUIDToAssetPath(guid);b.AppendLine("VFX "+p+" "+EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<VFXConfig>(p)));immutable.AppendLine(Hash(p));}
        System.IO.File.WriteAllText("AgentScripts/NormalHitFeedback"+suffix+".txt",b.ToString());System.IO.File.WriteAllText("AgentScripts/NormalHitProtected"+suffix+".txt",immutable.ToString());
        if(suffix=="After"&&System.IO.File.ReadAllText("AgentScripts/NormalHitProtectedBefore.txt")!=immutable.ToString())throw new Exception("Protected Animator/Clip/Player prefab/VFX assets changed");return b.ToString()+"\nProtected asset fingerprints "+suffix+" saved.";
    }
    static string Hash(string path){using(var h=SHA256.Create())return path+" "+BitConverter.ToString(h.ComputeHash(System.IO.File.ReadAllBytes(path)));}
}
