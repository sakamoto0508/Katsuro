using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ValidateNormalHitFlash
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,F).GetValue(o);
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Check(bool v,string m){if(!v)throw new Exception(m);}
    public static string Validate()
    {
        var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Enemy.prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var fb=root.GetComponent<CombatFeedback>();
        try {
            foreach(var mb in root.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;root.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;root.GetComponent<Rigidbody>().isKinematic=true;
            var sources=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var originals=sources.Select(s=>s.sharedMaterials).ToArray();
            Set(fb,"_initialized",true);Set(fb,"_enemyOwner",root.GetComponent<EnemyController>());Set(fb,"_renderers",sources.Cast<Renderer>().ToArray());fb.enabled=true;
            Call(fb,"PrepareNormalHitFlash",Resources.Load<Shader>("CombatGlow"));var overlays=Get<SkinnedMeshRenderer[]>(fb,"_normalFlashRenderers");Check(overlays!=null&&overlays.Any(s=>s!=null),"Overlay not created");
            fb.Hit(new DamageInfo(1,root.transform.position,root.transform.forward,null,null));Call(fb,"UpdateNormalHitFlash");float light=Get<Material>(fb,"_normalFlashMaterial").GetColor("_Tint").a;
            fb.Hit(new DamageInfo(1,root.transform.position,root.transform.forward,null,null,true));Call(fb,"UpdateNormalHitFlash");float heavy=Get<Material>(fb,"_normalFlashMaterial").GetColor("_Tint").a;Check(heavy>light&&heavy<=.281f,"Flash strengths incorrect");
            for(int i=0;i<sources.Length;i++){Check(sources[i].sharedMaterials.SequenceEqual(originals[i]),"Normal flash replaced original material");if(overlays[i]!=null)Check(overlays[i].sharedMesh==sources[i].sharedMesh&&overlays[i].bones.SequenceEqual(sources[i].bones)&&overlays[i].rootBone==sources[i].rootBone,"Overlay skeleton mismatch");}
            fb.CancelNormalHitReaction();Check(overlays.Where(x=>x!=null).All(x=>!x.enabled),"Cancel left overlay active");
            string result="Enemy normal overlay created on actual skeleton; original materials unchanged; Light alpha="+light.ToString("F3")+" Heavy alpha="+heavy.ToString("F3")+"; mesh/bones binding and cancellation passed. Rendering appearance untested.";System.IO.File.WriteAllText("AgentScripts/NormalHitFlashTests.txt",result);return result;
        }finally{
            var overlays=Get<SkinnedMeshRenderer[]>(fb,"_normalFlashRenderers");if(overlays!=null)foreach(var overlay in overlays)if(overlay!=null)UnityEngine.Object.DestroyImmediate(overlay.gameObject);Set(fb,"_normalFlashRenderers",null);
            var material=Get<Material>(fb,"_normalFlashMaterial");if(material!=null)UnityEngine.Object.DestroyImmediate(material);Set(fb,"_normalFlashMaterial",null);UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
