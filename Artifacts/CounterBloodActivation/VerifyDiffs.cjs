const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/CounterBloodActivation/',read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const hashes=JSON.parse(read(dir+'Baseline/StartHashes.json'));
const changed=Object.keys(hashes).filter(p=>!fs.existsSync(p)||crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')!==hashes[p]);
if(changed.length!==1||changed[0]!=='Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs')throw Error('Unexpected changed files '+changed.join(','));
const files=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean);
if(files.some(p=>!Object.hasOwn(hashes,p)))throw Error('Unexpected new asset');
const variants={};for(const v of ['Editor','Development','Release']){const log=read(dir+v+'Compile.log');if(log.includes('error CS'))throw Error('Compile failed '+v);variants[v]={errors:0,warnings:(log.match(/warning CS/g)||[]).length};}
const p=changed[0],r=cp.spawnSync('git',['diff','--no-index','--',dir+'Baseline/JustAvoidCounterCutVFX.cs',p],{encoding:'utf8'});if(r.status>1)throw Error(r.stderr);fs.writeFileSync(dir+'TaskOnly.patch',r.stdout);
const result={changed,variants,bloodAssetsAndParametersUnchanged:true,allOtherInitialAssetsUnchanged:true,priorUncommittedWorkPreserved:true};
fs.writeFileSync(dir+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
