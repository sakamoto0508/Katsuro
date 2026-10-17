const fs=require('node:fs'), crypto=require('node:crypto'), cp=require('node:child_process');
const root='Artifacts/CounterSlashDirectionTiming/';
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const initial=JSON.parse(read(root+'Baseline/StartHashes.json'));
const changed=Object.entries(initial).filter(([p,sha])=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase()!==sha).map(([p])=>p);
const paths=['Assets/Mock/Scripts/Feedback/SwordTrail.cs','Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs','Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs','Assets/Mock/Resources/CounterImpactStroke.shader','Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset','Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat'];
for(const p of changed)if(!paths.includes(p))throw Error('Unexpected asset change '+p);
if(changed.length!==paths.length)throw Error('Expected six changed production files');
const configChanges=[];
function compare(a,b,path=''){
 if(JSON.stringify(a)===JSON.stringify(b))return;
 if(a&&b&&typeof a==='object'&&typeof b==='object')for(const k of new Set([...Object.keys(a),...Object.keys(b)]))compare(a[k],b[k],path?path+'.'+k:k);
 else configChanges.push({path,before:a,after:b});
}
const before=JSON.parse(read(root+'ConfigBefore.json')), after=JSON.parse(read(root+'ConfigAfter.json'));
compare(before.MonoBehaviour??before,after.MonoBehaviour??after);
const fields=new Set(['StrokeLength','StrokeCoreHold','StrokeCoreDuration','StrokePeakDuration','StrokeDuration','StrokeMinProjection','StrokeMaxProjectionAngle']);
for(const d of configChanges){const k=d.path.split('.');if(!['First','Second'].includes(k[0])||!fields.has(k[1]))throw Error('Unexpected config change '+d.path);}
// Compare against exact start copies, so the patch excludes prior uncommitted micro-tuning.
let patch='';
for(const p of paths){
 const baseline=root+'Baseline/'+p.split('/').pop();
 const diff=cp.spawnSync('git',['diff','--no-index','--',baseline,p],{encoding:'utf8'});
 if(diff.status>1)throw Error(diff.stderr);patch+=diff.stdout;
}
fs.writeFileSync(root+'TaskOnly.patch',patch);
const result={changed,configChanges,otherInitialAssetsUnchanged:true,firstStrokeMaterialIdenticalToStart:true};
fs.writeFileSync(root+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
