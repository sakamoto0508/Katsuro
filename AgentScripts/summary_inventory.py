from pathlib import Path
import re,json,sys
root=Path('Assets/Mock/Scripts')
def read(p):
    data=p.read_bytes()
    for enc in ['utf-8','cp932']:
        try:return data.decode(enc).lstrip('\ufeff'),enc
        except UnicodeDecodeError:pass
    raise ValueError(p)
def declarations(text):
    lines=text.splitlines(keepends=True)
    masked=re.sub(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/',lambda m:' ' * len(m.group()),text,flags=re.S)
    spans=[]
    for m in re.finditer(r'\b(?:class|struct|interface|enum)\s+(\w+)',masked):
        start=masked.find('{',m.end());depth=1;end=start+1
        while start>=0 and end<len(masked) and depth:
            depth += (masked[end]=='{')-(masked[end]=='}');end+=1
        if start>=0:spans.append((start,end,m.group(1)))
    offsets=[];pos=0
    for line in lines:offsets.append(pos);pos+=len(line)
    result=[]
    for i,line in enumerate(lines):
        s=line.strip()
        if not re.match(r'(public|private|protected|internal)\b',s):continue
        typ=re.search(r'\b(class|struct|interface|enum)\s+(\w+)',s)
        if typ:kind,name=typ.groups()
        elif '(' in s:
            pre=s.split('(')[0]
            if '=' in pre or 'delegate ' in pre:continue
            name=pre.split()[-1].split('<')[0];kind='method'
        else:continue
        j=i-1
        while j>=0 and (not lines[j].strip() or lines[j].strip().startswith('[')):j-=1
        documented=j>=0 and lines[j].strip().startswith('///')
        owner=next((n for a,b,n in reversed(spans) if a<offsets[i]<b),'')
        result.append({'line':i,'kind':kind,'name':name,'owner':owner,'documented':documented,'signature':s})
    return result
if __name__=='__main__':
    folder=sys.argv[1] if len(sys.argv)>1 else ''
    for p in sorted(root.rglob('*.cs')):
        if folder and not str(p.relative_to(root)).replace('\\','/').startswith(folder):continue
        text,enc=read(p);missing=[d for d in declarations(text) if not d['documented']]
        if not missing:continue
        print(str(p.relative_to(root)),enc)
        lines=text.splitlines()
        for d in missing:
            print(' ',d['kind'],d['name'],d['signature'])
            if d['kind']=='method':print('    '+' / '.join(lines[d['line']+1:d['line']+7]).strip())
