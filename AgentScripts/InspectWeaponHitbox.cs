using UnityEngine;
using UnityEditor;
using System.Text;
public static class InspectWeaponHitbox
{
    static void Describe(PlayerController pc,StringBuilder b)
    {
        var so=new SerializedObject(pc);
        Vector3 padding=so.FindProperty("_weaponHitboxPadding").vector3Value;
        b.AppendLine(pc.name+" asset="+AssetDatabase.GetAssetPath(pc)+" scene="+pc.gameObject.scene.path+" padding="+padding);
        var colliders=so.FindProperty("_weaponColliders");
        for(int i=0;i<colliders.arraySize;i++)
        {
            var c=colliders.GetArrayElementAtIndex(i).objectReferenceValue as Collider;
            if(c==null){b.AppendLine("Missing collider "+i);continue;}
            b.AppendLine(c.name+" type="+c.GetType().Name+" localScale="+c.transform.localScale+" lossyScale="+c.transform.lossyScale+" trigger="+c.isTrigger);
            if(c is BoxCollider box) b.AppendLine("size="+box.size+" center="+box.center+" effective="+(box.size+padding));
        }
    }
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("PlayMode="+EditorApplication.isPlaying);
        Describe(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Prefabs/Player.prefab").GetComponent<PlayerController>(),b);
        foreach(var pc in Resources.FindObjectsOfTypeAll<PlayerController>())
            if(!EditorUtility.IsPersistent(pc)&&pc.gameObject.scene.IsValid()&&!EditorSceneManagerProxy(pc)) Describe(pc,b);
        return b.ToString();
    }
    static bool EditorSceneManagerProxy(PlayerController pc)=>UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(pc.gameObject.scene);
}
