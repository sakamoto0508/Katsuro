using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Unity.Cinemachine;
using TMPro;
using Mock.UI;
public static class InspectFinalBlow
{
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    public static string Main()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Mock/Scenes/GameScene.unity");bool opened=!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Mock/Scenes/GameScene.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var b=new StringBuilder();
        try
        {
            var roots=scene.GetRootGameObjects();
            foreach(var root in roots)
            {
                foreach(var m in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if(m is FinalBlowManager||m is GameManager||m is GlobalFader||m is HitStopManager||m is AudioManager||m is DamageNumbers)
                        b.AppendLine(Path(m.transform)+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m.gameObject)+" "+EditorJsonUtility.ToJson(m));
                foreach(var c in root.GetComponentsInChildren<Canvas>(true))
                {
                    b.AppendLine("CANVAS "+Path(c.transform)+" mode="+c.renderMode+" order="+c.sortingOrder+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject));
                    foreach(var t in c.GetComponentsInChildren<RectTransform>(true))
                        if(t.parent==c.transform||t.GetComponent<CanvasGroup>()!=null||t.GetComponent<TMP_Text>()!=null||t.GetComponent<PlayerHUDView>()!=null)
                        {
                            var group=t.GetComponent<CanvasGroup>();var text=t.GetComponent<TMP_Text>();
                            b.AppendLine(" UI "+Path(t)+" active="+t.gameObject.activeSelf+" rect="+t.rect+" group="+(group!=null?group.alpha.ToString():"none")+" text="+(text!=null?text.text:""));
                        }
                }
                foreach(var c in root.GetComponentsInChildren<CinemachineCamera>(true))b.AppendLine("CM "+Path(c.transform)+" "+EditorJsonUtility.ToJson(c));
                foreach(var c in root.GetComponentsInChildren<Camera>(true))b.AppendLine("CAM "+Path(c.transform)+" FOV="+c.fieldOfView);
                foreach(var a in root.GetComponentsInChildren<Animator>(true))
                {
                    var ctrl=a.runtimeAnimatorController as AnimatorController;if(ctrl==null)continue;
                    b.AppendLine("ANIMATOR "+Path(a.transform)+" controller="+AssetDatabase.GetAssetPath(ctrl));
                    foreach(var s in ctrl.layers.SelectMany(l=>l.stateMachine.states).Select(x=>x.state).Where(s=>s.name.Contains("Death")||s.name.Contains("Unequip")))
                    {
                        var clip=s.motion as AnimationClip;b.AppendLine(" state="+s.name+" speed="+s.speed+" clip="+AssetDatabase.GetAssetPath(clip)+" length="+clip?.length+" events="+string.Join(",",clip?.events.Select(e=>e.functionName+":"+e.stringParameter+"@"+e.time)??new string[0]));
                    }
                }
            }
            foreach(var guid in AssetDatabase.FindAssets("t:AudioConfig")){var path=AssetDatabase.GUIDToAssetPath(guid);b.AppendLine(path+" "+EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<AudioConfig>(path)));}
        }
        finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        return b.ToString();
    }
}
