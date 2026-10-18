import json
from PIL import Image,ImageDraw,ImageFont
rows=json.load(open('Artifacts/UAL2Windup/LeadInPoses.json'))
im=Image.new('RGB',(1600,800),'#f4f7fb');d=ImageDraw.Draw(im);f=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
links=[(0,1),(1,2),(2,3),(2,4),(4,5),(5,6),(2,7),(7,8),(8,9),(0,10),(10,11),(11,12),(0,13),(13,14),(14,15)]
for j,p in enumerate(rows):
 x=(j%4)*400;y=(j//4)*400
 def pt(v):return(x+200+150*(v['x']*.85+v['z']*.5),y+360+150*(-v['y']+v['z']*.12))
 for a,b in links:d.line((pt(p['bones'][a]),pt(p['bones'][b])),fill='#3a6381',width=4)
 for a,b in zip(p['blade'],p['blade'][1:]):d.line((pt(a),pt(b)),fill='#cf364c',width=5)
 d.text((x+12,y+12),p['kind']+' clip '+str(p['t'])+'s',font=f,fill='#123')
im.save('Artifacts/UAL2Windup/LeadInPoses.png')
