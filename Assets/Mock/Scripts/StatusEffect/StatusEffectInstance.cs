using UnityEngine;

/// <summary>付与された効果の定義・残り時間・スタック数を個別に保持するランタイムデータ。</summary>
public class StatusEffectInstance
{
    public StatusEffectDef Def { get; }
    public float Remaining { get; set; }
    public int Stacks { get; set; }
    public GameObject Source { get; }
    public GameObject Vfx { get; set; }

    /// <summary>効果定義と付与元を保持し、残り時間・一段階のスタックと未生成VFXを初期化する。</summary>
    public StatusEffectInstance(StatusEffectDef def, GameObject source)
    {
        Def = def;
        Source = source;
        Remaining = def.Duration;
        Stacks = 1;
        Vfx = null;
    }
}
