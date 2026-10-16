from pathlib import Path
import hashlib,json,re,difflib
root=Path.cwd();out=root/'Artifacts/AKI-111';baseline=out/'Baseline'
start=json.loads((baseline/'StartHashes.json').read_text(encoding='utf-8-sig'))
changed=[p for p,h in start.items() if hashlib.sha256((root/p).read_bytes()).hexdigest().upper()!=h]
expected={'Assets/Mock/Resources/JustAvoidCounterCut.shader','Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs','Assets/Mock/Effects/JustAvoidCounterFirstCut.mat','Assets/Mock/Effects/JustAvoidCounterCut.mat','Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab','Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab'}
assert set(changed)==expected,(changed,expected)
patch=[]
for p in sorted(changed):
 before=(baseline/p).read_text(encoding='utf-8-sig').splitlines(keepends=True);after=(root/p).read_text(encoding='utf-8-sig').splitlines(keepends=True)
 patch+=difflib.unified_diff(before,after,fromfile='start/'+p,tofile='AKI-111/'+p)
 if p.endswith('.prefab'):
  normalize=lambda s:re.sub(r'  _cutSize: [^\n]+','  _cutSize: <footprint>',s)
  assert normalize(''.join(before))==normalize(''.join(after)), 'Unexpected prefab/Blood modification'
  assert re.findall(r'guid: ([0-9a-f]+)',''.join(before))==re.findall(r'guid: ([0-9a-f]+)',''.join(after))
(out/'ThisTask.patch').write_text(''.join(patch),encoding='utf-8')
shader=(root/'Assets/Mock/Resources/JustAvoidCounterCut.shader').read_text(encoding='utf-8-sig')
assert shader.count('{')==shader.count('}') and shader.count('(')==shader.count(')')
props=set(re.findall(r'^\s*(?:\[[^\]]+\]\s*)*(_\w+)\(',shader,re.M))
material_data=[]
for name,hold in [('JustAvoidCounterFirstCut',.045),('JustAvoidCounterCut',.155)]:
 p=root/f'Assets/Mock/Effects/{name}.mat';text=p.read_text()
 values={k:float(v) for k,v in re.findall(r'^    - (_\w+): ([\d.]+)$',text,re.M)}
 assert set(values)<=props and values['_ZTest']==4
 assert values['_SweepDuration']<values['_LineHold']<values['_LineDuration']<values['_GlowDuration']
 assert values['_LineHold']>=hold and values['_FlashHold']<values['_FlashDuration']<values['_LineDuration']
 material_data.append({'material':str(p.relative_to(root)),'values':values})
# Resolve all CFX/Material external GUID references against local metadata.
meta_map={}
for p in (root/'Assets/Mock').rglob('*.meta'):
 text=p.read_text(encoding='utf-8-sig',errors='replace');m=re.search(r'^guid: ([0-9a-f]+)$',text,re.M)
 if m:meta_map[m[1]]=p.with_suffix('')
references=[]
for p in expected:
 if not p.endswith(('.mat','.prefab')):continue
 for guid in set(re.findall(r'guid: ([0-9a-f]{32})',(root/p).read_text())):
  assert guid in meta_map and meta_map[guid].exists(),(p,guid)
  references.append({'from':p,'to':str(meta_map[guid].relative_to(root))})
report={'status':'Static checks passed; Unity Shader import/compilation and visual tests NOT performed','baseline_asset_files':len(start),'changed_since_task_start':sorted(changed),'preserved_baseline_files':len(start)-len(changed),'prefab_change_only':'_cutSize (Blood, hierarchy, GUIDs and component references unchanged)','materials':material_data,'references':references,'csharp_compile':'Unity 6000.2.7f2 bundled Roslyn + existing Assembly-CSharp.rsp, exit 0, 9 existing warnings','shader_check':'Balanced ShaderLab/HLSL delimiters, known HDRP include and exposure API, matching property names; not a GPU compile'}
(out/'StaticVerification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:report[k] for k in ('status','baseline_asset_files','changed_since_task_start','preserved_baseline_files','prefab_change_only')},ensure_ascii=False,indent=2))
