using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タイトル画面の環境演出全体を制御する。風を先に更新し、子に置いた ITitleAmbientElement へ同じ状態を配る。
/// 入力・画面遷移はTitleManagerが持ち、ここは見た目だけを扱う。
/// </summary>
public sealed class TitleAmbientDirector : MonoBehaviour
{
    /// <summary>霧と火の粉が共有する風。</summary>
    [UnityEngine.Tooltip("霧と火の粉が共有する風。")]
    [SerializeField] private TitleWind _wind;
    /// <summary>演出全体の強度。0で全演出を消し、1で設定どおりに表示する。</summary>
    [UnityEngine.Tooltip("演出全体の強度。0で全演出を消し、1で設定どおりに表示する。")]
    [SerializeField, Range(0f, 1f)] private float _intensity = 1f;
    /// <summary>強度が0から1まで変化するのに掛かる時間（実時間の秒）。</summary>
    [UnityEngine.Tooltip("強度が0から1まで変化するのに掛かる時間（実時間の秒）。")]
    [SerializeField, Min(0.01f)] private float _intensityFadeTime = 2f;
    /// <summary>シーン開始時に強度0から緩やかに立ち上げるか。</summary>
    [UnityEngine.Tooltip("シーン開始時に強度0から緩やかに立ち上げるか。")]
    [SerializeField] private bool _fadeInOnStart = true;

    private readonly List<ITitleAmbientElement> _elements = new List<ITitleAmbientElement>();
    private float _currentIntensity;
    private float _time;
    private bool _initialized;

    /// <summary>現在の補間済み強度。</summary>
    public float CurrentIntensity => _currentIntensity;
    /// <summary>更新対象として登録済みの演出要素数。</summary>
    public int ElementCount => _elements.Count;
    /// <summary>登録済みの演出要素を順に参照する。</summary>
    public ITitleAmbientElement GetElement(int index) => _elements[index];
    /// <summary>現在の風。</summary>
    public TitleWind Wind => _wind;

    /// <summary>子の演出要素を集め、風を初期化する。</summary>
    private void Awake() => Init();

    /// <summary>演出要素の登録と初期強度の設定を一度だけ行う。</summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (_wind == null) _wind = GetComponent<TitleWind>();
        _wind?.Init();
        // 非表示の要素も登録しておき、後から有効化しても更新対象に入るようにする
        GetComponentsInChildren(true, _elements);
        _currentIntensity = _fadeInOnStart ? 0f : _intensity;
        // スクリプト参照が外れると演出が無言で止まるため、原因を追えるよう一度だけ知らせる
        if (_elements.Count == 0) Debug.LogWarning("TitleAmbientDirector: 子に演出要素が見つかりません。スクリプト参照を確認してください。", this);
        if (_wind == null) Debug.LogWarning("TitleAmbientDirector: TitleWindが未設定のため無風で動作します。", this);
    }

    /// <summary>実行中に生成した演出要素を追加する。</summary>
    public void Register(ITitleAmbientElement element)
    {
        if (element != null && !_elements.Contains(element)) _elements.Add(element);
    }

    /// <summary>破棄する演出要素を更新対象から外す。</summary>
    public void Unregister(ITitleAmbientElement element) => _elements.Remove(element);

    /// <summary>演出全体の目標強度を変更する。実際の値は _intensityFadeTime で補間される。</summary>
    public void SetIntensity(float intensity) => _intensity = Mathf.Clamp01(intensity);

    /// <summary>風→各演出の順に更新する。タイトルはtimeScaleに依存させないため実時間で進める。</summary>
    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        _time += deltaTime;
        _currentIntensity = Mathf.MoveTowards(_currentIntensity, _intensity, deltaTime / _intensityFadeTime);
        _wind?.Tick(deltaTime);
        var context = new TitleAmbientContext(deltaTime, _time,
            _wind != null ? _wind.Current : Vector2.zero, _wind != null ? _wind.Gust : 0f, _currentIntensity);
        for (int i = 0; i < _elements.Count; i++) _elements[i].Tick(in context);
    }
}
