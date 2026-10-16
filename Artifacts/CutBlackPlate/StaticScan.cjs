// CPU equation check only. Not a GPU, Scene, or Game View test.
const fs = require('node:fs');
const inspection = JSON.parse(fs.readFileSync('Artifacts/CutBlackPlate/AssetInspection.json', 'utf8'));
const sat = x => Math.max(0, Math.min(1, x));
const mix = (a,b,t) => a+(b-a)*t;
const smooth = (a,b,x) => { const t=sat((x-a)/(b-a)); return t*t*(3-2*t); };
const gaussian = x => Math.exp(-x*x);
const vec = x => x.slice(1,-1).split(',').map(Number);
function sample(m, px, py, age) {
  const aa=.01; // Assumed derivative for static scan, not measured screen pixels.
  const p=m.properties, f=n=>p[n];
  const sweep=Math.max(.001,f('_SweepDuration'));
  const travel=1-Math.pow(1-sat(age/sweep),3);
  const end=Math.max(0,f('_LineHold'))+Math.max(.001,f('_FadeDuration'));
  const withdrawal=smooth(Math.max(0,f('_LineHold')),end,age);
  const tail=-.88+f('_ForwardExtension')*.55*(1-travel)+withdrawal*.55;
  const tip=.98-f('_ForwardExtension')*(1-travel);
  function lens(x,t) {
    const along=sat((x-tail)/Math.max(.001,tip-tail));
    return [smooth(tail-aa,tail+aa,x)*(1-smooth(tip-aa,tip+aa,x)), along,
      Math.pow(sat(4*along*(1-along)),Math.max(.1,t))];
  }
  const x=px/Math.max(.01,f('_CoreLength'));
  const [seg,along,taper]=lens(x,f('_TaperStrength'));
  let width=f('_CoreWidth')*taper*mix(.55,1.25,along)*mix(1.12,1,travel);
  width=Math.max(width,aa*f('_MinCorePixels')*sat(taper*5));
  const y=py-f('_BladeCurve')*px*px;
  const ridge=1-smooth(width*.35,width+aa,Math.abs(y));
  const coreLife=1-smooth(Math.max(0,f('_LineHold')),end,age);
  const heel=mix(1,smooth(0,.65,along),sat((age-sweep)/Math.max(.001,end-sweep))*.7);
  const core=ridge*seg*coreLife*heel;
  const edge=(1-smooth(width+aa,width+aa*3+.035,Math.abs(y)))*seg*coreLife*heel;
  const head=gaussian((x-tip+.1)/.13)*ridge*seg*(1-smooth(sweep,sweep+.025,age));
  const glowEnd=end+Math.max(.001,f('_ResidualGlowDuration'));
  const glowLife=1-smooth(sweep,Math.max(sweep+.001,glowEnd),age);
  const glow=gaussian(y/Math.max(.01,f('_GlowWidth')))*seg*taper*f('_GlowIntensity')*glowLife;
  const air=(gaussian((y-.22-px*.07)/.025)+gaussian((y+.18-px*.04)/.02)*.5)*f('_AirStrength')*seg*taper*glowLife;
  const [ss,sa,st]=lens(px/Math.max(.01,f('_SheetLength')),f('_TaperStrength')*.8);
  const upper=Math.max(.001,f('_SheetWidth')*st*mix(.4,1.25,sa));
  const lower=Math.max(.001,f('_SheetWidth')*st*mix(.18,.42,sa));
  const bound=y>=0?upper:lower, soft=Math.max(aa,bound*f('_SheetSoftness'));
  const sm=1-smooth(Math.max(0,bound-soft),bound+aa,Math.abs(y));
  const sl=mix(.42,1.4,1-smooth(0,sweep+.03,age))*(1-smooth(Math.max(0,end-.03),glowEnd,age));
  const sheetMask=sm*ss*st*mix(1,smooth(0,.65,sa),withdrawal*.8)*sl;
  const sheet=sheetMask*f('_SheetIntensity');
  const facet=1-smooth(aa,aa*2+upper*.14,Math.abs(y-upper*.7));
  const silverFactor=mix(.35,.95,sat((y+lower)/Math.max(.001,upper+lower)))+facet*.6;
  const flash=vec(p._FlashSize).map(x=>Math.max(Math.abs(x),.005));
  const contactMask=(gaussian(px/flash[0])*gaussian(py/flash[1])+
    gaussian(px/(flash[0]*.25))*gaussian(py/(flash[1]*1.65))*.18)*
    (1-smooth(Math.max(0,f('_FlashHold')),Math.max(f('_FlashHold')+.001,f('_FlashDuration')),age));
  const contact=contactMask*Math.max(0,f('_FlashStrength'));
  const alpha=sat(core*.95+edge*f('_EdgeContrast')+contact*.75+sheet*.32+glow+air);
  const tint=vec(p._Tint), silver=vec(p._SheetTint), glowTint=vec(p._GlowTint);
  const impact=1+f('_ImpactBoost')*(1-smooth(0,sweep+.025,age));
  const rgb=tint.slice(0,3).map((c,k)=>(c*(core*f('_CoreGain')*impact+head*.3+contact*(1+f('_CoreGain')*.9)*.72)+
    silver[k]*silverFactor*sheet+glowTint[k]*(glow+air))*f('_StrikeMultiplier'));
  return { px,py,age,core,sheetMask,contactMask,alpha,rgb,edgeAlpha:edge*f('_EdgeContrast') };
}
const results=inspection.prefabs.map(m=>{
  let samples=0,nonFinite=0,lowRGBHighAlpha=0,example=null,nearZeroMasksWithAlpha=0;
  for(const age of [0,.02,.06,.12,.2,.3,.4]) for(let xi=0;xi<=160;xi++) for(let yi=0;yi<=160;yi++) {
    const v=sample(m,xi/80-1,yi/80-1,age); samples++;
    if(![v.core,v.sheetMask,v.contactMask,v.alpha,...v.rgb].every(Number.isFinite)) nonFinite++;
    if(v.alpha>.1 && Math.max(...v.rgb)<.05) { lowRGBHighAlpha++; if(!example || v.alpha>example.alpha) example=v; }
    if(v.core<1e-6 && v.sheetMask<1e-6 && v.contactMask<1e-6 && v.alpha>.1) nearZeroMasksWithAlpha++;
  }
  const point=sample(m,0,.3,0);
  return {material:m.material,samples,nonFinite,lowRGBHighAlphaAtExposure1:lowRGBHighAlpha,example,
    nearZeroCoreSheetFlashWithAlphaAbovePoint1:nearZeroMasksWithAlpha,
    illustrativeExposurePoint:{...point, exposure:.001, exposedRGB:point.rgb.map(x=>x*.001),
      note:'Hypothetical pre-exposure, not measured in user Game View. Alpha is unchanged.'}};
});
const result={scope:'CPU equations with assumed fwidth=.01. Not GPU validation. HLSL negative-base pow behavior cannot be modeled with JavaScript Math.pow.',results};
fs.writeFileSync('Artifacts/CutBlackPlate/StaticScan.json',JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
