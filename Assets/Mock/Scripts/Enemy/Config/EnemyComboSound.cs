using System;
using UnityEngine;

/// <summary>重ねて鳴らす1音分のClip・音量・再生開始位置。重さの差は素材の重ね方で作る。</summary>
[Serializable]
public sealed class ComboSoundLayer
{
    /// <summary>再生するAudioClip。未設定の層は鳴らさない。</summary>
    [UnityEngine.Tooltip("再生するAudioClip。未設定の層は鳴らさない。")]
    public AudioClip Clip;
    /// <summary>この層の基準音量。0で無音、1で最大。ユーザーのMaster/SE音量をさらに掛ける。</summary>
    [UnityEngine.Tooltip("この層の基準音量。0で無音、1で最大。ユーザーのMaster/SE音量をさらに掛ける。")]
    [Range(0f, 1f)] public float Volume = 1f;
    /// <summary>素材先頭の間を除く再生開始位置（Clip秒）。再生を遅らせる時間ではない。</summary>
    [UnityEngine.Tooltip("素材先頭の間を除く再生開始位置（Clip秒）。再生を遅らせる時間ではない。")]
    [Min(0f)] public float StartOffset;
}

/// <summary>敵コンボ1段分の振り抜き音と命中音。段番号はAnimation EventのintParameter（1始まり）に対応する。</summary>
[Serializable]
public sealed class EnemyComboStageSound
{
    /// <summary>Inspectorで段を識別するラベル。再生には使用しない。</summary>
    [UnityEngine.Tooltip("Inspectorで段を識別するラベル。再生には使用しない。")]
    public string Label;
    /// <summary>攻撃判定が開いた瞬間に鳴らす振り抜き音。空なら既存ComboSound Eventの登録名を使う。</summary>
    [UnityEngine.Tooltip("攻撃判定が開いた瞬間に鳴らす振り抜き音。空なら既存ComboSound Eventの登録名を使う。")]
    public ComboSoundLayer[] Swing = Array.Empty<ComboSoundLayer>();
    /// <summary>この段がPlayerへ実際に命中した時だけ鳴らす音。空振り・回避では鳴らない。空なら通常の命中音を使う。</summary>
    [UnityEngine.Tooltip("この段がPlayerへ実際に命中した時だけ鳴らす音。空振り・回避では鳴らない。空なら通常の命中音を使う。")]
    public ComboSoundLayer[] Hit = Array.Empty<ComboSoundLayer>();

    /// <summary>有効なClipを持つ層が1つ以上あるか。</summary>
    public static bool HasAny(ComboSoundLayer[] layers)
    {
        if (layers == null) return false;
        foreach (var layer in layers) if (layer != null && layer.Clip != null && layer.Volume > 0f) return true;
        return false;
    }

    /// <summary>有効な層をすべて同時に既存SEプールへ要求する。ユーザー音量とMixer経路はAudioManagerに任せる。</summary>
    /// <returns>1層以上を再生要求できればtrue。</returns>
    public static bool Play(AudioManager audio, ComboSoundLayer[] layers)
    {
        if (audio == null || !HasAny(layers)) return false;
        foreach (var layer in layers)
            if (layer != null && layer.Clip != null && layer.Volume > 0f) audio.PlaySE(layer.Clip, layer.Volume, layer.StartOffset);
        return true;
    }
}
