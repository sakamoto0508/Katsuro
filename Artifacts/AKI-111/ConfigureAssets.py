import re
from pathlib import Path
root=Path('Assets/Mock/Effects')
for second in (False,True):
 name='JustAvoidCounterCut.mat' if second else 'JustAvoidCounterFirstCut.mat'
 path=root/name
 values={'_CoreWidth':.17 if second else .13,'_CoreGain':1.75 if second else 1.45,'_ImpactBoost':.65 if second else .4,'_TaperStrength':1.2 if second else .8,'_BladeCurve':.12 if second else .06,'_GlowWidth':.36 if second else .27,'_GlowIntensity':.28 if second else .22,'_MinCorePixels':1.5 if second else 1.3,'_SweepDuration':.045 if second else .028,'_LineHold':.16 if second else .045,'_LineDuration':.25 if second else .17,'_GlowDuration':.32 if second else .235,'_FlashHold':.025 if second else .018,'_FlashDuration':.105 if second else .07,'_FlashStrength':1.65 if second else 1.2,'_ZTest':4}
 text=path.read_text(encoding='utf-8-sig')
 for key,value in values.items():
  pattern=rf'(^    - {re.escape(key)}: )[^\r\n]+'
  if re.search(pattern,text,re.M): text=re.sub(pattern,lambda m:m[1]+str(value),text,flags=re.M)
  else: text=text.replace('    m_Colors:',f'    - {key}: {value}\n    m_Colors:')
 path.write_text(text,encoding='utf-8',newline='\n')
 path=root/('JustAvoidCounterFinisherCFX.prefab' if second else 'JustAvoidCounterFirstCutCFX.prefab')
 text=path.read_text(encoding='utf-8-sig')
 text,n=re.subn(r'  _cutSize: \{x: [^,]+, y: [^}]+\}',f'  _cutSize: {{x: {2.6 if second else 1.95}, y: {.84 if second else .6}}}',text);assert n==1
 # Leave the existing tiny position offset intact; depth testing is the minimal positioning fix.
 path.write_text(text,encoding='utf-8',newline='\n')
