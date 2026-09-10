using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Navigation focus selects equipment just as a pointer click does.</summary>
[RequireComponent(typeof(Toggle))]
public sealed class EquipmentOptionSelection : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        var option = GetComponent<Toggle>();
        if (option.IsActive() && option.IsInteractable()) option.isOn = true;
    }
}
