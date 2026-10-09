from summary_inventory import *
import shutil,difflib,html
baseline=Path('AgentScripts/SummaryBaseline')
catalog=json.loads(Path('AgentScripts/summary_catalog.json').read_text(encoding='utf-8'))
group=sys.argv[1]
logpath=Path('AgentScripts/SummaryChanges.json')
log=json.loads(logpath.read_text(encoding='utf-8')) if logpath.exists() else []
for p in sorted(root.rglob('*.cs')):
    if not str(p.relative_to(root)).replace('\\','/').startswith(group):continue
    rel=p.relative_to(root);saved=baseline/rel
    if not saved.exists():saved.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,saved)
    text,enc=read(p);lines=text.splitlines(keepends=True);entries=catalog.get(p.stem,{})
    for d in reversed(declarations(text)):
        if d['documented']:continue
        desc=catalog['_types'].get(d['name']) if d['kind']!='method' else entries.get(d['owner']+'.'+d['name'],entries.get(d['name']))
        if not desc:continue
        summary,*ret=desc.split('|');i=d['line'];indent=re.match(r'\s*',lines[i]).group().replace('\r','').replace('\n','')
        # Place docs before attributes, so they attach to the declaration.
        while i>0 and lines[i-1].strip().startswith('['):i-=1
        ending='\r\n' if '\r\n' in text else '\n'
        comment=indent+'/// <summary>'+html.escape(summary,quote=False)+'</summary>'+ending
        if ret:comment+=indent+'/// <returns>'+html.escape(ret[0],quote=False)+'</returns>'+ending
        lines.insert(i,comment)
        log.append({'file':str(p).replace('\\','/'),'kind':d['kind'],'name':d['name'],'summary':summary})
    updated=''.join(lines)
    if updated!=text:
        bom=p.read_bytes().startswith(b'\xef\xbb\xbf');p.write_bytes((b'\xef\xbb\xbf' if bom else b'')+updated.encode(enc))
logpath.write_text(json.dumps(log,ensure_ascii=False,indent=2),encoding='utf-8')
print(group,'total added',len(log))
