using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>操作中の加護を中央に拡大表示するView。ToggleやRunSessionの値は変更しない。</summary>
public sealed class RunSetupBlessingPreview : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public UnityEngine.UI.Selectable Option;
        public TMP_Text Symbol;
        public TMP_Text Name;
        public TMP_Text Effect;
    }

    [SerializeField] private TMP_Text _symbol;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _effect;
    [SerializeField] private Entry[] _entries;
    private GameObject _lastFocus;

    private void OnEnable()
    {
        _lastFocus = null;
        if (_entries != null && _entries.Length > 0) Show(_entries[0].Option);
    }

    private void LateUpdate()
    {
        var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (focus == _lastFocus) return;
        _lastFocus = focus;
        RestoreFocus();
    }

    public void RestoreFocus()
    {
        var focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (focus != null) Show(focus.GetComponent<UnityEngine.UI.Selectable>());
    }

    public void Show(UnityEngine.UI.Selectable option)
    {
        if (option == null || _entries == null) return;
        foreach (var entry in _entries)
        {
            if (entry.Option != option || entry.Symbol == null || entry.Name == null || entry.Effect == null) continue;
            if (_symbol != null) _symbol.text = entry.Symbol.text;
            if (_name != null) _name.text = entry.Name.text;
            if (_effect != null) _effect.text = entry.Effect.text;
            return;
        }
    }
}
