from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import re,json
root=Path('Artifacts/JustAvoidCounter/PlayMode'); rows=json.loads((root/'Final2/Frames.json').read_text())
before=[]
for line in (root/'Comparison/Validation.log').read_text(encoding='utf-8-sig').splitlines():
 m=re.match(r'\d+,[\d.]+,Before-All,FRAME strike=(\d+) index=(\d+) age=([\d.]+)',line)
 if m: before.append((int(m[1]),int(m[2]),float(m[3])))
board=Image.new('RGB',(1280,1320),'#111820');d=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
for stage in (1,2):
 for col in (0,1):
  if col==0:
   r=min((r for r in before if r[0]==stage),key=lambda r:abs(r[2]-.1));im=Image.open(root/f'Comparison/Before-All-Strike{stage}-{r[1]:02}.png').convert('RGB');label=f'Before contact visibility tuning / Strike {stage} / {r[2]*1000:.0f} ms'
  else:
   r=min((r for r in rows if r['condition']=='Bright-All' and r['strike']==stage),key=lambda r:abs(r['age']-.1));im=Image.open(root/f"Final2/Bright-All-Strike{stage}-{r['index']:02}.png").convert('RGB');label=f"After / Strike {stage} / {r['age']*1000:.0f} ms"
  x=col*640;y=(stage-1)*660;d.text((x+8,y+4),label,font=font,fill='white');board.paste(im.resize((640,360),Image.Resampling.LANCZOS),(x,y+32));board.paste(im.crop((440,400,840,550)).resize((640,240),Image.Resampling.NEAREST),(x,y+398));d.text((x+8,y+402),'Same frame detail crop',font=font,fill='#ffffaa',stroke_width=1,stroke_fill='black')
board.save(root/'PlayModeBeforeAfter.png')
# Lossless PNG re-encoding; pixels and capture timestamps stay unchanged.
total_before=total_after=0
for folder in [root/'Final2',root/'Comparison']:
 for p in folder.glob('*.png'):
  total_before+=p.stat().st_size
  with Image.open(p) as original:
   im=original.convert('RGB') if original.mode=='RGBA' and original.getextrema()[-1]==(255,255) else original.copy()
  im.save(p,compress_level=6)
  total_after+=p.stat().st_size
print(f'Lossless capture encoding: {total_before/1048576:.1f} MB -> {total_after/1048576:.1f} MB')
