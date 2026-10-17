const fs=require('node:fs'),crypto=require('node:crypto');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const initial=JSON.parse(read('Artifacts/CounterBladeStroke/Baseline/StartHashes.json'));
const changed=Object.entries(initial).filter(([p,sha])=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase()!==sha).map(([p])=>p);
const before=JSON.parse(read('Artifacts/CounterBladeStroke/ConfigBefore.json'));
const after=JSON.parse(read('Artifacts/CounterBladeStroke/ConfigAfter.json'));
const allowed=new Set(['TrailWidth','TrailBrightness','CoreIntensity','CoreDuration','TrailDuration','Afterglow','StrokeMaterial','StrokeLength','StrokeWidth','StrokeCoreDuration','StrokeDuration','StrokeIntensity']);
const configChanges=[];
function compare(a,b,path=''){
 if(JSON.stringify(a)===JSON.stringify(b))return;
 if(a&&b&&typeof a==='object'&&typeof b==='object')for(const k of new Set([...Object.keys(a),...Object.keys(b)]))compare(a[k],b[k],path?path+'.'+k:k);
 else configChanges.push(path);
}
compare(before.MonoBehaviour??before,after.MonoBehaviour??after);
for(const p of configChanges){const k=p.split('.');if(!['First','Second'].includes(k[0])||!allowed.has(k[1]))throw Error('Unexpected config change '+p);}
for(const p of changed)if(!['Assets/Mock/Effects/JustAvoidCounterFirstSlash.mat','Assets/Mock/Effects/JustAvoidCounterFinisherSlash.mat','Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset','Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs','Assets/Mock/Scripts/Feedback/SwordTrail.cs','Assets/Mock/Scripts/Feedback/CombatFeedback.cs','Assets/Mock/Scripts/Feedback/CounterSlashPresentation.cs','Assets/Mock/Resources/CounterSwordSlash.shader'].includes(p))throw Error('Unexpected asset change '+p);
const result={changed,configChanges,otherInitialAssetsUnchanged:true};
fs.writeFileSync('Artifacts/CounterBladeStroke/DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
