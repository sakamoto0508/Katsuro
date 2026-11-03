import re,sys,collections,json
raw=open(sys.argv[1],encoding='utf-8',newline='').read()
assert '\r\r' not in raw and re.search(r'[^\r]\n',raw) is None, 'line ending mix'
t=raw.replace('\r\n','\n')
heads=re.findall(r'^--- !u!(\d+) &(-?\d+)',t,re.M)
ids=[h[1] for h in heads]
dup=[k for k,v in collections.Counter(ids).items() if v>1]
assert not dup, dup
parts=re.split(r'^--- !u!\d+ &(-?\d+).*\n',t,flags=re.M)
docs={parts[i]:parts[i+1] for i in range(1,len(parts),2)}
cls={h[1]:h[0] for h in heads}
errs=[]
for fid,b in docs.items():
    for m in re.finditer(r'\{fileID: (-?\d+)\}',b):
        r=m.group(1)
        if r!='0' and r not in docs: errs.append((fid,'dangling',r))
for fid,b in docs.items():
    if cls[fid]=='1':
        for c in re.findall(r'component: \{fileID: (-?\d+)\}',b):
            g=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',docs[c]).group(1)
            if g!=fid: errs.append((fid,'comp owner',c))
    if cls[fid]=='224':
        ch=re.findall(r'^  - \{fileID: (-?\d+)\}',b.split('m_Father')[0],re.M)
        for c in ch:
            f=re.search(r'm_Father: \{fileID: (-?\d+)\}',docs[c]).group(1)
            if f!=fid: errs.append((fid,'father',c))
        f=re.search(r'm_Father: \{fileID: (-?\d+)\}',b).group(1)
        if f!='0' and fid not in re.findall(r'- \{fileID: (-?\d+)\}',docs[f].split('m_Father')[0]): errs.append((fid,'not child of',f))
    if cls[fid]=='114':
        if not re.search(r'm_Script: \{fileID: 11500000, guid: [0-9a-f]{32}',b): errs.append((fid,'script'))
# 型チェック：RunSetupUIの参照先
def ident(r): return re.search(r'm_EditorClassIdentifier: (.*)',docs[r]).group(1) if cls[r]=='114' else 'class'+cls[r]
ui=docs['5778134622391078993']
for line in ui.split('\n'):
    m=re.match(r'\s+(_\w+)?:? ?-? ?\{fileID: (-?\d+)\}',line)
    if m and m.group(2)!='0': print(line.strip()[:40].ljust(40), ident(m.group(2)))
for k in ['5778134622391078993','9043415182819911970','4759766509143167490','7462631114155061920']:
    print(k, k in docs)
# 文字列確認
for fid,b in docs.items():
    m=re.search(r'^  m_text: (.*)$',b,re.M)
    if m:
        g=re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b).group(1)
        name=re.search(r'm_Name: (.*)',docs[g]).group(1)
        print('TXT',name, json.loads(m.group(1)) if m.group(1).startswith('"') else m.group(1))
print('errors',errs[:20],len(errs))
