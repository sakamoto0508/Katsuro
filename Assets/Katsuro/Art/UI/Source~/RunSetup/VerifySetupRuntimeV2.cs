using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
public static class VerifySetupRuntimeV2
{
    public static string Main()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        var ui=UnityEngine.Object.FindAnyObjectByType<RunSetupUI>();
        if(!ui.IsOpen)throw new Exception("Preparation screen is not open");
        var so=new SerializedObject(ui);var events=EventSystem.current;
        var attacks=Enumerable.Range(0,3).Select(i=>(Toggle)so.FindProperty("_attackOptions").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var defenses=Enumerable.Range(0,3).Select(i=>(Toggle)so.FindProperty("_defenseOptions").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        var originalA=attacks.First(t=>t.isOn);var originalD=defenses.First(t=>t.isOn);var results=new List<string>();
        try
        {
            events.SetSelectedGameObject(attacks[0].gameObject);
            for(int i=1;i<3;i++)
            {
                var data=new AxisEventData(events){moveDir=MoveDirection.Down,moveVector=Vector2.down};
                ExecuteEvents.Execute(events.currentSelectedGameObject,data,ExecuteEvents.moveHandler);
                if(events.currentSelectedGameObject!=attacks[i].gameObject)throw new Exception("Down focus did not reach "+attacks[i].name);
                if(!originalA.isOn||!originalD.isOn)throw new Exception("Focus changed equipment");
            }
            results.Add("PASS: Move events traverse the vertical rows without changing equipment.");
            for(int kind=0;kind<2;kind++)
            {
                var toggles=kind==0?attacks:defenses;var name=(TMP_Text)so.FindProperty(kind==0?"_attackName":"_defenseName").objectReferenceValue;
                var effect=(TMP_Text)so.FindProperty(kind==0?"_attackDescription":"_defenseDescription").objectReferenceValue;
                for(int i=0;i<3;i++)
                {
                    ExecuteEvents.Execute(toggles[i].gameObject,new BaseEventData(events),ExecuteEvents.submitHandler);
                    if(!toggles[i].isOn||toggles.Count(t=>t.isOn)!=1)throw new Exception("Exclusive selection failed");
                    if(name.text!=(kind==0?RunSession.AttackNames[i]:RunSession.DefenseNames[i]))throw new Exception("Name did not update");
                    if(effect.text!=(kind==0?RunSession.AttackDescription(i):RunSession.DefenseDescription(i)))throw new Exception("Effect did not update");
                }
            }
            results.Add("PASS: Submit events select all 6 blessings exclusively and update existing names/effects.");
            var info=ui.transform.Find("Content/Body/Details/InfoPanel/Title").GetComponent<TMP_Text>();
            results.Add("Rule heading: "+info.text+"; glyph U+639F present="+info.font.HasCharacter(0x639F));
            results.Add("Resolution "+Screen.width+"x"+Screen.height+"; EventSystems="+UnityEngine.Object.FindObjectsByType<EventSystem>().Length);
            return string.Join("\n",results);
        }
        finally
        {
            originalA.isOn=true;originalD.isOn=true;events.SetSelectedGameObject(attacks[2].gameObject);
        }
    }
}
