using System;
using UnityEngine;
using UnityEditor;
public static class ConfigureNormalHitFeedback
{
    public static string Save()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("Compiled Edit Mode required");
        string path="Assets/Mock/Prefabs/Enemy.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try {
            var feedback=root.GetComponent<CombatFeedback>();var so=new SerializedObject(feedback);
            so.FindProperty("_lightReactionAngle").floatValue=7.5f;so.FindProperty("_heavyReactionAngle").floatValue=13f;
            so.FindProperty("_reactionDuration").floatValue=.16f;so.FindProperty("_enemyHeavyReactionDuration").floatValue=.22f;
            so.FindProperty("_enemyAttackReactionScale").floatValue=.55f;
            so.FindProperty("_enemyLightFlashStrength").floatValue=.18f;so.FindProperty("_enemyHeavyFlashStrength").floatValue=.28f;
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new Exception("Enemy prefab save failed");
            return "Enemy prefab saved: Light 7.5deg/.16s, Heavy 13deg/.22s, attacking scale .55; normal overlay alpha .18/.28. Existing contact flash .055s and Counter settings preserved.";
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
