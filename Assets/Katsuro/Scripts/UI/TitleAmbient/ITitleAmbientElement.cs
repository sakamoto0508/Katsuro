using UnityEngine;

/// <summary>TitleAmbientDirectorが毎フレーム各演出へ渡す、風と強度の共有状態。</summary>
public readonly struct TitleAmbientContext
{
    /// <summary>前フレームからの経過時間（実時間の秒）。</summary>
    public readonly float DeltaTime;
    /// <summary>演出開始からの累積時間（実時間の秒）。</summary>
    public readonly float Time;
    /// <summary>補間済みの風ベクトル（Canvas基準解像度のpx/秒）。</summary>
    public readonly Vector2 Wind;
    /// <summary>突風の強さ。0で基本風速、1で最大突風。</summary>
    public readonly float Gust;
    /// <summary>演出全体の強度。開始時のフェードインと外部からの抑制に使う。</summary>
    public readonly float Intensity;

    /// <summary>1フレーム分の共有状態を作る。</summary>
    public TitleAmbientContext(float deltaTime, float time, Vector2 wind, float gust, float intensity)
    {
        DeltaTime = deltaTime;
        Time = time;
        Wind = wind;
        Gust = gust;
        Intensity = intensity;
    }
}

/// <summary>
/// タイトル環境演出の1要素。TitleAmbientDirectorの子に置くと自動で登録され、風の更新後に呼ばれる。
/// 呼吸・布揺れ・血の滴下など後続の演出もこれを実装すれば同じ風と強度を共有できる。
/// </summary>
public interface ITitleAmbientElement
{
    /// <summary>共有状態を受け取り、1フレーム分の演出を進める。</summary>
    void Tick(in TitleAmbientContext context);
}
