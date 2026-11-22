using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Character Mesh Spawn ブロックを使うオーラ系 VFX Graph を、ブロック → グラフの順で再インポートする。
/// エディタ起動時・オーラ関連アセットが再インポートされた後・ビルド前に実行し、粒子が手元の1頂点に集まる不具合を防ぐ。
/// </summary>
[InitializeOnLoad]
internal static class AuraGraphReimporter
{
    /// <summary>全身サンプリングを行う Character Mesh Spawn.vfxblock の GUID。</summary>
    private const string MeshSpawnBlockGuid = "20545407f52b9a14d850f9745e1b3986";

    /// <summary>
    /// ゲーム内で使っていて必ず作り直すグラフの GUID。
    /// Local Connect: 敵ダークオーラ(Dark)・Poison / Smoke: プレイヤーのバフオーラ(Smoke 1)・Local Smoke 2 / Fire: Arcane Fire。
    /// </summary>
    private static readonly string[] InGameGraphGuids =
    {
        "d3af3603fa3c5fe4e966293013f961ea",
        "1a59a2425359f264f937feb99eb40f72",
        "54cb6e382a61bba47a73f0ea0b52bf66",
    };

    /// <summary>このエディタセッションで起動時の再インポートを済ませたかを保持するキー。</summary>
    private const string SessionDoneKey = "Katsuro.AuraGraphReimporter.SessionDone";

    /// <summary>自前の再インポート直後に届くインポート通知を無視する秒数。</summary>
    private const double SelfImportIgnoreSeconds = 3d;

    /// <summary>同期かつ強制で再インポートするオプション。</summary>
    private const ImportAssetOptions ReimportOptions = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;

    /// <summary>再インポート待ちの間は true。</summary>
    private static bool _pending;

    /// <summary>最後に自前で再インポートした時刻。</summary>
    private static double _lastReimportTime = double.NegativeInfinity;

    static AuraGraphReimporter()
    {
        // 起動直後の早いインポートで型が未登録のままコンパイルされている可能性があるため、セッションごとに1回作り直す
        if (!SessionState.GetBool(SessionDoneKey, false)) Schedule();
    }

    /// <summary>
    /// 再インポートを予約する。コンパイル中・Play中は終わるまで待つ。
    /// </summary>
    internal static void Schedule()
    {
        if (_pending) return;
        _pending = true;
        EditorApplication.update += WaitAndReimport;
    }

    /// <summary>
    /// 自前の再インポート直後かどうか。インポート通知による無限ループを防ぐ。
    /// </summary>
    internal static bool IsSelfImport => EditorApplication.timeSinceStartup - _lastReimportTime < SelfImportIgnoreSeconds;

    /// <summary>
    /// 再インポート対象かどうかを判定する。
    /// </summary>
    /// <returns>Mesh Spawn ブロックまたはゲーム内のオーラグラフなら true。</returns>
    internal static bool IsTarget(string path)
    {
        var guid = AssetDatabase.AssetPathToGUID(path);
        return guid == MeshSpawnBlockGuid || InGameGraphGuids.Contains(guid);
    }

    /// <summary>
    /// エディタが落ち着くまで待ってから再インポートする。
    /// </summary>
    private static void WaitAndReimport()
    {
        // 型が読み込まれる前に作り直すと同じ不具合になるので、コンパイル・インポート・Play が終わるまで待つ
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= WaitAndReimport;
        _pending = false;
        Reimport();
        SessionState.SetBool(SessionDoneKey, true);
    }

    /// <summary>
    /// オーラ関連アセットを順に再インポートする。
    /// </summary>
    [MenuItem("Katsuro/VFX/Reimport Aura Graphs")]
    internal static void Reimport()
    {
        var meshSpawnBlock = AssetDatabase.GUIDToAssetPath(MeshSpawnBlockGuid);
        var inGameGraphs = InGameGraphGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        if (string.IsNullOrEmpty(meshSpawnBlock) || inGameGraphs.Any(string.IsNullOrEmpty))
        {
            // 黙って抜けると修正が効いていないことに気付けないので警告を出す
            Debug.LogWarning("[AuraGraphReimporter] オーラ関連アセットが見つからないため再インポートをスキップしました。GUID を確認してください。");
            return;
        }

        _lastReimportTime = EditorApplication.timeSinceStartup;

        // 早い段階の VFX インポートは BarycentricTriangleSampling 型の読み込み前に走ることがあり、
        // その場合バッファ参照が抜けて全粒子が同じ頂点(手元)に集まる。ブロック → グラフの順で作り直す。
        AssetDatabase.ImportAsset(meshSpawnBlock, ReimportOptions);

        // 依存関係の取得漏れがあってもゲーム内のグラフは確実に作り直せるよう、明示リストと依存検索を合わせる
        var graphs = AssetDatabase.FindAssets("t:VisualEffectAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => AssetDatabase.GetDependencies(path, false).Contains(meshSpawnBlock))
            .Union(inGameGraphs);
        foreach (var graph in graphs)
            AssetDatabase.ImportAsset(graph, ReimportOptions);

        _lastReimportTime = EditorApplication.timeSinceStartup;
        Debug.Log("[AuraGraphReimporter] オーラ関連アセットを再インポートしました。");
    }
}

/// <summary>
/// オーラ関連アセットが Unity によって再インポートされたら、順序どおりの再インポートを予約する。
/// </summary>
internal sealed class AuraGraphImportWatcher : AssetPostprocessor
{
    /// <summary>
    /// インポート完了時に呼ばれる。途中で単体インポートされて壊れたグラフを作り直すために使う。
    /// </summary>
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (AuraGraphReimporter.IsSelfImport) return;
        if (importedAssets.Any(AuraGraphReimporter.IsTarget)) AuraGraphReimporter.Schedule();
    }
}
