using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
public static class VerifyRedesignV2
{
    public static string Main()
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Katsuro/Prefabs/UI/RunSetupCanvas.prefab"));
        var result=new List<string>();
        try
        {
            root.SetActive(true);var choices=new List<Toggle>();
            foreach(string kind in new[]{"Attack","Defense"})for(int i=0;i<3;i++)choices.Add(root.transform.Find("Content/Body/Blessings/"+kind+"Options/"+kind+"_"+i).GetComponent<Toggle>());
            for(int i=0;i<6;i++)
            {
                var t=choices[i];if(t.group==null||t.targetGraphic==null||!t.targetGraphic.raycastTarget)throw new Exception("Unwired toggle: "+t.name);
                if(i<5&&t.navigation.selectOnDown!=choices[i+1])throw new Exception("Down navigation "+t.name);
                if(i>0&&t.navigation.selectOnUp!=choices[i-1])throw new Exception("Up navigation "+t.name);
                if(t.navigation.selectOnRight==null)throw new Exception("Action navigation "+t.name);
                var group=new SerializedObject(t.GetComponent<RunSetupSelectableEffect>());
                if(group.FindProperty("_selectedLayer").objectReferenceValue==null||group.FindProperty("_focusFrame").objectReferenceValue==null||group.FindProperty("_hoverLayer").objectReferenceValue==null)throw new Exception("Missing state layer "+t.name);
            }
            result.Add("PASS: 6-row up/down traversal, action links, raycast targets, groups and independent focus/equipped layers.");
            foreach(var b in root.GetComponentsInChildren<Button>(true))
            {
                var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
                var data=new PointerEventData(null){button=PointerEventData.InputButton.Left};var image=b.GetComponent<UnityEngine.UI.Image>();
                if(image.sprite.name!="ActionNormal")throw new Exception("Normal "+b.name);
                b.OnPointerEnter(data);if(image.overrideSprite!=b.spriteState.highlightedSprite)throw new Exception("Hover "+b.name);
                b.OnPointerDown(data);if(image.overrideSprite!=b.spriteState.pressedSprite)throw new Exception("Press "+b.name);
                b.OnPointerUp(data);b.OnPointerExit(data);b.OnSelect(new BaseEventData(null));if(image.overrideSprite!=b.spriteState.selectedSprite)throw new Exception("Selected "+b.name);
                b.interactable=false;if(image.overrideSprite!=b.spriteState.disabledSprite)throw new Exception("Disabled "+b.name);
                result.Add("PASS: "+b.name+" normal / hover / pointer-down / selected / disabled.");
            }
            foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(t.font==null)throw new Exception("Missing font "+t.name);
            foreach(string s in new[]{"Normal","Selected","Pressed","Disabled"})
            {var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Katsuro/Art/UI/Textures/Setup/Redesign/Action"+s+".png");if(sprite==null||sprite.border.x!=32||sprite.border.y!=8)throw new Exception("Slice settings "+s);}
            result.Add("PASS: live TMP font references and 4 action sprites with protected 9-slice borders.");
            return string.Join("\n",result);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
