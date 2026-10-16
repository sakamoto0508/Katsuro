from pathlib import Path
import json,hashlib,re,difflib
root=Path.cwd();out=root/'Artifacts/CutSilverSheet';base=out/'Baseline'
start=json.loads((base/'StartHashes.json').read_text(encoding='utf-8-sig'))
changed=sorted(p for p,h in start.items() if hashlib.sha256((root/p).read_bytes()).hexdigest().upper()!=h)
expected=sorted(['Assets/Mock/Resources/JustAvoidCounterCut.shader','Assets/Mock/Effects/JustAvoidCounterFirstCut.mat','Assets/Mock/Effects/JustAvoidCounterCut.mat','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'])
outside=[p for p in changed if p not in expected]
assert set(outside)<= {'Assets/Mock/UI/KatsuroUIFont.asset'},outside
assert all(p in changed for p in expected)
patch=[]
for p in expected:
 old=(base/p).read_text(encoding='utf-8-sig');new=(root/p).read_text(encoding='utf-8-sig')
 patch+=difflib.unified_diff(old.splitlines(True),new.splitlines(True),fromfile='start/'+p,tofile='silver-sheet/'+p)
 if p.endswith('.prefab'):
  norm=lambda t:re.sub(r'  _cutSize: [^\n]+','  _cutSize: <footprint>',t)
  assert norm(old)==norm(new),'Unexpected prefab edit'
 assert re.findall(r'guid: ([0-9a-f]+)',old)==re.findall(r'guid: ([0-9a-f]+)',new),'Changed references'
(out/'ThisTask.patch').write_text(''.join(patch),encoding='utf-8')
for name,hitstop in [('JustAvoidCounterFirstCut',.045),('JustAvoidCounterCut',.155)]:
 t=(root/f'Assets/Mock/Effects/{name}.mat').read_text()
 values={k:float(v) for k,v in re.findall(r'^    - (_\w+): ([\d.]+)$',t,re.M)}
 assert values['_ZTest']==4 and values['_FadeDuration']>0 and values['_ResidualGlowDuration']>0
 if name=='JustAvoidCounterCut':assert values['_LineHold']>=hitstop
 assert '_LineDuration' not in values and '_GlowDuration' not in values
shader=(root/expected[-1]).read_text() if expected[-1].endswith('.shader') else (root/'Assets/Mock/Resources/JustAvoidCounterCut.shader').read_text()
assert shader.count('{')==shader.count('}') and shader.count('(')==shader.count(')')
report={'intentional_changes':expected,'other_observed_changes_preserved':outside,'preserved_baseline_files':len(start)-len(changed),'prefabs':'Only _cutSize changed; particles and references identical','CSharp':'Unchanged including VFX clock, pool and combat code','Blood':'All shader, material, particle settings unchanged','Shader':'Both material passes synchronously compiled in Editor; see EditorVerification.json','visual_tests':'Not performed. Existing user-started Play Mode was not started, stopped or controlled.'}
(out/'StaticVerification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))

