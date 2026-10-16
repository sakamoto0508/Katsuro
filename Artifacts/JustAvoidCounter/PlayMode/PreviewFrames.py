from PIL import Image
from pathlib import Path
root=Path('Artifacts/JustAvoidCounter/PlayMode/Final')
for name in ['Bright-BloodOnly-Strike1-04.png','Bright-BloodOnly-Strike2-04.png','Bright-All-Strike1-04.png','Bright-All-Strike2-04.png']:
 p=root/name
 if p.exists(): Image.open(p).transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(root/('View-'+name))
