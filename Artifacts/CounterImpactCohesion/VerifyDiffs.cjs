const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/CounterImpactCohesion/';
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const baseline=JSON.parse(read(dir+'Baseline/StartHashes.json'));
const expected=['Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs','Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs','Assets/Mock/Resources/CounterImpactStroke.shader','Assets/Mock/Scripts/Feedback/CounterHitPose.cs','Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs','Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'];
const changed=Object.keys(baseline).filter(p=>!fs.existsSync(p)||crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')!==baseline[p]);
if(changed.length!==expected.length||changed.some(p=>!expected.includes(p)))throw Error('Unexpected production changes: '+changed.join(','));
const current=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean);
if(current.some(p=>!Object.hasOwn(baseline,p)))throw Error('Unexpected new production asset');
const before=JSON.parse(read(dir+'Baseline/Config.json')),after=JSON.parse(read(dir+'ConfigAfter.json')),changes=[];
function diff(a,b,p=''){
 if(JSON.stringify(a)===JSON.stringify(b))return;
 if(a&&b&&typeof a==='object'&&typeof b==='object')for(const k of new Set([...Object.keys(a),...Object.keys(b)]))diff(a[k],b[k],p?p+'.'+k:k);
 else changes.push({path:p,before:a??null,after:b??null});
}
diff(before.MonoBehaviour??before,after.MonoBehaviour??after);
const fields=new Set(['StrokeHitPeak','StrokePeakDuration','PoseDuration','TorsoFollowThrough','FollowThroughPeak','FollowThroughHold']);
for(const c of changes){const p=c.path.split('.');if(c.path!=='FirstReactionWeight'&&(!['First','Second'].includes(p[0])||!fields.has(p[1])))throw Error('Out-of-scope config '+c.path);}
fs.writeFileSync(dir+'TaskOnly.patch',cp.execFileSync('git',['diff','--binary']));
const result={changed,configChanges:changes,allOtherInitialAssetsUnchanged:true,allMaterialsUnchanged:true,normalAttackAIHitStopCamerasAnimationControllersAndClipsUnchanged:true};
fs.writeFileSync(dir+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
