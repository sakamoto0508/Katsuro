import json,math
from PIL import Image,ImageDraw,ImageFont
rows=json.load(open('Artifacts/RushWarp/Candidates.json'));f=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
for p in rows:
 if (p['name'].endswith('Sprint') and p['yaw']!=0) or (p['name'].endswith('Dash') and p['yaw']!=180):continue
 im=Image.new('RGB',(2400,1000),'#f4f7fb');d=ImageDraw.Draw(im)
 links=[(0,1),(1,2),(2,3),(2,4),(4,5),(5,6),(2,7),(7,8),(8,9),(0,10),(10,11),(11,12),(0,13),(13,14),(14,15)]
 for j,frame in enumerate(p['frames'][::6]):
  x=(j%12)*200;y=(j//12)*250
  def pt(v):return(x+100+90*(v['x']*.85+v['z']*.5),y+220+90*(-v['y']+v['z']*.12))
  for a,b in links:d.line((pt(frame['bones'][a]),pt(frame['bones'][b])),fill='#3a6381',width=3)
  for a,b in zip(frame['blade'],frame['blade'][1:]):d.line((pt(a),pt(b)),fill='#cf364c',width=4)
  d.text((x+4,y+4),f'{frame["t"]:.2f}s',font=f,fill='#123')
 name='Sprint' if p['name'].endswith('Sprint') else 'Dash';im.save('Artifacts/RushWarp/'+name+'Poses.png')
 speeds=[]
 for a,b in zip(p['frames'],p['frames'][1:]):
  aa=[a['blade'][-1][k]-a['bones'][0][k] for k in ['x','y','z']];bb=[b['blade'][-1][k]-b['bones'][0][k] for k in ['x','y','z']];speeds.append(math.dist(aa,bb)*60)
 print(name,[(p['frames'][i]['t'],round(v,2)) for i,v in enumerate(speeds) if i>2 and i<len(speeds)-3 and v==max(speeds[i-3:i+4]) and v>3])
