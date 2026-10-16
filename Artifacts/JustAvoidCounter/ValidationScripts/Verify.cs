var report=new System.Text.StringBuilder();
var config=UnityEditor.AssetDatabase.LoadAssetAtPath<JustAvoidCounterConfig>("Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset");
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception(label);report.AppendLine("PASS "+label);};
check(config.FirstCFX!=null && config.FinisherCFX!=null && config.FirstCFX!=config.FinisherCFX,"independent first/second prefab references");
check(Mathf.Approximately(config.First.HitStop,.045f)&&Mathf.Approximately(config.Second.HitStop,.155f),"original HitStop durations unchanged");
var vfx=UnityEditor.AssetDatabase.LoadAssetAtPath<VFXConfig>("Assets/Mock/ScriptableObjects/VFXConfig.asset");check(vfx.CounterPresentation==config,"VFXConfig reference");
foreach(var prefab in new[]{config.FirstCFX,config.FinisherCFX}) {
 foreach(var c in prefab.GetComponentsInChildren<UnityEngine.Component>(true))check(c!=null,"CFX component "+(c!=null?c.GetType().Name:"missing"));
 var ps=prefab.GetComponentsInChildren<UnityEngine.ParticleSystem>(true);check(ps.Length==3,"three blood layers "+prefab.name);
 foreach(var p in ps)check(!p.main.loop&&!p.main.playOnAwake&&p.main.useUnscaledTime&&p.main.stopAction==UnityEngine.ParticleSystemStopAction.None,"pooled particles "+p.name);
 var material=prefab.GetComponentInChildren<UnityEngine.MeshRenderer>(true).sharedMaterial;check(material!=null&&!UnityEditor.ShaderUtil.ShaderHasError(material.shader),"rendered cut shader valid "+prefab.name);
}
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Mock/AnimationController/Player.controller");
var baseline=System.IO.File.ReadAllText("Artifacts/JustAvoidCounter/ValidationScripts/BaselineAnimationEvents.txt");
foreach(var layer in controller.layers) foreach(var child in layer.stateMachine.states)if(child.state.tag=="JustAvoidCounter") {
 var s=child.state;var events=UnityEditor.AnimationUtility.GetAnimationEvents((UnityEngine.AnimationClip)s.motion);
 var sequence=string.Join("\r\n",System.Array.ConvertAll(events,e=>"EVENT="+e.time+" "+e.functionName));check(baseline.Contains(sequence),"existing event sequence unchanged "+s.name);
 foreach(var b in s.behaviours)if(b is JustAvoidCounterSpeedState)check(new UnityEditor.SerializedObject(b).FindProperty("_strike").enumValueIndex==(s.name.EndsWith("1")?1:2),"state stage "+s.name);
}
foreach(var trail in UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Mock/Prefabs/Player.prefab").GetComponentsInChildren<SwordTrail>(true))check(new UnityEditor.SerializedObject(trail).FindProperty("_counterPresentation").objectReferenceValue==config,"Player trail reference "+trail.name);
foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyAnimationController>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None))check(new UnityEditor.SerializedObject(enemy).FindProperty("_counterPresentation").objectReferenceValue==config,"live GameScene enemy reference");
check(!UnityEditor.EditorApplication.isPlaying,"Play Mode not used");check(!UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty,"GameScene remains clean");
foreach(var asset in new UnityEngine.Object[]{config,vfx})UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
System.IO.File.WriteAllText("Artifacts/JustAvoidCounter/ReferenceVerification.txt",report.ToString());
return "All reference, particle, shader, event and scene-state checks passed.";

