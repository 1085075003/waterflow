"""Original UI artwork. Rasterized from vector primitives at 3x for native Unity sprites.
No source game bitmap is read or modified. Lato is licensed under SIL OFL 1.1.
"""
from pathlib import Path
import math, json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).parent
RESOURCES=ROOT.parents[1]/'Resources/ArtUpgrade/UI'
PNG=RESOURCES/'Sprites'; SVG=ROOT/'Sources'; PNG.mkdir(parents=True,exist_ok=True); SVG.mkdir(exist_ok=True)
S=3
INK='#243f44'; TEAL='#508e88'; MINT='#a9d1c6'; IVORY='#f0f0e6'; EDGE='#a6beb7'; GOLD='#cfa568'

def new(w,h):
 im=Image.new('RGBA',(w*S,h*S)); return im,ImageDraw.Draw(im)
def rr(d,box,r,fill,outline=None,width=1):
 d.rounded_rectangle(tuple(round(v*S) for v in box),r*S,fill=fill,outline=outline,width=width*S)
def line(d,pts,fill=INK,width=5):
 pts=[(int(x*S),int(y*S)) for x,y in pts];d.line(pts,fill=fill,width=width*S,joint='curve')
 r=width*S/2
 for x,y in [pts[0],pts[-1]]: d.ellipse((x-r,y-r,x+r,y+r),fill=fill)
def poly(d,pts,fill,outline=None,width=1):
 d.polygon([(int(x*S),int(y*S)) for x,y in pts],fill=fill)
 if outline:d.line([(int(x*S),int(y*S)) for x,y in pts+[pts[0]]],fill=outline,width=width*S,joint='curve')
def ellipse(d,box,fill,outline=None,width=1):d.ellipse(tuple(int(v*S) for v in box),fill,outline,width*S)
def save(name,im,svg):
 im.resize((im.width//S,im.height//S),Image.Resampling.LANCZOS).save(PNG/(name+'.png'))
 (SVG/(name+'.svg')).write_text(svg,encoding='utf8')
def doc(w,h,content):return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{content}</svg>'

for name,fill,edge in [('card',IVORY,EDGE),('button','#e7eee7',EDGE),('primary',MINT,'#81b5a9'),('inset','#e3e9e0','#c4d3c8'),('gold','#e9d9b9','#d0b87d'),('hud','#edf0e7','#afc3b7'),('tab','#d9e6df','#9ab4ad'),('badge','#c2ddd2','#97bdad')]:
 im,d=new(192,192)
 rr(d,(3,7,189,191),22,(17,45,43,24))
 rr(d,(3,3,189,187),22,fill,edge,2)
 line(d,[(25,5),(167,5)],'#ffffff90',1)
 save(name,im,doc(192,192,f'<rect x="3" y="7" width="186" height="184" rx="22" fill="#112d2b" opacity=".09"/><rect x="3" y="3" width="186" height="184" rx="22" fill="{fill}" stroke="{edge}" stroke-width="2"/>'))

# The original cajaNivel8b1 is a transparent-centred highlight drawn AFTER its
# text sibling. Preserve that compositing contract so hover never masks labels.
im,d=new(192,192);rr(d,(3,3,189,187),22,None,'#64aa9cc0',3)
save('highlight',im,doc(192,192,'<rect x="3" y="3" width="186" height="184" rx="22" fill="none" stroke="#64aa9c" stroke-opacity=".75" stroke-width="3"/>'))
im,d=new(192,192);rr(d,(3,3,189,187),22,'#83b4a926','#83b4a9c0',2)
save('tab-overlay',im,doc(192,192,'<rect x="3" y="3" width="186" height="184" rx="22" fill="#83b4a9" fill-opacity=".15" stroke="#83b4a9" stroke-opacity=".75" stroke-width="2"/>'))

im,d=new(128,128);ellipse(d,(5,7,123,125),'#112d2b14');ellipse(d,(5,4,123,122),IVORY,EDGE,2)
save('round',im,doc(128,128,f'<circle cx="64" cy="63" r="59" fill="{IVORY}" stroke="{EDGE}" stroke-width="2"/>'))
im,d=new(128,128);ellipse(d,(5,5,123,123),None,EDGE,2)
save('ring',im,doc(128,128,f'<circle cx="64" cy="64" r="59" fill="none" stroke="{EDGE}" stroke-width="2"/>'))

icons={
 'check':[[(28,64),(52,87),(101,36)]],
 'close':[[(36,36),(92,92)],[(92,36),(36,92)]],
 'menu':[[(34,35),(94,35)],[(34,64),(94,64)],[(34,93),(94,93)]],
 'arrow-left':[[(76,30),(42,64),(76,98)],[(44,64),(101,64)]],
 'arrow-right':[[(49,30),(83,64),(49,98)],[(27,64),(81,64)]],
 'undo':[[(45,32),(22,53),(47,74)],[(25,53),(77,53),(92,60),(98,74),(95,89),(83,99),(56,99)]],
 'music':[[(47,88),(47,31),(99,20),(99,77)],[(49,48),(98,37)]],
 'sound':[[(28,51),(46,51),(66,33),(66,95),(46,77),(28,77),(28,51)],[(81,45),(90,54),(92,64),(90,75),(81,84)],[(96,31),(108,45),(113,64),(108,84),(96,99)]],
 'restart':[[(96,41),(101,18),(78,22)]],
 'cart':[[(20,25),(34,25),(43,80),(94,80),(106,42),(39,42)]],
 'lock':[[(40,57),(40,40),(44,28),(55,21),(73,21),(84,28),(88,40),(88,57)]],
 'bulb':[[(49,93),(79,93)],[(51,103),(77,103)],[(57,112),(71,112)],[(47,83),(41,72),(36,57),(39,40),(48,27),(63,22),(80,27),(90,40),(93,57),(88,72),(80,83),(79,93)]],
 'video':[[(24,34),(104,34),(104,94),(24,94),(24,34)]],
 'footsteps':[], 'rotate':[], 'no-ads':[], 'play':[], 'star':[],
}
for name,paths in icons.items():
 im,d=new(128,128);svg=''
 for pts in paths:
  line(d,pts,INK,6);svg+=f'<polyline points="'+ ' '.join(f'{x},{y}' for x,y in pts)+f'" fill="none" stroke="{INK}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>'
 if name=='music':
  ellipse(d,(26,78,49,99),INK);ellipse(d,(78,67,101,88),INK);svg+=f'<ellipse cx="37" cy="89" rx="12" ry="10" fill="{INK}"/><ellipse cx="89" cy="78" rx="12" ry="10" fill="{INK}"/>'
 if name=='restart':
  d.arc((22*S,24*S,104*S,108*S),39,313,fill=INK,width=6*S);svg+=f'<path d="M96 40 A41 41 0 1 0 100 90" fill="none" stroke="{INK}" stroke-width="6"/>'
 if name=='cart':
  ellipse(d,(45,92,57,104),INK);ellipse(d,(85,92,97,104),INK);svg+=f'<circle cx="51" cy="98" r="6" fill="{INK}"/><circle cx="91" cy="98" r="6" fill="{INK}"/>'
 if name=='lock':
  rr(d,(30,55,98,109),9,None,INK,6);line(d,[(64,76),(64,89)],INK,6);svg+=f'<rect x="30" y="55" width="68" height="54" rx="9" fill="none" stroke="{INK}" stroke-width="6"/><path d="M64 76v13" stroke="{INK}" stroke-width="6"/>'
 if name in ['play','video']:
  pts=[(52,44),(82,64),(52,84)];poly(d,pts,INK);svg+=f'<path d="M52 44L82 64 52 84Z" fill="{INK}"/>'
 if name=='star':
  pts=[(64+49*math.sin(i*math.pi/5)*(1 if i%2==0 else .47),64-49*math.cos(i*math.pi/5)*(1 if i%2==0 else .47)) for i in range(10)]
  poly(d,pts,GOLD,'#b38954',2);svg+=f'<polygon points="'+ ' '.join(f'{x:.2f},{y:.2f}' for x,y in pts)+f'" fill="{GOLD}" stroke="#b38954" stroke-width="2"/>'
 if name=='rotate':
  d.arc((18*S,36*S,110*S,92*S),12,338,INK,6*S);poly(d,[(107,31),(110,57),(85,49)],INK);svg+=f'<path d="M109 58A46 28 0 1 0 107 47" fill="none" stroke="{INK}" stroke-width="6"/><path d="M107 31L110 57 85 49Z" fill="{INK}"/>'
 if name=='footsteps':
  for x,y in [(42,78),(83,47)]:
   ellipse(d,(x-10,y-18,x+10,y+19),INK)
   for xx,yy,rrr in [(x-8,y-27,5),(x+4,y-29,5),(x+14,y-23,4)]:ellipse(d,(xx-rrr,yy-rrr,xx+rrr,yy+rrr),INK)
   svg+=f'<ellipse cx="{x}" cy="{y}" rx="10" ry="19" fill="{INK}"/><circle cx="{x-8}" cy="{y-27}" r="5" fill="{INK}"/><circle cx="{x+4}" cy="{y-29}" r="5" fill="{INK}"/><circle cx="{x+14}" cy="{y-23}" r="4" fill="{INK}"/>'
 if name=='no-ads':
  rr(d,(23,34,105,94),10,None,INK,5);line(d,[(29,105),(99,23)],TEAL,7);svg+=f'<rect x="23" y="34" width="82" height="60" rx="10" stroke="{INK}" stroke-width="5" fill="none"/><path d="M29 105L99 23" stroke="{TEAL}" stroke-width="7"/>'
 save(name,im,doc(128,128,svg))

def spaced(d,text,font,center,y,spacing,fill):
 widths=[d.textlength(c,font=font) for c in text];x=center-(sum(widths)+spacing*S*(len(text)-1))/2
 for c,w in zip(text,widths):d.text((x,y*S),c,font=font,fill=fill,stroke_width=0);x+=w+spacing*S

bold=RESOURCES/'Fonts/Lato-Bold.ttf';regular=RESOURCES/'Fonts/Lato-Regular.ttf'
im,d=new(1024,194)
rr(d,(0,0,1024,184),24,'#f0f0e6f2')
font=ImageFont.truetype(str(bold),122*S)
spaced(d,'FLOW WATER',font,570*S,16,2,INK)
drop=[(68,20),(30,72),(19,97),(21,126),(33,149),(56,161),(81,160),(104,145),(116,121),(115,96),(101,72)]
poly(d,drop,TEAL);line(d,[(42,120),(51,135),(70,140)],'#d5e6dc',5)
save('wordmark',im,doc(1024,194,f'<rect width="1024" height="184" rx="24" fill="{IVORY}" opacity=".95"/><path d="M68 20C45 52 19 79 19 109C19 178 117 178 117 109C117 79 90 52 68 20Z" fill="{TEAL}"/><text x="155" y="140" font-family="Lato" font-weight="700" font-size="122" letter-spacing="2" fill="{INK}">FLOW WATER</text>'))
im,d=new(512,121);rr(d,(0,6,512,115),14,'#f0f0e6f2');font=ImageFont.truetype(str(bold),30*S);spaced(d,'3D WATER PUZZLE',font,256*S,38,3,TEAL)
save('subtitle',im,doc(512,121,f'<rect y="6" width="512" height="109" rx="14" fill="{IVORY}" opacity=".95"/><text x="256" y="75" text-anchor="middle" font-family="Lato" font-size="30" font-weight="700" letter-spacing="3" fill="{TEAL}">3D WATER PUZZLE</text>'))

for name,top,bot in [('background-day','#e2ece8','#9cb7b6'),('background-night','#172f3c','#35535d'),('background-night-tinted','#6aa2bc','#a0ced9'),('background-soft','#dbe9e5','#88a8ac')]:
 im,d=new(256,512);top=tuple(bytes.fromhex(top[1:]));bot=tuple(bytes.fromhex(bot[1:]))
 for y in range(im.height):
  t=y/(im.height-1);col=tuple(round(top[i]*(1-t)+bot[i]*t) for i in range(3))+(255,);d.line((0,y,im.width,y),fill=col)
 save(name,im,doc(256,512,f'<defs><linearGradient id="g" x2="0" y2="1"><stop stop-color="#{bytes(top).hex()}"/><stop offset="1" stop-color="#{bytes(bot).hex()}"/></linearGradient></defs><path d="M0 0H256V512H0Z" fill="url(#g)"/>'))

# Store pack thumbnails: six original miniature isometric water gardens.
for variant in range(1,7):
 im,d=new(512,320);shapes=[]
 def iso(x,y,z=0):return (256+(x-y)*41,95+(x+y)*20-z*35)
 def face(pts,c):
  poly(d,pts,c);shapes.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}' for x,y in pts)+f'" fill="{c}"/>')
 def tile(x,y,z=0,c='#c8d5bf'):
  face([iso(x,y,z),iso(x+1,y,z),iso(x+1,y+1,z),iso(x,y+1,z)],c)
 def block(x,y,z=0,h=1):
  face([iso(x,y+1,z),iso(x+1,y+1,z),iso(x+1,y+1,z+h),iso(x,y+1,z+h)],'#9faca3')
  face([iso(x+1,y,z),iso(x+1,y+1,z),iso(x+1,y+1,z+h),iso(x+1,y,z+h)],'#8a9e99')
  face([iso(x,y,z+h),iso(x+1,y,z+h),iso(x+1,y+1,z+h),iso(x,y+1,z+h)],'#e5e8da')
 face([iso(-.12,4.12,-.18),iso(4.12,4.12,-.18),iso(4.12,4.12,0),iso(-.12,4.12,0)],'#7a9388')
 face([iso(4.12,-.12,-.18),iso(4.12,4.12,-.18),iso(4.12,4.12,0),iso(4.12,-.12,0)],'#627f79')
 for x in range(4):
  for y in range(4):tile(x,y,0,['#bbc9ae','#c3cfb6'][(x+y)%2])
 for x in range(4):
  for y in range(4):
   water= (x==1 or y==2) if variant==1 else ((x in (1,2) and y in(1,2)) if variant==2 else ((x+y==3 or x==1) if variant==3 else ((x in(0,3) or y in(0,3)) if variant==4 else ((x%2==0 or y%2==0) if variant==5 else (x==y or x+y==3)))))
   if water:
    block(x,y,0,.18);p=[iso(x+.1,y+.1,.19),iso(x+.9,y+.1,.19),iso(x+.9,y+.9,.19),iso(x+.1,y+.9,.19)];face(p,'#7dbdbb' if variant!=6 else '#85a5c2')
 block(0,0,0,1.65)
 stream=[iso(.55,.48,1.5),iso(.68,.53,1.5),iso(1.42,1.2,.22),iso(1.18,1.25,.22)];face(stream,'#81c7c4')
 for (x,y) in [(2.5,2.5),(1.5,3.5)][:1 if variant<4 else 2]:
  xx,yy=iso(x,y,.2);ellipse(d,(xx-17,yy-6,xx+17,yy+6),'#cbd4c4');ellipse(d,(xx-12,yy-4,xx+12,yy+4),'#7fbeba');line(d,[(xx,yy),(xx,yy-30)],'#d2bd89',6);ellipse(d,(xx-12,yy-32,xx+12,yy-24),'#e3d6ac')
  shapes.append(f'<ellipse cx="{xx}" cy="{yy}" rx="17" ry="6" fill="#cbd4c4"/><path d="M{xx} {yy}v-30" stroke="#d2bd89" stroke-width="6"/>')
 save('garden-'+str(variant),im,doc(512,320,''.join(shapes)))

im,d=new(192,192);rr(d,(0,0,192,192),0,(255,255,255,0));save('transparent',im,doc(192,192,''))
im,d=new(16,16);rr(d,(0,0,16,16),0,'#ffffff');save('overlay',im,doc(16,16,'<path d="M0 0H16V16H0Z" fill="white"/>'))

(ROOT/'ARTWORK.md').write_text('''# Flow Water — Water Garden UI

Original vector-based artwork generated by `generate_ui.py`. Sources are in `Sources/`, rasterized native Unity sprites and fonts are in `Assets/Resources/ArtUpgrade/UI/`. Icons use a consistent six-unit stroke with round caps. Nine-slice panels preserve scale without stretching corner radii. This generator does not read or modify original game bitmaps.

Palette: ink #243f44, flowing teal #508e88, mint #a9d1c6, ivory #f0f0e6, stone edge #a6beb7, brass #cfa568.

Typography: Lato Regular / Bold by Łukasz Dziedzic, downloaded from https://github.com/google/fonts/tree/main/ofl/lato . License: SIL Open Font License 1.1, see Fonts/OFL.txt. Noto Sans SC Regular is a static CJK fallback from https://github.com/notofonts/noto-cjk/tree/main/Sans/SubsetOTF/SC , under SIL OFL 1.1 (Fonts/Noto-OFL.txt). Typefaces are embedded unchanged.

Apply via Tools > Flow Water > Art Upgrade > Apply UI Theme. Scene hierarchy, node names, child ordering, event bindings, raycast values and input targets are retained. Only native Image/Text visual values and visual RectTransform rotations are edited. RecoveryUI-built exceptional service dialogs load the same fonts and rounded panel sprites; only display assignments in BuildOverlay and its visual factories were changed.
''',encoding='utf8')
print('Generated',len(list(PNG.glob('*.png'))),'original UI sprites')
