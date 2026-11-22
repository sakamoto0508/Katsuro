using UnityEditor.Build;

/// <summary>
/// ビルド前にオーラ系 VFX Graph を順に再インポートする(処理本体は <see cref="AuraGraphReimporter"/>)。
/// </summary>
internal sealed class EnemyDarkAuraBuildProcessor : BuildPlayerProcessor
{
    /// <summary>
    /// ビルド開始前に呼ばれ、オーラ関連アセットを順に再インポートする。
    /// </summary>
    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext) => AuraGraphReimporter.Reimport();
}
