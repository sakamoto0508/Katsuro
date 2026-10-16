using UnityEditor;
public static class CounterSlashRefresh
{
    public static object Main()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return new { playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling };
    }
}
