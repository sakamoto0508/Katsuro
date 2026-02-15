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
        // BGM 用 AudioSource を準備する。
        EnsureBGMSource();

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

    private void EnsureBGMSource()
    {
        // bgmSource が未設定なら生成して親に配置する。
        if (bgmSource == null || bgmSource.Equals(null))
        {
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
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


    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // BGM 用 AudioSource を用意する。
        EnsureBGMSource();
        if (bgmSource == null)
        {
            Debug.LogError("bgmSource が作成されていません。");
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

        // 既に再生中なら処理しない。
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
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
    /// BGM を停止する。
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null && !bgmSource.Equals(null))
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    /// <summary>
    /// BGM の音量を設定する。
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        EnsureBGMSource();
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
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
        StopBGM();
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