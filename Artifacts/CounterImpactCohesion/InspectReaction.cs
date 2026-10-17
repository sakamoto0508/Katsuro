using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InspectCounterReaction
{
    public static object Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
        var c=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Mock/AnimationController/Enemy.controller");
        var layer=c.layers.First(l=>l.name=="HitReaction");
        var mask=Enumerable.Range(0,(int)AvatarMaskBodyPart.LastBodyPart).Select(i=>new{part=((AvatarMaskBodyPart)i).ToString(),active=layer.avatarMask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i)}).ToArray();
        var clips=layer.stateMachine.states.Select(s=>s.state.motion as AnimationClip).Where(x=>x!=null).Distinct().Select(clip=>{
            var bindings=AnimationUtility.GetCurveBindings(clip).Where(b=>b.type==typeof(Animator)&&
                (b.propertyName.StartsWith("Spine")||b.propertyName.StartsWith("Chest")||b.propertyName.StartsWith("Head")||b.propertyName.StartsWith("UpperChest"))).ToArray();
            var curves=bindings.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();
            return new{clip.name,clip.length,bindings=bindings.Select(b=>b.propertyName).ToArray(),samples=new[]{0f,.05f,.1f,.15f,.2f,.3f,.4f,.6f,.8f,1f}.Select(t=>new{normalized=t,muscleDelta=curves.Sum(v=>Mathf.Abs(v.Evaluate(t*clip.length)-v.Evaluate(0)))}).ToArray()};}).ToArray();
        var particles=new[]{"Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab","Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab"}.Select(p=>new{path=p,modules=AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<ParticleSystem>(true).Select(ps=>new{ps.name,velocity=ps.velocityOverLifetime.enabled,limitVelocity=ps.limitVelocityOverLifetime.enabled,force=ps.forceOverLifetime.enabled,inherit=ps.inheritVelocity.enabled,noise=ps.noise.enabled,emissionMode=ps.emission.GetBurst(0).count.mode.ToString(),count=ps.emission.GetBurst(0).count.constant,ps.main.loop}).ToArray()});
        var result=new{mask,clips,particles};File.WriteAllText("Artifacts/CounterImpactCohesion/ReactionAnalysis.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
