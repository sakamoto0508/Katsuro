from serialized_field_docs import *
import difflib,xml.etree.ElementTree as ET
def code(text):
    # Ignore only documentation comments and Tooltip annotations. Keep all other code/attributes/literals.
    text=text.replace('[SerializeField] private RectTransform _letterboxTop, _letterboxBottom;', '[SerializeField] private RectTransform _letterboxTop;[SerializeField] private RectTransform _letterboxBottom;')
    text=re.sub(r'\b(?:UnityEngine\.)?Tooltip\("(?:\\.|[^"\\])*"\)','',text)
    text=re.sub(r'\[\s*\]\s*','',text)
    rx=r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/'
    kept=[];pos=0
    for m in re.finditer(rx,text,flags=re.S):
        kept.append(re.sub(r'\s+','',text[pos:m.start()]));v=m.group()
        if not v.startswith('//') and not v.startswith('/*'):kept.append(v)
        pos=m.end()
    kept.append(re.sub(r'\s+','',text[pos:]));return ''.join(kept)
fields=json.loads(Path('AgentScripts/SerializedFields.json').read_text(encoding='utf-8'))['fields'];missing=[];errors=[];xmlerrors=[];patch=[]
for saved in Path('AgentScripts/FieldDocsBaseline').rglob('*.cs'):
    p=root/saved.relative_to('AgentScripts/FieldDocsBaseline');before=read(saved)[0];after=read(p)[0]
    if code(before)!=code(after):errors.append(str(p))
    patch.extend(difflib.unified_diff(before.splitlines(keepends=True),after.splitlines(keepends=True),fromfile='a/'+str(p).replace('\\','/'),tofile='b/'+str(p).replace('\\','/')))
    for m in re.finditer(r'(?:^[ \t]*///[^\n]*(?:\n|$))+',after,re.M):
        try:ET.fromstring('<doc>'+re.sub(r'^[ \t]*/// ?', '',m.group(),flags=re.M)+'</doc>')
        except ET.ParseError as e:xmlerrors.append((str(p),str(e)))
for f in fields:
    lines=read(Path(f['path']))[0].splitlines();i=next(i for i,l in enumerate(lines) if re.search(r'\b'+re.escape(f['name'])+r'\s*(?:=|;|,)',l) and re.search(r'\b(public|private|protected|internal)\b',l) and '=>' not in l and not re.search(r'\b(return|void)\b',l))
    start=i
    while start>0 and re.match(r'^\s*\[.*\]\s*$',lines[start-1]):start-=1
    j=start-1
    while j>=0 and not lines[j].strip():j-=1
    if j<0 or not lines[j].strip().startswith('///'):missing.append(f['owner']+'.'+f['name'])
    if 'Tooltip' not in ''.join(lines[start:i+1]):missing.append('Tooltip '+f['owner']+'.'+f['name'])
changes=json.loads(Path('AgentScripts/FieldDocsChanges.json').read_text(encoding='utf-8'))
original=json.loads(Path('AgentScripts/SerializedFieldsBefore.json').read_text(encoding='utf-8'))['fields']
signatures=lambda items:sorted((f['path'],f['owner'],f['name'],f['type']) for f in items)
if signatures(original)!=signatures(fields):errors.append('Serialized field name/type/owner changed')
report={'fields':len(fields),'files':len(set(f['path'] for f in fields)),'added_field_summaries':sum(x.get('summary',False) for x in changes if 'field' in x),'added_tooltips':sum(x.get('tooltip',False) for x in changes),'added_property_summaries':sum('property' in x for x in changes),'missing':missing,'code_changes_excluding_documentation':errors,'invalid_xml':xmlerrors,'serialized_field_signatures_unchanged':signatures(original)==signatures(fields)}
Path('AgentScripts/SerializedFieldDocs.patch').write_text(''.join(patch),encoding='utf-8',newline='')
Path('AgentScripts/FieldDocsAudit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(report,ensure_ascii=False))
if missing or errors or xmlerrors:sys.exit(1)
