const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/BloodPlacementBalance/',read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''),norm=s=>s.replace(/\r\n/g,'\n');
const start=JSON.parse(read(dir+'Baseline/StartHashes.json'));
const allowed=['Assets/Mock/Scripts/Feedback/CombatFeedback.cs','Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs','Assets/Mock/Scripts/Config/VFXConfig.cs','Assets/Mock/ScriptableObjects/VFXConfig.asset','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'];
const changed=Object.keys(start).filter(p=>!fs.existsSync(p)||crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')!==start[p]);
if(changed.length!==allowed.length||changed.some(p=>!allowed.includes(p)))throw Error('Unexpected production changes '+changed.join(','));
const files=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean);
if(files.some(p=>!Object.hasOwn(start,p)))throw Error('New asset unexpectedly added');
const oldConfig=norm(read(dir+'Baseline/VFXConfig.asset')),newConfig=norm(read('Assets/Mock/ScriptableObjects/VFXConfig.asset'));
if(newConfig.replace(/  _lightBlood:\n[\s\S]*?(?=  _counterPresentation:)/,'')!==oldConfig)throw Error('Old VFX Config values changed');
let patch='';for(const p of allowed){const r=cp.spawnSync('git',['diff','--no-index','--',dir+'Baseline/'+p.split('/').pop(),p],{encoding:'utf8'});if(r.status>1)throw Error(r.stderr);patch+=r.stdout;}fs.writeFileSync(dir+'TaskOnly.patch',patch);
const variants={};for(const v of ['Editor','Development','Release']){const log=read(dir+v+'Compile.log');if(log.includes('error CS'))throw Error('Compile errors '+v);variants[v]={errors:0,warnings:(log.match(/warning CS/g)||[]).length};}
const result={changed,variants,allShaderMaterialAndFirstPrefabBytesUnchanged:true,normalSwordHitPrefabUnchanged:true,allPreviousVFXConfigFieldsUnchanged:true,allOtherInitialAssetsUnchanged:true,priorUncommittedWorkPreserved:true};
fs.writeFileSync(dir+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
