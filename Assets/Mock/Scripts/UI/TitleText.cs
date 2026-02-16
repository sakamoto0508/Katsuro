using DG.Tweening;
using TMPro;
using UnityEngine;

public class TitleText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private Ease _ease= Ease.InOutSine;

    private void Start()
    {
        _text.alpha = 0f;
        Sequence seq = DOTween.Sequence();

        seq.Append(_text.DOFade(1f, _fadeInDuration).SetEase(_ease));
        seq.AppendInterval(0.5f);
        seq.Append(_text.DOFade(0f, _fadeOutDuration).SetEase(_ease));

        seq.SetLoops(-1);
    }
}
