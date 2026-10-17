const fs=require('node:fs'),crypto=require('node:crypto');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const initial=JSON.parse(read('Artifacts/CounterSlashFinalPolish/Baseline/StartHashes.json'));
const changed=Object.entries(initial).filter(([p,sha])=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase()!==sha).map(([p])=>p);
const allowed=new Set(['Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs','Assets/Mock/Resources/CounterImpactStroke.shader','Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat','Assets/Mock/Effects/JustAvoidCounterFinisherStroke.mat','Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset']);
for(const p of changed)if(!allowed.has(p))throw Error('Unexpected asset change '+p);
const before=JSON.parse(read('Artifacts/CounterSlashFinalPolish/ConfigBefore.json'));
const after=JSON.parse(read('Artifacts/CounterSlashFinalPolish/ConfigAfter.json'));
if(JSON.stringify((before.MonoBehaviour??before).Second)!==JSON.stringify((after.MonoBehaviour??after).Second))throw Error('Second configuration changed');
const configChanges=[];
function compare(a,b,path=''){
 if(JSON.stringify(a)===JSON.stringify(b))return;
 if(a&&b&&typeof a==='object'&&typeof b==='object')for(const k of new Set([...Object.keys(a),...Object.keys(b)]))compare(a[k],b[k],path?path+'.'+k:k);
 else configChanges.push(path);
}
compare(before.MonoBehaviour??before,after.MonoBehaviour??after);
const fields=new Set(['StrokeLength','StrokeWidth','StrokeCoreDuration','StrokeDuration','StrokeIntensity']);
for(const p of configChanges){const k=p.split('.');if(!['First','Second'].includes(k[0])||!fields.has(k[1]))throw Error('Unexpected config change '+p);}
const result={changed,configChanges,otherInitialAssetsUnchanged:true};
fs.writeFileSync('Artifacts/CounterSlashFinalPolish/DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));

