using System;
using UnityEngine;

/// <summary>
/// Assets/ArtのMesh VFX（一閃・ワープ）を、パーツごとの短い時間エンベロープで再生する。
/// FeedbackVisualTime基準なのでPause中は止まる。Instantiateしたものを使い回す前提。
/// </summary>
[DisallowMultipleComponent]
public sealed class KatsuroTimedVFX : MonoBehaviour
{
    [Serializable]
    public sealed class Track
    {
        [Tooltip("制御するパーツのRenderer。Katsuro/VFXAdditive または Katsuro/VFXSmoke を使う。")]
        public Renderer Renderer;
        [Min(0f), Tooltip("再生開始からの遅延（秒）。")]
        public float Delay;
        [Min(.001f), Tooltip("立ち上がり（秒）。Wipe有効時はこの間に尾から先端へ走る。")]
        public float FadeIn = .02f;
        [Min(0f), Tooltip("最大値を保つ時間（秒）。")]
        public float Hold = .03f;
        [Min(.001f), Tooltip("消えるまでの時間（秒）。Smokeでは侵食して消える。")]
        public float FadeOut = .08f;
        [Min(0f), Tooltip("加算パーツは_Intensity、Smokeは_Alphaの最大値。")]
        public float Peak = 1f;
        [Tooltip("Katsuro/VFXSmokeとして_Alpha/_Erode/_Scrollを駆動する。")]
        public bool Smoke;
        [Tooltip("加算パーツをUV.xの尾→先端へ走らせる（_Progress）。")]
        public bool Wipe;
        [Tooltip("Smokeの上方向への流れ量（再生全体で0→この値）。")]
        public float Scroll;
        [Tooltip("再生全体でのローカルスケール倍率（開始→終了）。")]
        public Vector3 ScaleFrom = Vector3.one, ScaleTo = Vector3.one;
        [Tooltip("ONで加速しながら変化（収束用）。OFFで減速しながら変化（広がり用）。")]
        public bool EaseIn;
        public float End => Delay + FadeIn + Hold + FadeOut;
    }

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int ErodeId = Shader.PropertyToID("_Erode");
    private static readonly int ScrollId = Shader.PropertyToID("_Scroll");

    [SerializeField] private Track[] _tracks = Array.Empty<Track>();
    private MaterialPropertyBlock _block;
    private Vector3[] _baseScales;
    private float _started;
    private bool _playing;
    private float _intensityMultiplier = 1f, _timeScale = 1f;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Color[] _baseColors;
    private Color _tint = Color.white;
    private Transform _follow;
    private Vector3 _followOffset;
    private bool _faceCamera;
    private float _holdUntil, _heldTime, _holdBoost = 1f;

    /// <summary>全パーツが消え終わるまでの秒数。</summary>
    public float Duration
    {
        get
        {
            float end = 0f;
            foreach (var track in _tracks) if (track != null) end = Mathf.Max(end, track.End);
            return end;
        }
    }

    public bool IsPlaying => _playing;

    private void Awake()
    {
        Prepare();
        if (!_playing) HideAll();
    }

    /// <summary>位置・向き・一様スケールを決めて最初から再生する。向きはローカルX=斬撃方向、Z=正面法線。</summary>
    public void Play(Vector3 position, Quaternion rotation, float scale = 1f, float emission = 1f, float duration = 0f)
    {
        Prepare();
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = Vector3.one * Mathf.Max(.01f, scale);
        _intensityMultiplier = Mathf.Max(0f, emission);
        _timeScale = duration > 0f ? Mathf.Max(.01f, duration) / Mathf.Max(.001f, Duration) : 1f;
        _started = FeedbackVisualTime.Now;
        _playing = true;
        gameObject.SetActive(true);
        Apply(0f);
    }

    /// <summary>再生中のまま位置だけ移す（出現先の再検証など）。</summary>
    public void MoveTo(Vector3 position) => transform.position = position;

    /// <summary>全パーツの色に掛ける倍率（各Materialの_Colorに乗算）。次の再生まで保持する。</summary>
    public void SetTint(Color tint) => _tint = tint;

    /// <summary>再生中、指定Transformへ追従させる（刀先など）。faceCameraで面を常にカメラへ向ける。nullで解除。</summary>
    public void Follow(Transform target, Vector3 worldOffset = default, bool faceCamera = false)
    {
        _follow = target; _followOffset = worldOffset; _faceCamera = faceCamera;
    }

    /// <summary>今の見た目のまま指定秒数止める（命中時のHitStopと同期）。boostで保持中の明るさを上げる。</summary>
    public void HoldFor(float seconds, float boost = 1f)
    {
        if (!_playing || seconds <= 0f) return;
        float now = FeedbackVisualTime.Now;
        if (now >= _holdUntil) _heldTime = (now - _started) / _timeScale;
        _holdUntil = Mathf.Max(_holdUntil, now + seconds);
        _holdBoost = Mathf.Max(0f, boost);
    }

    public void Stop()
    {
        _playing = false;
        _holdUntil = 0f;
        _follow = null;
        HideAll();
    }

    private void LateUpdate()
    {
        if (!_playing) return;
        if (_follow != null)
        {
            transform.position = _follow.position + _followOffset;
            var view = _faceCamera ? Camera.main : null;
            if (view != null) transform.rotation = KatsuroVFXOrientation.FacingCamera(transform.position, view);
        }
        float now = FeedbackVisualTime.Now;
        if (_holdUntil > 0f)
        {
            if (now < _holdUntil) { Apply(_heldTime, _holdBoost); return; }
            _started = _holdUntil - _heldTime * _timeScale;   // resume exactly where the hold began
            _holdUntil = 0f;
        }
        float t = (now - _started) / _timeScale;
        if (t >= Duration) { Stop(); return; }
        Apply(t);
    }

    private void Prepare()
    {
        if (_block != null) return;
        _block = new MaterialPropertyBlock();
        _baseScales = new Vector3[_tracks.Length];
        _baseColors = new Color[_tracks.Length];
        for (int i = 0; i < _tracks.Length; i++)
        {
            var renderer = _tracks[i] != null ? _tracks[i].Renderer : null;
            _baseScales[i] = renderer != null ? renderer.transform.localScale : Vector3.one;
            var material = renderer != null ? renderer.sharedMaterial : null;
            _baseColors[i] = material != null && material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        }
    }

    private void Apply(float time, float boost = 1f)
    {
        for (int i = 0; i < _tracks.Length; i++)
        {
            var track = _tracks[i];
            if (track == null || track.Renderer == null) continue;
            float t = time - track.Delay;
            if (t < 0f || t >= track.FadeIn + track.Hold + track.FadeOut) { track.Renderer.enabled = false; continue; }
            float rise = Mathf.Clamp01(t / track.FadeIn);
            float fall = Mathf.Clamp01((t - track.FadeIn - track.Hold) / track.FadeOut);
            float life = Mathf.Clamp01(t / (track.FadeIn + track.Hold + track.FadeOut));
            float shaped = track.EaseIn ? life * life : 1f - (1f - life) * (1f - life);
            track.Renderer.transform.localScale = Vector3.Scale(_baseScales[i], Vector3.LerpUnclamped(track.ScaleFrom, track.ScaleTo, shaped));
            track.Renderer.GetPropertyBlock(_block);
            if (track.Smoke)
            {
                // Smoke thickens quickly, then thin wisps erode away before the dense cores.
                _block.SetFloat(AlphaId, track.Peak * Mathf.SmoothStep(0f, 1f, rise));
                _block.SetFloat(ErodeId, Mathf.Lerp(0f, 1.1f, fall));
                _block.SetFloat(ScrollId, track.Scroll * shaped);
            }
            else
            {
                float envelope = (1f - (1f - rise) * (1f - rise)) * (1f - Mathf.SmoothStep(0f, 1f, fall));
                _block.SetFloat(IntensityId, track.Peak * envelope * _intensityMultiplier * boost);
                _block.SetFloat(ProgressId, track.Wipe ? Mathf.Lerp(0f, 1.2f, rise) : 1.2f);
            }
            _block.SetColor(ColorId, _baseColors[i] * _tint);
            track.Renderer.SetPropertyBlock(_block);
            track.Renderer.enabled = true;
        }
    }

    private void HideAll()
    {
        foreach (var track in _tracks) if (track != null && track.Renderer != null) track.Renderer.enabled = false;
    }

#if UNITY_EDITOR
    /// <summary>エディタのセットアップツールからパーツ構成を書き込む。</summary>
    public void EditorSetTracks(Track[] tracks) => _tracks = tracks ?? Array.Empty<Track>();
#endif
}

/// <summary>Art VFXの向きを決める。ローカルX=斬撃方向、ローカルZ=カメラ側を向く面法線。</summary>
public static class KatsuroVFXOrientation
{
    /// <summary>斬撃方向を画面の平面へ投影し、面をカメラへ向けた回転を返す。方向が視線と平行なら画面右を使う。</summary>
    public static Quaternion FacingCamera(Vector3 position, Vector3 slashDirection, Camera view)
    {
        Vector3 toCamera = view.transform.position - position;
        if (toCamera.sqrMagnitude < .0001f) toCamera = -view.transform.forward;
        toCamera.Normalize();
        Vector3 along = Vector3.ProjectOnPlane(slashDirection, toCamera);
        if (along.sqrMagnitude < .0001f) along = Vector3.ProjectOnPlane(view.transform.right, toCamera);
        along.Normalize();
        return Quaternion.LookRotation(toCamera, Vector3.Cross(toCamera, along));
    }

    /// <summary>出現の光など向きのないパーツ用。面をカメラへ向け、ローカルXを画面の水平に合わせる。</summary>
    public static Quaternion FacingCamera(Vector3 position, Camera view) =>
        FacingCamera(position, view.transform.right, view);
}
