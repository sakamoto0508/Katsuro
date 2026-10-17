using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class RestoreCounterSlashMicroTuneValues
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required; Play Mode was not changed.");
        var changes=JArray.Parse(File.ReadAllText("Artifacts/CounterSlashMicroTune/Changes.json"));
        foreach(var change in changes)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>((string)change["path"]);
            if(mat==null||!mat.HasProperty((string)change["property"]))throw new Exception("Material property missing");
            mat.SetFloat((string)change["property"],(float)change["before"]);
            EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        }
        return new{restored=changes.Count};
    }
}
