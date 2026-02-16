using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundData
{
    public string name;      // サウンドの名前（識別用）。
    public AudioClip clip;   // 再生する AudioClip。
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public AudioConfig AudioConfig => _audioConfig; 

    [Header("BGM 用 AudioSource")]
    [SerializeField] private AudioSource bgmSource;
    [Header("BGM 用 AudioSources (複数チャンネル対応)")]
    [SerializeField] private List<AudioSource> bgmSources = new List<AudioSource>();
    [Header("SE 用 AudioSource (Prefab)")]
    [SerializeField] private AudioSource sfxSourcePrefab;
    [Header("BGM リスト")]
    [SerializeField] private List<SoundData> bgmList = new List<SoundData>();
    [Header("SE リスト")]
    [SerializeField] private List<SoundData> seList = new List<SoundData>();
    [Header("SFX プールサイズ")]
    [SerializeField] private int sfxPoolSize = 10;
    [Header("オーディオ設定")]
    [SerializeField] private AudioConfig _audioConfig;
    [SerializeField] private AudioListener _audioListener;

    private Dictionary<string, AudioClip> _bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> _seDict = new Dictionary<string, AudioClip>();
    private List<AudioSource> _sfxPool = new List<AudioSource>();

    private void Awake()
    {
        // シングルトン初期化。
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeAudioManager()
    {
        // BGM 用 AudioSource を準備する（複数チャンネル対応）。
        EnsureBGMSources();

        // BGM リストを辞書に登録。
        _bgmDict.Clear();
        foreach (var bgm in bgmList)
        {
            if (!_bgmDict.ContainsKey(bgm.name) && bgm.clip != null)
            {
                _bgmDict.Add(bgm.name, bgm.clip);
            }
        }

        // SE リストを辞書に登録。
        _seDict.Clear();
        foreach (var se in seList)
        {
            if (!_seDict.ContainsKey(se.name) && se.clip != null)
            {
                _seDict.Add(se.name, se.clip);
            }
        }

        // SFX 用のプールを生成。
        CreateSFXPool();
    }

    private void EnsureBGMSources()
    {
        // bgmSources リストを保証し、既存の単一 bgmSource が割り当てられている場合はそれを利用する。
        if (bgmSources == null) bgmSources = new List<AudioSource>();

        // 既存の inspector で設定された単一 bgmSource があり、リストが空なら追加する。
        if ((bgmSource != null && !bgmSource.Equals(null)) && bgmSources.Count == 0)
        {
            bgmSources.Add(bgmSource);
        }

        // 少なくとも 1 つの BGM ソースが存在しなければ生成する。
        if (bgmSources.Count == 0)
        {
            GameObject bgmObj = new GameObject("BGM_Source_0");
            bgmObj.transform.SetParent(transform);
            var source = bgmObj.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            bgmSources.Add(source);
            // 保守のため古い単一参照にもセットしておく。
            bgmSource = source;
        }
    }

    private AudioSource GetBgmSource(int channel)
    {
        EnsureBGMSources();
        if (channel < 0) channel = 0;
        // チャンネルがリスト範囲外の場合は必要分のソースを生成する。
        while (channel >= bgmSources.Count)
        {
            int idx = bgmSources.Count;
            GameObject bgmObj = new GameObject($"BGM_Source_{idx}");
            bgmObj.transform.SetParent(transform);
            var source = bgmObj.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            bgmSources.Add(source);
        }
        return bgmSources[channel];
    }

    private void CreateSFXPool()
    {
        // 既存のプールを破棄してから再生成する。
        foreach (var source in _sfxPool)
        {
            if (source != null)
            {
                DestroyImmediate(source.gameObject);
            }
        }
        _sfxPool.Clear();

        // 指定数だけ SFX 用 AudioSource を生成してプールに追加する。
        for (int i = 0; i < sfxPoolSize; i++)
        {
            if (sfxSourcePrefab != null)
            {
                var sfxSource = Instantiate(sfxSourcePrefab, transform);
                sfxSource.playOnAwake = false;
                sfxSource.loop = false; // SFX はループしない。
                sfxSource.clip = null;  // クリップは後で設定する。
                _sfxPool.Add(sfxSource);
            }
        }
    }


    public void PlayBGM(AudioClip clip, int channel = 0, float volume = 1f)
    {
        if (clip == null) return;
        var source = GetBgmSource(channel);
        if (source == null)
        {
            Debug.LogError("指定チャンネルの BGM ソースが作成されていません。 channel=" + channel);
            return;
        }

        // 同じクリップを別の SFX ソースが再生している場合は停止する。
        foreach (var sfx in _sfxPool)
        {
            if (sfx != null && sfx.isPlaying && sfx.clip == clip)
            {
                sfx.Stop();
                sfx.clip = null;
            }
        }

        // 既に指定チャンネルで同じクリップ・同じ音量で再生中なら処理しない。
        float clampedVolume = Mathf.Clamp01(volume);
        if (source.clip == clip && source.isPlaying && Mathf.Approximately(source.volume, clampedVolume)) return;

        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.volume = clampedVolume;
        source.Play();
    }


    /// <summary>
    /// BGM を名前で再生する。
    /// </summary>
    public void PlayBGM(string bgmName)
    {
        if (_bgmDict.TryGetValue(bgmName, out var clip))
        {
            PlayBGM(clip);
        }
        else
        {
            Debug.LogWarning($"指定された BGM '{bgmName}' が見つかりません。");
        }
    }

    /// <summary>
    /// 既存互換: 名前とボリューム指定で再生 (チャンネルはデフォルト 0)
    /// </summary>
    public void PlayBGM(string bgmName, float volume)
    {
        PlayBGM(bgmName, 0, volume);
    }

    /// <summary>
    /// 名前で指定して BGM を再生します。チャンネルとボリュームを指定可能（既定は channel=0, volume=1）。
    /// </summary>
    public void PlayBGM(string bgmName, int channel = 0, float volume = 1f)
    {
        if (_bgmDict.TryGetValue(bgmName, out var clip))
        {
            PlayBGM(clip, channel, volume);
        }
        else
        {
            Debug.LogWarning($"指定された BGM '{bgmName}' が見つかりません。");
        }
    }

    /// <summary>
    /// BGM を停止する。
    /// </summary>
    public void StopBGM()
    {
        // 既存呼び出し互換: デフォルトチャンネル 0 を停止
        StopBGM(0);
    }

    /// <summary>
    /// 指定チャンネルの BGM を停止します。
    /// </summary>
    public void StopBGM(int channel)
    {
        var src = GetBgmSource(channel);
        if (src != null)
        {
            src.Stop();
            src.clip = null;
        }
    }

    /// <summary>
    /// すべての BGM チャンネルを停止します。
    /// </summary>
    public void StopAllBGMs()
    {
        if (bgmSources == null) return;
        foreach (var s in bgmSources)
        {
            if (s != null)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }

    /// <summary>
    /// BGM の音量を設定する。
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        // 既存呼び出し互換: チャンネル 0 を設定
        SetBGMVolume(volume, 0);
    }

    /// <summary>
    /// 指定チャンネルまたは全チャンネルの BGM 音量を設定する。
    /// channel が -1 の場合は全チャンネルに適用。
    /// </summary>
    public void SetBGMVolume(float volume, int channel)
    {
        volume = Mathf.Clamp01(volume);
        if (channel < 0)
        {
            if (bgmSources == null) return;
            foreach (var s in bgmSources)
            {
                if (s != null) s.volume = volume;
            }
        }
        else
        {
            var src = GetBgmSource(channel);
            if (src != null) src.volume = volume;
        }
    }

    /// <summary>
    /// SE を名前で再生する。
    /// </summary>
    public void PlaySE(string seName, float volume = 1f)
    {
        if (_seDict.TryGetValue(seName, out var clip))
        {
            var src = GetAvailableSfxSource();
            if (src != null)
            {
                src.clip = clip;
                src.volume = Mathf.Clamp01(volume);
                src.Play();
            }
        }
        else
        {
            Debug.LogWarning($"指定された SE '{seName}' が見つかりません。");
        }
    }

    /// <summary>
    /// 利用可能な SFX 用 AudioSource を取得する。
    /// </summary>
    private AudioSource GetAvailableSfxSource()
    {
        foreach (var s in _sfxPool)
        {
            if (s != null && !s.isPlaying) return s;
        }

        // 必要なら予備の SFX ソースを動的に生成する。
        if (sfxSourcePrefab != null)
        {
            var extra = Instantiate(sfxSourcePrefab, transform);
            extra.playOnAwake = false;
            _sfxPool.Add(extra);
            return extra;
        }

        return null;
    }

    /// <summary>
    /// SE の音量を設定する。
    /// </summary>
    public void SetSEVolume(float volume)
    {
        foreach (var s in _sfxPool)
        {
            if (s != null)
            {
                s.volume = Mathf.Clamp01(volume);
            }
        }
    }

    /// <summary>
    /// AudioListener に対してローパスフィルタを適用します（cutoff に Hz を指定）。
    /// </summary>
    public void ApplyLowPassToListener(float cutoffFrequency)
    {
        if (_audioListener == null) return;
        var filter = _audioListener.GetComponent<AudioLowPassFilter>();
        if (filter == null) filter = _audioListener.gameObject.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = cutoffFrequency;
    }

    /// <summary>
    /// AudioListener のローパスフィルタを削除します。
    /// </summary>
    public void RemoveLowPassFromListener()
    {
        if (_audioListener == null) return;
        var filter = _audioListener.GetComponent<AudioLowPassFilter>();
        if (filter != null) Destroy(filter);
    }

    /// <summary>
    /// 即座に全てのオーディオを停止します（BGM と SFX）。
    /// </summary>
    public void StopAllAudioImmediate()
    {
        StopAllBGMs();
        foreach (var s in _sfxPool)
        {
            if (s != null)
            {
                s.Stop();
                s.clip = null;
            }
        }
    }
}