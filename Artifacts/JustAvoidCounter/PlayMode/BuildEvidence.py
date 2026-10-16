from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import re,json,statistics
root=Path('Artifacts/JustAvoidCounter/PlayMode')
source=root/'Final2'
rows=[]
for line in (source/'Validation.log').read_text(encoding='utf-8-sig').splitlines():
 m=re.match(r'(\d+),([\d.]+),([^,]+),FRAME strike=(\d+) index=(\d+) age=([\d.]+) animatorSpeed=([\d.]+)',line)
 if m: rows.append(dict(frame=int(m[1]),time=float(m[2]),condition=m[3],strike=int(m[4]),index=int(m[5]),age=float(m[6]),speed=float(m[7])))
(source/'Frames.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
def pick(condition,strike,age=.1):
 matches=[r for r in rows if r['condition']==condition and r['strike']==strike]
 r=min(matches,key=lambda r:abs(r['age']-age)); p=source/f"{condition}-Strike{strike}-{r['index']:02}.png"
 return Image.open(p).convert('RGB'),r
board=Image.new('RGB',(1920,1350),'#111820');d=ImageDraw.Draw(board)
d.text((20,12),'ACTUAL PLAY MODE GAME VIEW - 1280 x 720 - existing weapon trigger contacts',font=font,fill='white')
for stage in (1,2):
 for col,mode in enumerate(('CutOnly','BloodOnly','All')):
  im,r=pick('Bright-'+mode,stage); x=col*640;y=55+(stage-1)*645
  d.text((x+12,y),f"Strike {stage} / {mode} / {r['age']*1000:.0f} ms / animator {r['speed']:g}",font=small,fill='white')
  board.paste(im.resize((640,360),Image.Resampling.LANCZOS),(x,y+28))
  board.paste(im.crop((440,400,840,550)).resize((640,240),Image.Resampling.NEAREST),(x,y+390))
  d.text((x+8,y+394),'Detail crop of the same Game View frame',font=small,fill='#ffffaa',stroke_width=1,stroke_fill='black')
board.save(root/'PlayModeComparison.png')
# Whole Game View frames for background/distance review.
bg=Image.new('RGB',(1280,1530),'#111820');d=ImageDraw.Draw(bg)
for i,cond in enumerate(('Bright-All','RedGrass-All','Dark-All','Near-All')):
 for j,stage in enumerate((1,2)):
  im,r=pick(cond,stage);x=j*640;y=i*380
  d.text((x+10,y+5),f"{cond} / Strike {stage} / {r['age']*1000:.0f} ms",font=small,fill='white');bg.paste(im.resize((640,360),Image.Resampling.LANCZOS),(x,y+24))
bg.save(root/'Backgrounds.png')
# Unmodified full-resolution copies are included beside the board.
for stage in (1,2):
 im,r=pick('Bright-All',stage);im.save(root/f'GameView-Strike{stage}.png')
# Exact native capture timing, not an assertion of 60 fps.
stats={}
for cond in sorted({r['condition'] for r in rows}):
 part=[r for r in rows if r['condition']==cond];gaps=[]
 for stage in (1,2):
  p=[r for r in part if r['strike']==stage];gaps += [b['time']-a['time'] for a,b in zip(p,p[1:])]
 stats[cond]={'frames':len(part),'median_interval_ms':round(statistics.median(gaps)*1000,1),'max_interval_ms':round(max(gaps)*1000,1)}
(root/'CaptureStatistics.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats,indent=2))
