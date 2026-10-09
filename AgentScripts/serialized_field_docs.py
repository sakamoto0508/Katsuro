from summary_inventory import read,root
from pathlib import Path
import re,json,sys,html
catalog_path=Path('AgentScripts/SerializedFieldCatalog.json')
catalog=json.loads(catalog_path.read_text(encoding='utf-8')) if catalog_path.exists() else {}
def add(file,entries):
    target=catalog.setdefault(file,{})
    for line in entries.strip().splitlines():
        key,value=line.strip().split('=',1);target[key]=value
def save():catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
def apply(prefix):
    fields=json.loads(Path('AgentScripts/SerializedFields.json').read_text(encoding='utf-8'))['fields']
    selected=[f for f in fields if f['path'].removeprefix('Assets/Mock/Scripts/').startswith(prefix)]
    changes=[]
    for path in sorted(set(f['path'] for f in selected)):
        p=Path(path);raw=p.read_bytes();text,enc=read(p)
        backup=Path('AgentScripts/FieldDocsBaseline')/p.relative_to(root)
        if not backup.exists():backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(raw)
        lines=text.splitlines(keepends=True); edits=[]; descs={}
        for f in [f for f in selected if f['path']==path]:
            desc=catalog.get(p.stem,{}).get(f['name']) or f['tooltip']
            if not desc:raise ValueError('Missing description '+p.stem+'.'+f['name'])
            descs[f['name']]=desc
            candidates=[i for i,l in enumerate(lines) if re.search(r'\b'+re.escape(f['name'])+r'\s*(?:=|;|,)',l) and re.search(r'\b(public|private|protected|internal)\b',l) and '=>' not in l and not re.search(r'\b(return|void)\b',l)]
            if len(candidates)!=1:raise ValueError('Ambiguous field '+p.stem+'.'+f['name']+': '+str(candidates))
            i=candidates[0];start=i
            while start>0 and re.match(r'^\s*\[.*\]\s*$',lines[start-1]):start-=1
            j=start-1
            while j>=0 and not lines[j].strip():j-=1
            documented=j>=0 and lines[j].strip().startswith('///')
            indent=re.match(r'\s*',lines[start]).group().replace('\r','').replace('\n','');nl='\r\n' if '\r\n' in text else '\n'
            addition=''
            if not documented:addition+=indent+'/// <summary>'+html.escape(desc,quote=False)+'</summary>'+nl
            attrs=''.join(lines[start:i+1])
            tooltip_added='Tooltip' not in attrs
            if tooltip_added:addition+=indent+'[UnityEngine.Tooltip('+json.dumps(desc,ensure_ascii=False)+')]'+nl
            if addition:edits.append((start,addition));changes.append({'path':path,'field':f['name'],'summary':not documented,'tooltip':tooltip_added})
        # Getter wrappers also show the same explanation in code-editor hover.
        for i,l in enumerate(lines):
            m=re.match(r'\s*public\s+[\w<>,\[\]. ]+\s+(\w+)\s*=>\s*(_\w+)\s*;',l)
            if not m or m.group(2) not in descs:continue
            j=i-1
            while j>=0 and not lines[j].strip():j-=1
            if j>=0 and lines[j].strip().startswith('///'):continue
            indent=re.match(r'\s*',l).group();nl='\r\n' if '\r\n' in text else '\n'
            edits.append((i,indent+'/// <summary>'+html.escape(descs[m.group(2)],quote=False)+'</summary>'+nl))
            changes.append({'path':path,'property':m.group(1),'summary':True,'tooltip':False})
        for i,addition in sorted(edits,reverse=True):lines.insert(i,addition)
        result=''.join(lines);bom=b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b''
        p.write_bytes(bom+result.encode(enc))
    record=Path('AgentScripts/FieldDocsChanges.json');previous=json.loads(record.read_text(encoding='utf-8')) if record.exists() else []
    record.write_text(json.dumps(previous+changes,ensure_ascii=False,indent=2),encoding='utf-8')
    print(prefix,len(selected),'fields;',len(changes),'documentation additions')
if __name__=='__main__':apply(sys.argv[1])
