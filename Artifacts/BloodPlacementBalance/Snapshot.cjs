const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/BloodPlacementBalance/Baseline';fs.mkdirSync(dir,{recursive:true});
for(const [n,a] of [['GitStatus.txt',['status','--short']],['ExistingChanges.patch',['diff','--binary']],['Head.txt',['rev-parse','HEAD']]])fs.writeFileSync(dir+'/'+n,cp.execFileSync('git',a));
const hashes={};for(const p of new Set(cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean)))if(fs.existsSync(p))hashes[p]=crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
fs.writeFileSync(dir+'/StartHashes.json',JSON.stringify(hashes,null,2));
for(const p of ['Assets/Mock/Scripts/Feedback/CombatFeedback.cs','Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs','Assets/Mock/Scripts/Config/VFXConfig.cs','Assets/Mock/ScriptableObjects/VFXConfig.asset','Assets/Mock/Effects/SwordHitVFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab'])fs.copyFileSync(p,dir+'/'+p.split('/').pop());
console.log({files:Object.keys(hashes).length});
