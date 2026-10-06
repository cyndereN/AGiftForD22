"""Rasterize legible shop lettering for the Meshy card mesh; creates no geometry."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'assets/recordshop/v4/labels';out.mkdir(parents=True,exist_ok=True)
labels={'new':['NEW','ARRIVALS'],'rock':['ROCK'],'jazz':['JAZZ'],'electronic':['ELECTRONIC'],'rock_wall':['ROCK'],'soul':['SOUL'],'used':['USED'],'used35':['USED','35 / LP'],'demo':['LOCAL','DEMO'],'cash':['CASH','收银'],'open':['OPEN','12:00 — 22:00'],'staff':['STAFF PICKS']}
for key,lines in labels.items():
 image=Image.new('RGB',(1024,512),(223,210,181));draw=ImageDraw.Draw(image)
 draw.rectangle((17,15,1006,495),outline=(161,145,116),width=3)
 for j,line in enumerate(lines):
  size=210 if len(lines)==1 else 145
  fontpath='/System/Library/Fonts/Hiragino Sans GB.ttc' if key=='cash' and j else '/System/Library/Fonts/Supplemental/Courier New Bold.ttf' if key in ['used','used35','demo','soul'] else '/System/Library/Fonts/Supplemental/Arial Bold.ttf'
  while True:
   font=ImageFont.truetype(fontpath,size)
   if draw.textbbox((0,0),line,font=font)[2]<940 or size<45:break
   size-=4
  y=256 if len(lines)==1 else 170+j*175
  draw.text((512,y),line,font=font,fill=(52,44,36) if key not in ['used','used35','cash'] else (125,44,32),anchor='mm')
 image.save(out/(key+'.png'))
print('LABEL_TEXTURES',len(labels))
