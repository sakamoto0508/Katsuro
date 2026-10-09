from serialized_field_docs import *
def code(text):
    text=re.sub(r'^[ \t]*\[UnityEngine\.Tooltip\("(?:\\.|[^"\\])*"\)\]\r?\n','',text,flags=re.M)
    rx=r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/'
    kept=[];pos=0
    for m in re.finditer(rx,text,flags=re.S):
        kept.append(re.sub(r'\s+','',text[pos:m.start()]));v=m.group()
        if not v.startswith('//') and not v.startswith('/*'):kept.append(v)
        pos=m.end()
    kept.append(re.sub(r'\s+','',text[pos:]));return ''.join(kept)
for saved in Path('AgentScripts/FieldDocsBaseline').rglob('*.cs'):
    dest=root/saved.relative_to('AgentScripts/FieldDocsBaseline')
    if code(read(saved)[0])!=code(read(dest)[0]):raise ValueError('Unexpected non-documentation change: '+str(dest))
for saved in Path('AgentScripts/FieldDocsBaseline').rglob('*.cs'):
    dest=root/saved.relative_to('AgentScripts/FieldDocsBaseline');dest.write_bytes(saved.read_bytes())
Path('AgentScripts/FieldDocsChanges.json').write_text('[]',encoding='utf-8')
for prefix in ['Config/','Camera/','Enemy/','Feedback/','Managers/','Player/','Progression/','StatusEffect/','UI/']:apply(prefix)
