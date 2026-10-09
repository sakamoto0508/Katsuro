from summary_inventory import *
from collections import Counter
import difflib,hashlib,xml.etree.ElementTree as ET
baseline=Path('AgentScripts/SummaryBaseline')
def executable(text):
    # Strings are kept verbatim; only comments and whitespace outside literals are ignored.
    rx=r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/'
    kept=[];pos=0
    for m in re.finditer(rx,text,flags=re.S):
        kept.append(re.sub(r'\s+','',text[pos:m.start()]));v=m.group()
        if not v.startswith('//') and not v.startswith('/*'):kept.append(v)
        pos=m.end()
    kept.append(re.sub(r'\s+','',text[pos:]));return ''.join(kept)
patch=[];errors=[];missing=[];xmlerrors=[];unchanged=0
for p in sorted(root.rglob('*.cs')):
    text,enc=read(p);saved=baseline/p.relative_to(root)
    if saved.exists():
        before=read(saved)[0]
        if executable(before)!=executable(text):errors.append(str(p))
        else:unchanged+=1
        patch.extend(difflib.unified_diff(before.splitlines(keepends=True),text.splitlines(keepends=True),fromfile='a/'+str(p).replace('\\','/'),tofile='b/'+str(p).replace('\\','/')))
    missing.extend((str(p),d['kind'],d['name']) for d in declarations(text) if not d['documented'])
    for m in re.finditer(r'(?:^[ \t]*///[^\n]*(?:\n|$))+',text,re.M):
        block=re.sub(r'^[ \t]*/// ?', '',m.group(),flags=re.M)
        try:ET.fromstring('<doc>'+block+'</doc>')
        except ET.ParseError as e:xmlerrors.append((str(p),text[:m.start()].count('\n')+1,str(e)))
Path('AgentScripts/SummaryComments.patch').write_text(''.join(patch),encoding='utf-8',newline='')
changes=json.loads(Path('AgentScripts/SummaryChanges.json').read_text(encoding='utf-8'))
counts=Counter(d['kind'] for d in changes)
report={'files_in_scope':len(list(root.rglob('*.cs'))),'added':dict(counts),'added_total':len(changes),'comment_only_files_compared':unchanged,'code_changes_in_comment_phase':errors,'undocumented_declarations':missing,'invalid_xml':xmlerrors}
Path('AgentScripts/SummaryAudit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(report,ensure_ascii=False))
if errors or missing or xmlerrors:sys.exit(1)
