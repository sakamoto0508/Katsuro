using UnityEngine;
using UnityEditor;
using System;
public static class EnlargeWeaponHitbox
{
    const string Path="Assets/Mock/Prefabs/Player.prefab";
    public static string Stage()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var root=PrefabUtility.LoadPrefabContents(Path);
        var pc=root.GetComponent<PlayerController>();
        var so=new SerializedObject(pc);
        var field=so.FindProperty("_weaponHitboxPadding");
        var before=field.vector3Value;
        field.vector3Value=new Vector3(.16f,.08f,.16f);
        so.ApplyModifiedPropertiesWithoutUndo();
        SessionState.SetInt("KatsuroWeaponHitboxRoot",root.GetInstanceID());
        return "Player weapon padding: "+before+" -> "+field.vector3Value;
    }
    public static string Save()
    {
        if(EditorApplication.isCompiling)throw new Exception("Compile pending");
        var root=EditorUtility.InstanceIDToObject(SessionState.GetInt("KatsuroWeaponHitboxRoot",0)) as GameObject;
        if(root==null)throw new Exception("Staged prefab missing");
        PrefabUtility.SaveAsPrefabAsset(root,Path,out bool success);
        if(!success)throw new Exception("Save failed");
        PrefabUtility.UnloadPrefabContents(root);
        SessionState.EraseInt("KatsuroWeaponHitboxRoot");
        AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceUpdate);
        var pc=AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<PlayerController>();
        var padding=new SerializedObject(pc).FindProperty("_weaponHitboxPadding").vector3Value;
        if(padding!=new Vector3(.16f,.08f,.16f))throw new Exception("Reimport verification failed");
        return "Saved and reimport verified: "+padding;
    }
}
