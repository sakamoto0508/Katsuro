import zlib,struct,math,uuid,os,sys,re
out=sys.argv[1]; os.makedirs(out,exist_ok=True)
tmpl=open(sys.argv[2],encoding='utf-8').read()
def png(path,w,h,f):
    raw=b''.join(b'\x00'+b''.join(bytes((255,255,255,max(0,min(255,int(round(f(x,y)*255)))))) if True else b'' for x in range(w)) for y in range(h))
    def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    data=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b'')
    open(path,'wb').write(data)
def black(path,w,h,f):
    raw=b''.join(b'\x00'+b''.join(bytes((0,0,0,max(0,min(255,int(round(f(x,y)*255)))))) for x in range(w)) for y in range(h))
    def chunk(t,d): return struct.pack('>I',len(d))+t+d+struct.pack('>I',zlib.crc32(t+d)&0xffffffff)
    open(path,'wb').write(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))
guids={}
def meta(name,border):
    g=uuid.uuid4().hex; guids[name]=g
    m=re.sub(r'guid: \w+','guid: '+g,tmpl,count=1)
    m=m.replace('spriteBorder: {x: 0, y: 0, z: 0, w: 0}','spriteBorder: {x: %d, y: %d, z: %d, w: %d}'%border)
    m=re.sub(r'spriteID: \w+','spriteID: '+uuid.uuid4().hex,m)
    m=m.replace('wrapU: 1','wrapU: 1')
    open(os.path.join(out,name+'.png.meta'),'w',encoding='utf-8',newline='\n').write(m)
def cov(d): return max(0.0,min(1.0,0.5-d))  # 距離→被覆率（1px AA）
# 1. 2px枠
def frame(x,y,s=32,t=2):
    cx,cy=x+0.5,y+0.5
    d=min(cx,cy,s-cx,s-cy)
    return cov(d-t)
png(os.path.join(out,'SetupFrame.png'),32,32,frame); meta('SetupFrame',(6,6,6,6))
# 2. 四隅ブラケット
def corners(x,y,s=64,t=3,L=18):
    cx,cy=x+0.5,y+0.5
    a=0
    for (px,py) in ((cx,cy),(s-cx,cy),(cx,s-cy),(s-cx,s-cy)):
        if px<=L and py<=t: a=1
        if py<=L and px<=t: a=1
    return a
png(os.path.join(out,'SetupFocusCorners.png'),64,64,corners); meta('SetupFocusCorners',(24,24,24,24))
# 3. ビネット（黒）
def vig(x,y,w=256,h=256):
    u=(x+0.5)/w*2-1; v=(y+0.5)/h*2-1
    r=math.sqrt(u*u*0.8+v*v*1.0)
    t=max(0,min(1,(r-0.25)/1.0))
    t=t*t*(3-2*t)
    return 0.25+0.7*t
black(os.path.join(out,'SetupVignette.png'),256,256,vig); meta('SetupVignette',(0,0,0,0))
# 4. 上→下で濃くなるグラデーション（PNG上端=画像上）
png(os.path.join(out,'SetupGradient.png'),8,128,lambda x,y:((y+0.5)/128)**1.6); meta('SetupGradient',(0,0,0,0))
# 5. 両端が消える線
def fl(x,y,w=256):
    u=(x+0.5)/w; t=min(u,1-u)/0.35
    return min(1,t)**1.5
png(os.path.join(out,'SetupFadeLine.png'),256,4,fl); meta('SetupFadeLine',(0,0,0,0))
# 6. 菱形
png(os.path.join(out,'SetupDiamond.png'),32,32,lambda x,y:cov((abs(x+0.5-16)+abs(y+0.5-16))-14)); meta('SetupDiamond',(0,0,0,0))
# 7. 左から右へ消える帯（見出し・エラー帯の下地）
png(os.path.join(out,'SetupFadeRight.png'),128,8,lambda x,y:(1-(x+0.5)/128)**1.3); meta('SetupFadeRight',(0,0,0,0))
for k,v in guids.items(): print(k,v)
