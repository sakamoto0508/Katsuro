#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Executes only an explicit, local, one-shot request. AssetDatabase keeps GUIDs and live Editor paths intact.
[InitializeOnLoad]
internal static class FolderOrganizationBridge
{
    [Serializable] internal sealed class Move { public string source; public string destination; }
    [Serializable] internal sealed class Request { public Move[] moves; }
    [Serializable] internal sealed class Result { public bool success; public string error; public int moved; public string[] dirtyScenes; }
    private const string Root = "Library/FolderOrganization/";

    static FolderOrganizationBridge() => EditorApplication.update += Poll;

    private static void Poll()
    {
        string request = Root + "request.json";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Poll;
        var result = new Result();
        try
        {
            var plan = JsonUtility.FromJson<Request>(File.ReadAllText(request));
            File.Move(request, Root + "started.json");
            var builds = EditorBuildSettings.scenes.Select(s => new { guid = AssetDatabase.AssetPathToGUID(s.path), s.enabled }).ToArray();
            var dirty = new List<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.isDirty) dirty.Add(scene.path);
            }
            result.dirtyScenes = dirty.ToArray();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var move in plan.moves)
                {
                    if (!move.source.StartsWith("Assets/", StringComparison.Ordinal) || !move.destination.StartsWith("Assets/", StringComparison.Ordinal) || move.source.Contains("..") || move.destination.Contains(".."))
                        throw new InvalidOperationException("Invalid asset path");
                    EnsureFolder(Path.GetDirectoryName(move.destination).Replace('\\', '/'));
                    string error = AssetDatabase.MoveAsset(move.source, move.destination);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(move.source + " -> " + move.destination + ": " + error);
                    result.moved++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            EditorBuildSettings.scenes = builds.Select(s => new EditorBuildSettingsScene(AssetDatabase.GUIDToAssetPath(s.guid), s.enabled)).ToArray();
            AssetDatabase.Refresh();
            result.success = true;
        }
        catch (Exception error) { result.error = error.ToString(); Debug.LogException(error); }
        finally
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Root + "result.json", JsonUtility.ToJson(result, true));
            Debug.Log("Folder organization: " + JsonUtility.ToJson(result));
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        if (string.IsNullOrEmpty(guid)) throw new IOException("Could not create " + folder);
    }
}
#endif
