const fs=require('node:fs'),cp=require('node:child_process'),crypto=require('node:crypto');
const dir='Artifacts/AKI112Blood/',read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const baseline=JSON.parse(read(dir+'Baseline/StartHashes.json'));
const allowed=['Assets/Mock/Effects/JustAvoidCounterBlood.shader','Assets/Mock/Effects/JustAvoidCounterBlood.mat','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab','Assets/Mock/Scripts/Feedback/SwordTrail.cs','Assets/Mock/Scripts/Feedback/CombatFeedback.cs'];
const changed=Object.keys(baseline).filter(p=>!fs.existsSync(p)||crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')!==baseline[p]);
if(changed.length!==allowed.length||changed.some(p=>!allowed.includes(p)))throw Error('Unexpected changed assets '+changed.join(','));
const files=cp.execFileSync('git',['ls-files','-co','--exclude-standard','-z','--','Assets','Packages','ProjectSettings'],{encoding:'utf8'}).split('\0').filter(Boolean);
if(files.some(p=>!Object.hasOwn(baseline,p)))throw Error('Unexpected new asset');
// Existing material settings must remain byte-equivalent after removing the two added properties.
const norm=s=>s.replace(/\r\n/g,'\n');
const oldMat=norm(read(dir+'Baseline/JustAvoidCounterBlood.mat'));
const newMat=norm(read('Assets/Mock/Effects/JustAvoidCounterBlood.mat')).replace('    - _ContourVariation: 0.65\n','').replace('    - _Opacity: 1\n','');
if(oldMat!==newMat)throw Error('Existing Blood material values changed');
const shader=read('Assets/Mock/Effects/JustAvoidCounterBlood.shader');
for(const token of ['Blend SrcAlpha OneMinusSrcAlpha','ZWrite Off','ZTest [_ZTest]','saturate(i.color.a*_Opacity)*coverage'])if(!shader.includes(token))throw Error('Unexpected transparency mode');
const before=JSON.parse(read(dir+'Before.json')),after=JSON.parse(read(dir+'After.json'));
const parameterChanges=[];
for(let i=0;i<2;i++)for(let j=0;j<3;j++){
 const a=before.effects[i].particles[j],b=after.effects[i].particles[j];
 if(a.name!==b.name||a.bursts[0].count.samples[0].min!==b.count)throw Error('Emitter or count changed');
 parameterChanges.push({strike:i===0?'First':'Second',emitter:b.name,size:{before:[a.size.constantMin,a.size.constantMax],after:b.size},delay:{before:a.delay.samples[0].min===a.delay.samples[0].max?a.delay.samples[0].min:[a.delay.constantMin,a.delay.constantMax],after:b.delay},angle:{before:a.angle,after:b.angle},lengthScale:{before:a.lengthScale,after:b.lengthScale}});
}
fs.writeFileSync(dir+'TaskOnly.patch',cp.execFileSync('git',['diff','--binary']));
const result={changed,parameterChanges,particleCountsUnchanged:true,existingMaterialColorsGainDepthUnchanged:true,configAnimationsHitReactionSlashAssetsAndOtherInitialAssetsUnchanged:true};
fs.writeFileSync(dir+'DiffVerification.json',JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
