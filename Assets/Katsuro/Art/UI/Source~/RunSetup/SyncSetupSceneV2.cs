using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SyncSetupSceneV2
{
    public static string Main()
    {
        if (EditorApplication.isPlaying)
            throw new Exception("Run outside Play Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Katsuro/Scenes/TitleScene.unity")
            throw new Exception("Expected TitleScene, got " + scene.path);
        var setup = UnityEngine.Object.FindAnyObjectByType<RunSetupUI>(FindObjectsInactive.Include);
        if (setup == null)
            throw new Exception("Missing existing setup view.");
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(setup.gameObject);
        var mods = PrefabUtility.GetPropertyModifications(root);
        var rootAsset = PrefabUtility.GetCorrespondingObjectFromSource(setup.transform);
        var retained = mods.Where(m => !(m.target is RectTransform) || m.target == rootAsset).ToArray();
        int removed = mods.Length - retained.Length;
        // セットアップのインスタンス配下にある RectTransform のオーバーライドだけを取り除く。
        // 実行時参照、TMP の文言、入力設定、他のインスタンスは維持する。
        PrefabUtility.SetPropertyModifications(root, retained);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Removed " + removed + " stale setup RectTransform overrides; kept " + retained.Length + " other overrides.";
    }
}
