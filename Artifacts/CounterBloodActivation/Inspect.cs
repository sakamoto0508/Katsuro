using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class InspectCounterBloodActivation
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only; no particle playback");
        var c=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        var result=new{playing=EditorApplication.isPlaying,prefabs=new[]{c.FirstCFX,c.FinisherCFX}.Select((fx,i)=>{
            var so=new SerializedObject(fx);var refs=so.FindProperty("_blood");
            return new{stage=i==0?"First":"Second",path=AssetDatabase.GetAssetPath(fx),rootActiveSelf=fx.gameObject.activeSelf,
                rootActiveInHierarchy=fx.gameObject.activeInHierarchy,fx.UsesLocalContactFlash,duration=so.FindProperty("_duration").floatValue,
                blood=Enumerable.Range(0,refs.arraySize).Select(j=>{var p=(ParticleSystem)refs.GetArrayElementAtIndex(j).objectReferenceValue;
                    return new{p.name,activeSelf=p.gameObject.activeSelf,activeInHierarchy=p.gameObject.activeInHierarchy,
                        parent=p.transform.parent.name,directChild=p.transform.parent==fx.transform,insideRoot=p.transform.IsChildOf(fx.transform),
                        p.main.playOnAwake,stopAction=p.main.stopAction.ToString(),space=p.main.simulationSpace.ToString(),
                        scripts=p.GetComponents<MonoBehaviour>().Select(b=>b.GetType().Name).ToArray(),emissionEnabled=p.emission.enabled};}).ToArray(),
                scripts=fx.GetComponentsInChildren<MonoBehaviour>(true).Select(b=>b.GetType().Name).ToArray()};}).ToArray()};
        File.WriteAllText("Artifacts/CounterBloodActivation/PrefabInspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
