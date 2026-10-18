import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=json.loads(Path('Artifacts/AKI123/ComboPoses.json').read_text(encoding='utf-8-sig'))['poses']
links=[(0,1),(1,2),(2,3),(2,4),(4,5),(5,6),(2,7),(7,8),(8,9),(0,10),(10,11),(11,12),(0,13),(13,14),(14,15)]
im=Image.new('RGB',(1920,980),'#f4f7fb');d=ImageDraw.Draw(im);f=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
d.text((20,15),'Enemy Avatar / Edit Mode skeletal sample / red: blade colliders / diagonal depth projection',fill='#18304f',font=f)
for n in range(1,4):
 poses=[x for x in p if x['clip'].endswith('Combo'+str(n))]
 print('COMBO',n,[(round(a['time'],2),round(math.sqrt(sum((a['positions'][9][k]-b['positions'][9][k])**2 for k in ['x','y','z']))/.05,2)) for a,b in zip(poses,poses[1:])])
 for j in range(8):
  pose=poses[round(j*(len(poses)-1)/7)];x=j*240;y=50+(n-1)*310
  def project(v):return (x+120+95*(.85*v['x']+.5*v['z']),y+275+95*(-v['y']+.12*v['z']))
  d.text((x+10,y+5),f'Combo{n}  {pose["time"]:.2f}s',fill='#18304f',font=f)
  for a,b in links:d.line((project(pose['positions'][a]),project(pose['positions'][b])),fill='#395e83',width=4)
  for a,b in zip(pose['blade'],pose['blade'][1:]):d.line((project(a),project(b)),fill='#d13648',width=5)
im.save('Artifacts/AKI123/ComboPoseComparison.png')
