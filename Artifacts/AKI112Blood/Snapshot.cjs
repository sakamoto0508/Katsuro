const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/AKI112Blood/Baseline';fs.mkdirSync(dir,{recursive:true});
for(const [name,args] of [['GitStatus.txt',['status','--short']],['ExistingChanges.patch',['diff','--binary']],['Head.txt',['rev-parse','HEAD']]])fs.writeFileSync(dir+'/'+name,cp.execFileSync('git',args));
const files=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean),hashes={};
for(const p of new Set(files))if(fs.existsSync(p))hashes[p]=crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
fs.writeFileSync(dir+'/StartHashes.json',JSON.stringify(hashes,null,2));
for(const p of ['Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs','Assets/Mock/Effects/JustAvoidCounterBlood.shader','Assets/Mock/Effects/JustAvoidCounterBlood.mat','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'])fs.copyFileSync(p,dir+'/'+p.split('/').pop());
console.log({files:Object.keys(hashes).length});
