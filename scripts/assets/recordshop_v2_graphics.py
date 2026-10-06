"""Author fictional period paper props and portable surface maps. Requires Pillow + NumPy."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter,ImageOps
import numpy as np
import random,json
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'assets/recordshop/v2/textures'
random.seed(2006)
# Environment variable allows the same generator to run with a Windows CJK font.
import os
FONT=os.environ.get('D22_CJK_FONT','/System/Library/Fonts/Supplemental/Songti.ttc')
def font(size):return ImageFont.truetype(FONT,size)
def wear(im,amount=5):
 a=np.array(im.convert('RGB')).astype(float);rng=np.random.default_rng(22)
 a+=rng.normal(0,amount,a.shape[:2])[...,None]
 return Image.fromarray(np.uint8(np.clip(a,0,255)))
for name in ['plaster','stone']:
 im=Image.open(DEST/(name+'-albedo.png')).convert('RGB');h=np.asarray(im.convert('L').filter(ImageFilter.GaussianBlur(1)),dtype=float)/255
 gy,gx=np.gradient(h);v=np.stack([-gx*3,-gy*3,np.ones_like(h)],axis=-1);v/=np.linalg.norm(v,axis=2)[...,None]
 Image.fromarray(np.uint8((v*.5+.5)*255)).save(DEST/(name+'-normal.png'))
 Image.fromarray(np.uint8(np.clip(190+(1-h)*35,0,255))).save(DEST/(name+'-rough.png'))
# Distinct editorial layouts, actual readable Chinese, same fictional bands as D22.
headings=['地下的声音','周末 / 现场','翻到 B 面','独立发行','夜班列车','噪音练习','小房间里的回声','唱片交换日','今晚 D-22','失真 / LIVE','新到磁带','门票在柜台']
for i,title in enumerate(headings):
 w,h=768,1024;bg=[(202,187,150),(194,175,143),(213,200,171),(159,157,133)][i%4]
 im=Image.new('RGB',(w,h),bg);d=ImageDraw.Draw(im)
 ink=(33,33,27);red=(113,43,36)
 d.rectangle((25,25,w-25,h-25),outline=ink,width=3)
 d.text((50,46),'独 立 声 音    /    2006',font=font(28),fill=ink)
 d.text((48,108),title,font=font(64 if len(title)<7 else 49),fill=red if i%3==0 else ink)
 src=Image.open(ROOT/f'assets/d22/v9/textures/band-{i%6}.jpg').convert('L')
 src=ImageOps.fit(src,(660,505));src=ImageOps.colorize(ImageOps.autocontrast(src),ink,bg)
 im.paste(src,(54,215));d=ImageDraw.Draw(im)
 d.text((54,750),'D-22  /  北京 · 五道口' if i%3==0 else 'SIDE B  /  唱片 · 磁带 · 小刊物',font=font(32),fill=ink)
 d.line((52,812,715,812),fill=ink,width=4)
 d.text((54,840),['周五 20:30    现场 / 交流','欢迎试听    请轻拿轻放','自制发行 · 只剩少量','把声音带回家'][i%4],font=font(29),fill=ink)
 d.text((54,920),'NO. %03d        不止一面 / ANOTHER SIDE'%(i+1),font=font(23),fill=ink)
 # Torn edge flecks, print dropout, tape stains.
 for j in range(180):
  x=random.randint(10,w-10);y=random.randint(10,h-10);r=random.randrange(1,4);d.ellipse((x,y,x+r,y+r),fill=bg)
 wear(im).save(DEST/f'flyer-{i:02d}.jpg',quality=93)
labels=['新到 / NEW','华语独立','摇滚 / ROCK','打口 · 进口','磁带  10元起','试听请找店主','旧唱片 / USED','小刊物 / ZINES']
for i,t in enumerate(labels):
 im=Image.new('RGB',(768,256),(194,173,125));d=ImageDraw.Draw(im)
 d.text((27,65),t,font=font(62),fill=(38,36,27));d.line((20,220,748,220),fill=(104,82,52),width=2)
 wear(im,8).save(DEST/f'card-{i}.jpg',quality=94)
# Oxblood painted timber: retain inherited real wood grain and fine cracks.
wood=Image.open(ROOT/'assets/d22/textures/fine_grained_wood_Diffuse.jpg').convert('L').resize((2048,2048))
a=np.asarray(wood,dtype=float)/255
red=np.stack([86+a*38,29+a*25,25+a*22],axis=-1)
rng=np.random.default_rng(69);mask=(a<.12)&(rng.random(a.shape)>.4);red[mask]=[136,116,84]
Image.fromarray(np.uint8(red)).save(DEST/'oxide-timber.jpg',quality=94)
# Woven rug designed as a repeated geometric textile, not a flat coloured rectangle.
im=Image.new('RGB',(768,1536),(75,38,31));d=ImageDraw.Draw(im)
for inset,col,width in [(14,(163,126,78),12),(37,(40,42,36),18),(64,(142,103,64),9),(84,(108,57,40),8)]:d.rectangle((inset,inset,768-inset,1536-inset),outline=col,width=width)
for y in range(160,1420,138):
 for x in range(165,650,138):
  d.polygon([(x,y-51),(x+45,y),(x,y+51),(x-45,y)],fill=(144,94,58),outline=(41,47,43))
  d.polygon([(x,y-24),(x+20,y),(x,y+24),(x-20,y)],fill=(51,63,58))
for x in range(2,767,3):d.line((x,0,x,1535),fill=(99,67,46),width=1)
wear(im,12).save(DEST/'woven-runner.jpg',quality=94)
# Reference crop excludes the screenshot's browser chrome.
ref=Image.open('/var/folders/hj/3fjyhjt93m7c5t7sztk1h8200000gn/T/codex-clipboard-2689c293-49c5-415c-8858-0eceb254b30e.png')
w,h=ref.size;ref.crop((int(w*.225),int(h*.155),int(w*.66),int(h*.90))).save(ROOT/'design/recordshop-v2/user-reference.jpg',quality=94)
print('Authored 12 flyers, 8 category cards, rug, oxide paint and normal/roughness maps.')
