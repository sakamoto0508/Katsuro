const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/CounterBloodOnly/',read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''),norm=s=>s.replace(/\r\n/g,'\n');
const start=JSON.parse(read(dir+'Baseline/StartHashes.json'));
const allowed=['Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs','Assets/Mock/Scripts/Feedback/CombatFeedback.cs','Assets/Mock/Scripts/Feedback/SwordTrail.cs','Assets/Mock/Scripts/Feedback/CounterSlashPresentation.cs','Assets/Mock/Scripts/Feedback/CounterImpactStroke.cs','Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs'];
const changed=Object.keys(start).filter(p=>!fs.existsSync(p)||crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')!==start[p]);
if(changed.length!==allowed.length||changed.some(p=>!allowed.includes(p)))throw Error('Unexpected changes '+changed.join(','));
const files=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean);
if(files.some(p=>!Object.hasOwn(start,p)))throw Error('Unexpected new asset');
let patch='';for(const p of allowed){const r=cp.spawnSync('git',['diff','--no-index','--',dir+'Baseline/'+p.split('/').pop(),p],{encoding:'utf8'});if(r.status>1)throw Error(r.stderr);patch+=r.stdout;}fs.writeFileSync(dir+'TaskOnly.patch',patch);
const variants={};for(const variant of ['Editor','Development','Release']){
 const log=read(dir+variant+'Compile.log');if(log.includes('error CS'))throw Error('Compiler errors '+variant);
 const bytes=fs.readFileSync(dir+'Assembly-CSharp.'+variant+'.dll');
 const hasEnum=bytes.includes(Buffer.from('CounterVFXDebugMode','utf8'));
 const hasDiagnosticLog=bytes.includes(Buffer.from('[CounterBlood]','utf16le'));
 if(variant==='Release'?(hasEnum||hasDiagnosticLog):(!hasEnum||!hasDiagnosticLog))throw Error('Build guard failure '+variant);
 variants[variant]={errors:0,warnings:(log.match(/warning CS/g)||[]).length,diagnosticEnum:hasEnum,diagnosticLogStrings:hasDiagnosticLog};
}
const result={changed,variants,allInitialBloodAssetsUnchanged:true,allOtherInitialAssetsUnchanged:true,priorAKI112ChangesPreserved:true};
fs.writeFileSync(dir+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
