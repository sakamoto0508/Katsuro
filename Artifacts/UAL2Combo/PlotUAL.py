import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
rows=json.loads(Path('Artifacts/UAL2Combo/MotionSamples.json').read_text());f=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
for row in rows:
 if not row['apply']:continue
 frames=row['frames'];im=Image.new('RGB',(2400,960),'#f4f7fb');d=ImageDraw.Draw(im);d.text((10,10),row['kind']+' / 0.1s samples / red: blade collider chain (enemy Avatar, Edit Mode)',font=f,fill='#123'); links=[(0,1),(1,2),(2,3),(2,4),(4,5),(5,6),(2,7),(7,8),(8,9),(0,10),(10,11),(11,12),(0,13),(13,14),(14,15)]
 poses=frames[::6]
 for j,p in enumerate(poses):
  x=(j%12)*200;y=40+(j//12)*240
  def pt(v):return(x+100+85*(v['x']*.85+v['z']*.5),y+215+85*(-v['y']+v['z']*.12))
  for a,b in links:d.line((pt(p['bones'][a]),pt(p['bones'][b])),fill='#3a6381',width=3)
  for a,b in zip(p['blade'],p['blade'][1:]):d.line((pt(a),pt(b)),fill='#cf364c',width=4)
  d.text((x+5,y),f'{p["t"]:.2f}s',font=f,fill='#123')
 im.save('Artifacts/UAL2Combo/'+row['kind']+'PoseSheet.png')
 # weapon tip motion relative hip, 5-frame mean speed for locating primary cuts.
 speeds=[]
 for a,b in zip(frames,frames[1:]):
  aa=[a['blade'][-1][k]-a['bones'][0][k] for k in ['x','y','z']];bb=[b['blade'][-1][k]-b['bones'][0][k] for k in ['x','y','z']]
  speeds.append(math.dist(aa,bb)*60)
 peaks=[(frames[i]['t'],round(v,1)) for i,v in enumerate(speeds) if i>2 and i<len(speeds)-3 and v==max(speeds[i-3:i+4]) and v>3]
 print(row['kind'],peaks)
