using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
public static class VerifySetupV3
{
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var ui=UnityEngine.Object.FindAnyObjectByType<RunSetupUI>();var body=ui.transform.Find("Content/Body");var panel=body.Find("FocusPreview");
        var name=panel.Find("Name").GetComponent<TMP_Text>();var effect=panel.Find("Effect").GetComponent<TMP_Text>();var symbol=panel.Find("Symbol").GetComponent<TMP_Text>();
        var es=EventSystem.current;var originalFocus=es.currentSelectedGameObject;var toggles=ui.GetComponentsInChildren<Toggle>();var equipped=toggles.Where(t=>t.isOn).ToArray();var messages=new List<string>();
        try
        {
            foreach(var option in toggles)
            {
                es.SetSelectedGameObject(option.gameObject);
                if(name.text!=option.transform.Find("Content/TopRow/Label").GetComponent<TMP_Text>().text||effect.text!=option.transform.Find("Content/Effect").GetComponent<TMP_Text>().text||symbol.text!=option.transform.Find("Content/TopRow/Emblem/Text").GetComponent<TMP_Text>().text)throw new Exception("Preview mismatch "+option.name);
                if(equipped.Any(t=>!t.isOn)||toggles.Count(t=>t.isOn)!=equipped.Length)throw new Exception("Preview changed equipment");
                if(effect.GetPreferredValues(effect.text,effect.rectTransform.rect.width,float.PositiveInfinity).y>effect.rectTransform.rect.height)throw new Exception("Center effect overflow "+option.name);
            }
            messages.Add("PASS: all six focus previews show their live symbol/name/effect; equipment unchanged; center effect text fits.");
            es.SetSelectedGameObject(toggles[0].gameObject);
            ExecuteEvents.Execute(toggles[2].gameObject,new PointerEventData(es),ExecuteEvents.pointerEnterHandler);
            if(name.text!=toggles[2].transform.Find("Content/TopRow/Label").GetComponent<TMP_Text>().text)throw new Exception("Hover preview failed");
            ExecuteEvents.Execute(toggles[2].gameObject,new PointerEventData(es),ExecuteEvents.pointerExitHandler);
            if(name.text!=toggles[0].transform.Find("Content/TopRow/Label").GetComponent<TMP_Text>().text)throw new Exception("Hover exit did not restore focus");
            messages.Add("PASS: mouse hover previews and exit restores the keyboard focus, without equipping.");
            foreach(var b in ui.GetComponentsInChildren<Button>())
            {
                var nav=b.navigation;var plain=nav;plain.mode=Navigation.Mode.None;b.navigation=plain;
                try
                {
                    var image=b.GetComponent<UnityEngine.UI.Image>();var p=new PointerEventData(es){button=PointerEventData.InputButton.Left};
                    if(!image.sprite.name.StartsWith("Strong"))throw new Exception("Unstyled action");
                    b.OnPointerEnter(p);if(image.overrideSprite!=b.spriteState.highlightedSprite)throw new Exception("Hover state");
                    b.OnPointerDown(p);if(image.overrideSprite!=b.spriteState.pressedSprite)throw new Exception("Pressed state");
                    b.OnPointerUp(p);b.OnPointerExit(p);b.OnSelect(new BaseEventData(es));if(image.overrideSprite!=b.spriteState.selectedSprite)throw new Exception("Selected state");
                    b.interactable=false;if(image.overrideSprite!=b.spriteState.disabledSprite)throw new Exception("Disabled state");
                    b.interactable=true;b.OnDeselect(new BaseEventData(es));
                }
                finally{b.navigation=nav;}
            }
            messages.Add("PASS: both emphasized actions retain normal / selected / pressed / disabled sprite states.");
            return string.Join("\n",messages);
        }
        finally{es.SetSelectedGameObject(originalFocus);}
    }
}
