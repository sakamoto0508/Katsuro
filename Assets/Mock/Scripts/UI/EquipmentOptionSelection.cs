using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>方向入力による選択先の移動でも、クリックと同様に装備を選択する。</summary>
[RequireComponent(typeof(Toggle))]
public sealed class EquipmentOptionSelection : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        var option = GetComponent<Toggle>();
        if (option.IsActive() && option.IsInteractable()) option.isOn = true;
    }
}
