using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class ConfigureCounterSlashDirectionTiming
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required; Play Mode was not changed.");
        AssetDatabase.ImportAsset("Assets/Mock/Resources/CounterImpactStroke.shader",ImportAssetOptions.ForceSynchronousImport);
        var config=AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
        File.WriteAllText("Artifacts/CounterSlashDirectionTiming/ConfigBefore.json",EditorJsonUtility.ToJson(config,true));
        foreach(var material in new[]{config.First.StrokeMaterial,config.Second.StrokeMaterial})
            File.WriteAllText("Artifacts/CounterSlashDirectionTiming/Baseline/"+material.name+".json",EditorJsonUtility.ToJson(material,true));
        config.First.StrokeLength=1.4f;
        config.First.StrokeCoreHold=.035f;
        config.First.StrokeCoreDuration=.105f;
        config.First.StrokePeakDuration=.05f;
        config.First.StrokeDuration=.185f;
        config.Second.StrokeCoreHold=.155f;
        config.Second.StrokeCoreDuration=.205f;
        config.Second.StrokePeakDuration=.065f;
        config.Second.StrokeDuration=.275f;
        foreach(var strike in new[]{config.First,config.Second})
        {strike.StrokeMinProjection=.55f;strike.StrokeMaxProjectionAngle=20f;}
        EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
        // Preserve the previous micro-tune's face, width, tint and all First material values.
        var second=config.Second.StrokeMaterial;
        second.SetFloat("_CoreIntensity",2.8f);second.SetFloat("_FrontIntensity",1.3f);
        EditorUtility.SetDirty(second);AssetDatabase.SaveAssetIfDirty(second);
        File.WriteAllText("Artifacts/CounterSlashDirectionTiming/ConfigAfter.json",EditorJsonUtility.ToJson(config,true));
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Player.controller");
        var states=new List<object>();
        foreach(var layer in controller.layers)ReadStates(layer.stateMachine,states);
        var result=new{playing=EditorApplication.isPlaying,states,hitStop=new[]{config.First.HitStop,config.Second.HitStop}};
        File.WriteAllText("Artifacts/CounterSlashDirectionTiming/AnimationInventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    private static void ReadStates(AnimatorStateMachine machine,List<object> output)
    {
        foreach(var child in machine.states)
        {
            var state=child.state;if(state.tag!="JustAvoidCounter")continue;
            var clip=state.motion as AnimationClip;
            output.Add(new{state.name,state.tag,motion=AssetDatabase.GetAssetPath(state.motion),clipLength=clip!=null?clip.length:0f,
                events=clip!=null?clip.events.Select(e=>new{e.functionName,e.time}).ToArray():null});
        }
        foreach(var child in machine.stateMachines)ReadStates(child.stateMachine,output);
    }
}
