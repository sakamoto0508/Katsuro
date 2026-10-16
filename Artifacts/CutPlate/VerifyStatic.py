from pathlib import Path
import json,hashlib,re,difflib
root=Path.cwd();out=root/'Artifacts/CutPlate';base=out/'Baseline'
start=json.loads((base/'StartHashes.json').read_text(encoding='utf-8-sig'))
changed=sorted(p for p,h in start.items() if hashlib.sha256((root/p).read_bytes()).hexdigest().upper()!=h)
expected=sorted(['Assets/Mock/Resources/JustAvoidCounterCut.shader','Assets/Mock/Effects/JustAvoidCounterFirstCut.mat','Assets/Mock/Effects/JustAvoidCounterCut.mat','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'])
assert changed==expected,changed
patch=[]
for p in changed:
 a=(base/p).read_text(encoding='utf-8-sig');b=(root/p).read_text(encoding='utf-8-sig')
 patch+=difflib.unified_diff(a.splitlines(True),b.splitlines(True),fromfile='start/'+p,tofile='cut-plate/'+p)
 assert re.findall(r'guid: ([0-9a-f]+)',a)==re.findall(r'guid: ([0-9a-f]+)',b)
 if p.endswith('.prefab'):
  norm=lambda s:re.sub(r'  _cutSize: [^\n]+','  _cutSize: <size>',s)
  assert norm(a)==norm(b),'Unexpected Blood or hierarchy change'
(out/'ThisTask.patch').write_text(''.join(patch),encoding='utf-8')
shader=(root/'Assets/Mock/Resources/JustAvoidCounterCut.shader').read_text()
assert shader.count('{')==shader.count('}') and shader.count('(')==shader.count(')')
for name,hold in [('JustAvoidCounterFirstCut',.025),('JustAvoidCounterCut',.16)]:
 s=(root/f'Assets/Mock/Effects/{name}.mat').read_text()
 values={k:float(v) for k,v in re.findall(r'^    - (_\w+): ([\d.]+)$',s,re.M)}
 assert values['_LineHold']==hold and values['_ZTest']==4
 assert values['_SheetLength']>values['_CoreLength'] and values['_SheetWidth']>values['_CoreWidth']
 assert values['_SheetSoftness']>0 and values['_FadeDuration']>0
 expiry=hold+values['_FadeDuration']+values['_ResidualGlowDuration']
 assert expiry<(.43 if name.endswith('FirstCut') else .6)
report={'intentional_changes':expected,'other_baseline_files_unchanged':len(start)-len(expected),'Prefab':'Only _cutSize changed. Blood, hierarchy and external references identical','CSharp':'All unchanged; pool, time, gameplay and animation unchanged','Shader':'Both Material passes compiled; see EditorVerification.json','RuntimeVisualTest':'Not performed; no Play Mode, gameplay inputs or capture requested by Codex'}
(out/'StaticVerification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
