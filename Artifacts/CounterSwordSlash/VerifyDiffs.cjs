const fs=require('node:fs'), crypto=require('node:crypto');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const hashes=JSON.parse(read('Artifacts/CounterSwordSlash/Baseline/StartHashes.json'));
const before=JSON.parse(read('Artifacts/CounterSwordSlash/ConfigBefore.json'));
const after=JSON.parse(read('Artifacts/CounterSwordSlash/ConfigAfter.json'));
const allowed=new Set(['SlashMaterial','BladeStart','TipExtension','TrailDuration','TrailBrightness','TrailWidth','CoreIntensity','CoreDuration','Afterglow']);
const changedConfig=[];
function compare(a,b,path='') {
  if(JSON.stringify(a)===JSON.stringify(b))return;
  if(a && b && typeof a==='object' && typeof b==='object'){
    for(const key of new Set([...Object.keys(a),...Object.keys(b)])) compare(a[key],b[key],path?`${path}.${key}`:key);
  }else changedConfig.push(path);
}
compare(before.MonoBehaviour ?? before,after.MonoBehaviour ?? after);
for(const path of changedConfig){
  const parts=path.split('.');
  if(!['First','Second'].includes(parts[0]) || !allowed.has(parts[1]))throw Error(`Unexpected config change: ${path}`);
}
const prefabs=['JustAvoidCounterFirstCutCFX.prefab','JustAvoidCounterFinisherCFX.prefab'].map(name=>{
  const path='Assets/Mock/Effects/'+name, original=fs.readFileSync(path,'utf8').replace(/^  _useLocalContactFlash: 1\r?\n/m,'');
  const sha=crypto.createHash('sha256').update(original).digest('hex').toUpperCase();
  if(sha!==hashes[path])throw Error(`Prefab changed beyond local flash flag: ${path}`);
  // Reconstruct and validate the exact pre-edit bytes against the recorded start hash.
  fs.writeFileSync('Artifacts/CounterSwordSlash/Baseline/'+name,original);
  return {path,onlyChange:'_useLocalContactFlash: 1',bloodAndOtherBytesUnchanged:true};
});
const existingChanged=[];
for(const [path,sha] of Object.entries(hashes)){
  const now=crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex').toUpperCase();
  if(now!==sha)existingChanged.push(path);
}
const result={changedConfig,prefabs,existingChanged};
fs.writeFileSync('Artifacts/CounterSwordSlash/DiffVerification.json',JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
