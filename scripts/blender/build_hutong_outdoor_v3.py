"""Measured residential hutong, circa 2003–2006. Blender architecture + Meshy props.

Independent exterior scene. Existing shop/livehouse interiors are never opened.
Run: Blender -b --python scripts/blender/build_hutong_outdoor_v3.py
"""
import bpy, math, random, json, hashlib, os
from pathlib import Path
from mathutils import Vector, Matrix
from math import pi, sin, cos

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'blender/verification/hutong-outdoor-v3'; OUT.mkdir(parents=True,exist_ok=True)
DESIGN=ROOT/'design/hutong-outdoor-v3'; DESIGN.mkdir(parents=True,exist_ok=True)
random.seed(22)
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene; s.name='D22_HutongOutdoor_v3_Residential'; s.unit_settings.system='METRIC'; s.unit_settings.scale_length=1
collections={}
for name in ['01 Architecture','02 Roofs','03 Ground repairs','04 Utilities','05 Doorstep life','06 Meshy props','07 Signs','08 Lights','09 Cameras','10 Scale reference 1.75m']:
 c=bpy.data.collections.new(name); s.collection.children.link(c); collections[name]=c
source_records=[]; blockers=[]; roof_states=[]

def move(o,group):
 for c in list(o.users_collection): c.objects.unlink(o)
 collections[group].objects.link(o)
 return o

def mat(name,color,rough=.8,metal=0,noise=0,brick=False):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
 if noise or brick:
  geo=n.new('ShaderNodeNewGeometry'); sep=n.new('ShaderNodeSeparateXYZ'); comb=n.new('ShaderNodeCombineXYZ'); l.new(geo.outputs['Position'],sep.inputs[0])
  # Project world XY around the street corner into a common metric brick plane.
  add=n.new('ShaderNodeMath'); add.operation='ADD'; l.new(sep.outputs['X'],add.inputs[0]); l.new(sep.outputs['Y'],add.inputs[1]); l.new(add.outputs[0],comb.inputs['X']); l.new(sep.outputs['Z'],comb.inputs['Y'])
  tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=5.0; tex.inputs['Detail'].default_value=5; tex.inputs['Roughness'].default_value=.75; l.new(geo.outputs['Position'],tex.inputs['Vector'])
  ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.18; ramp.color_ramp.elements[0].color=(*(v*.53 for v in color),1); ramp.color_ramp.elements[1].position=.82; ramp.color_ramp.elements[1].color=(*(min(v*1.35,1) for v in color),1)
  l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
  fine=n.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value=95; fine.inputs['Detail'].default_value=3; l.new(geo.outputs['Position'],fine.inputs[0])
  bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.28; bump.inputs['Distance'].default_value=.018; l.new(fine.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs['Normal'],p.inputs['Normal'])
  if brick:
   b=n.new('ShaderNodeTexBrick'); b.inputs['Scale'].default_value=1; b.inputs['Brick Width'].default_value=.24; b.inputs['Row Height'].default_value=.065; b.inputs['Mortar Size'].default_value=.0025; b.inputs['Mortar Smooth'].default_value=.002; b.inputs['Color1'].default_value=(*(v*.84 for v in color),1); b.inputs['Color2'].default_value=(*(v*1.04 for v in color),1); b.inputs['Mortar'].default_value=(.18,.18,.16,1); l.new(comb.outputs[0],b.inputs['Vector'])
   mix=n.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=.22; l.new(b.outputs['Color'],mix.inputs[1]); l.new(ramp.outputs[0],mix.inputs[2]); l.new(mix.outputs[0],p.inputs['Base Color'])
   joint=n.new('ShaderNodeBump'); joint.inputs['Strength'].default_value=.5; joint.inputs['Distance'].default_value=.012; joint.invert=True; l.new(b.outputs['Fac'],joint.inputs['Height']); l.new(bump.outputs['Normal'],joint.inputs['Normal']); l.new(joint.outputs['Normal'],p.inputs['Normal'])
 return m

M={
 'grey':mat('Weathered grey brick • 240 × 65 mm',(.28,.285,.265),brick=True),
 'redbrick':mat('Later red brick infill',(.32,.145,.082),brick=True),
 'plaster':mat('Old chalk lime render',(.57,.53,.43),noise=1),
 'cement':mat('Unpainted patched cement',(.32,.33,.31),noise=1),
 'road':mat('Worn concrete lane',(.27,.26,.235),noise=1),
 'repair':mat('Newer concrete repair',(.38,.365,.33),noise=1),
 'asphalt':mat('Dark utility trench repair',(.21,.21,.19),noise=1),
 'damp':mat('Drying damp marks',(.15,.165,.147),.52,noise=1),
 'tile':mat('Old charcoal clay tiles',(.095,.11,.115),noise=1),
 'tilepatch':mat('Replacement tiles',(.16,.18,.185),noise=1),
 'red':mat('Faded red wood',(.25,.049,.032),noise=1),
 'green':mat('Faded green steel',(.105,.18,.14),.59,.15,noise=1),
 'steel':mat('Dull stainless steel',(.34,.37,.36),.48,.65),
 'black':mat('Black rubber / wire',(.021,.025,.024),.75),
 'sheet':mat('Aged grey blue corrugated sheet',(.16,.23,.25),.7,.3,noise=1),
 'white':mat('Yellowed PVC / air conditioner',(.64,.625,.53),.7,noise=1),
 'glass':mat('Dusty window glass',(.095,.135,.145),.26,.15),
 'paper':mat('Faded cream paper',(.73,.66,.49),.94,noise=1),
 'blue':mat('Enamel address plate',(.035,.10,.18),.55),
 'stool':mat('Faded plastic stool',(.24,.32,.27),.75),
 'soil':mat('Dry dirt at wall foot',(.135,.12,.089),noise=1),
 'wood':mat('Unpainted timber',(.27,.18,.09),noise=1),
 'leaves':mat('Dusty green foliage',(.16,.24,.075),.9),
 'reference':mat('Scale reference saffron',(.8,.32,.035),.7),
}

def cube(name,loc,dims,material,group='01 Architecture',bevel=.012,collide=False):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.name=name; o.scale=dims; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); move(o,group); o['provenance']='Blender authored metric geometry'
 if bevel:
  b=o.modifiers.new('Worn edge','BEVEL'); b.width=bevel; b.segments=2
 if collide: blockers.append({'name':name,'min':[loc[i]-dims[i]/2 for i in range(3)],'max':[loc[i]+dims[i]/2 for i in range(3)]})
 return o

def mesh(name,verts,faces,material,group):
 data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update(); o=bpy.data.objects.new(name,data); collections[group].objects.link(o); data.materials.append(material); o['provenance']='Blender authored metric geometry'; return o

def line(name,points,r,material,group='04 Utilities'):
 c=bpy.data.curves.new(name,'CURVE'); c.dimensions='3D'; c.resolution_u=2; c.bevel_depth=r; c.bevel_resolution=2
 sp=c.splines.new('POLY'); sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points): p.co=(*v,1)
 o=bpy.data.objects.new(name,c); collections[group].objects.link(o); c.materials.append(material); return o

def cyl(name,loc,r,depth,material,group='04 Utilities',rot=None):
 bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=depth,location=loc); o=bpy.context.object; o.name=name; o.data.materials.append(material); move(o,group)
 if rot:o.rotation_euler=rot
 b=o.modifiers.new('Rounded edge','BEVEL'); b.width=.006; b.segments=2
 return o

FONT=bpy.data.fonts.load('/System/Library/Fonts/Supplemental/Arial Unicode.ttf')
def label(name,text,pos,size,material,yaw=0,group='07 Signs'):
 c=bpy.data.curves.new(name,'FONT'); c.body=text; c.font=FONT; c.size=size; c.extrude=.0003; c.align_x='CENTER'; c.align_y='CENTER'; c.space_character=1.1
 o=bpy.data.objects.new(name,c); collections[group].objects.link(o); o.location=pos; o.rotation_euler=(pi/2,0,yaw); c.materials.append(material); return o

# Local facade coordinates: u runs along the street, v goes into the property.
def point(base,u,v,z,yaw):return (base[0]+cos(yaw)*u-sin(yaw)*v,base[1]+sin(yaw)*u+cos(yaw)*v,z)
def box_local(name,base,u,v,z,dims,m,yaw,group='01 Architecture',bevel=.01,collide=False):
 o=cube(name,point(base,u,v,z,yaw),dims,m,group,bevel); o.rotation_euler.z=yaw
 if collide:
  bpy.context.view_layer.update(); pts=[o.matrix_world@Vector(v) for v in o.bound_box]; blockers.append({'name':name,'min':[min(v[i] for v in pts) for i in range(3)],'max':[max(v[i] for v in pts) for i in range(3)]})
 return o

def wall(name,base,length,height,m,yaw=0,opening=None):
 # Front face at v=0; all solid thickness is behind this line.
 if opening:
  u,w,h=opening; intervals=[(-length/2,u-w/2),(u+w/2,length/2)]
  for i,(a,b) in enumerate(intervals):
   if b>a:box_local(name+f' pier {i}',base,(a+b)/2,.15,height/2,(b-a,.30,height),m,yaw,collide=True)
  box_local(name+' lintel',base,u,.15,(h+height)/2,(w,.30,height-h),m,yaw,collide=True)
 else:box_local(name,base,0,.15,height/2,(length,.30,height),m,yaw,collide=True)
 box_local(name+' flat coping',base,0,.16,height+.035,(length+.05,.36,.07),M['cement'],yaw)
 # Non-uniform damp lower course, not a decorative red dado.
 for j in range(int(length/.8)):
  box_local(name+' damp foot',base,-length/2+.4+j*.8,-.006,.08+random.random()*.025,(.76,.006,.15+random.random()*.06),M['damp'],yaw,bevel=0)

def roof(name,base,length,depth,eave,yaw,state):
 roof_states.append({'name':name,'state':state,'eave_m':eave,'ridge_m':eave+.52 if state in ['old_tile','patched_tile'] else eave+.12})
 if state in ['old_tile','patched_tile']:
  # Modest 0.52m rise over a 3.2m deep roof: no giant temple gables.
  verts=[point(base,-length/2,-.13,eave,yaw),point(base,length/2,-.13,eave,yaw),point(base,length/2,depth/2,eave+.52,yaw),point(base,-length/2,depth/2,eave+.52,yaw),point(base,-length/2,depth+.13,eave,yaw),point(base,length/2,depth+.13,eave,yaw)]
  mesh(name+' roof backing',verts,[(0,1,2,3),(3,2,5,4)],M['tile'],'02 Roofs')
  verts=[];faces=[]; rows=8;cols=max(1,int(length/.18)); step=length/cols
  # Half-round clay tiles, 180mm gauge, assembled in long quiet roof planes.
  for side in (0,1):
   for row in range(rows):
    for col in range(cols):
     u=-length/2+col*step; v0=-.16+row*(depth/2+.16)/rows; v1=v0+(depth/2+.16)/rows+.028
     for v in (v0,v1):
      for k in range(5):
       t=k/4; vv=v if side==0 else depth-v; zz=eave+.52*max(0,v)/(depth/2)+.03*sin(pi*t)
       verts.append(point(base,u+t*step,vv,zz,yaw))
     idx=len(verts)-10
     for k in range(4):faces.append((idx+k,idx+k+1,idx+6+k,idx+5+k))
  o=mesh(name+' clay tiles 180mm',verts,faces,M['tile'],'02 Roofs'); o.data.materials.append(M['tilepatch'])
  if state=='patched_tile':
   for f in o.data.polygons:
    if random.random()<.16:f.material_index=1
  a=point(base,-length/2,depth/2,eave+.56,yaw);b=point(base,length/2,depth/2,eave+.56,yaw);line(name+' ridge', [a,b],.065,M['tile'],'02 Roofs')
 else:
  box_local(name+' flat slab',base,0,depth/2,eave,(length+.15,depth+.22,.13),M['cement'],yaw,'02 Roofs')
  if state=='corrugated':
   for i in range(int(length/.12)):
    box_local(name+' sheet rib',base,-length/2+.06+i*.12,depth/2,eave+.092,(.042,depth+.3,.055),M['sheet'],yaw,'02 Roofs',.012)
  elif state=='service_flat':
   box_local(name+' roof parapet',base,0,.10,eave+.24,(length,.16,.4),M['plaster'],yaw,'02 Roofs')

def house(name,base,length,height,material,yaw,state,door=None):
 wall(name+' street face',base,length,height,material,yaw,door)
 depth=3.25
 for u in (-length/2,length/2): box_local(name+' return',base,u,depth/2,height/2,(.22,depth,height),material,yaw,collide=True)
 box_local(name+' back',base,0,depth,height/2,(length,.22,height),material,yaw)
 roof(name,base,length,depth,height+.05,yaw,state)
 if state in ['old_tile','patched_tile']:
  for u in (-length/2,length/2):
   mesh(name+' closed brick gable',[point(base,u,0,height,yaw),point(base,u,depth,height,yaw),point(base,u,depth/2,height+.57,yaw)],[(0,1,2)],material,'01 Architecture')

def entrance(name,base,u,yaw,color='red',number='17',couplet=False):
 width=1.02;height=2.1
 for j in range(8):box_local(name+' timber board',base,u-width/2+(j+.5)*width/8,.06,height/2,(width/8-.009,.055,height),M[color],yaw)
 for uu in (u-width/2-.045,u+width/2+.045):box_local(name+' jamb',base,uu,.065,1.09,(.075,.11,2.18),M['cement'],yaw)
 box_local(name+' small lintel',base,u,.06,2.20,(1.16,.18,.13),M['cement'],yaw)
 box_local(name+' step',base,u,-.19,.055,(1.19,.38,.11),M['cement'],yaw,'05 Doorstep life',.02)
 for uu in (u-.075,u+.075):
  p=point(base,uu,-.015,1.02,yaw);cyl(name+' pull',p,.03,.08,M['steel'],rot=(pi/2,0,yaw))
 box_local(name+' number plate',base,u+.71,-.025,1.92,(.28,.027,.16),M['blue'],yaw,'07 Signs')
 label(name+' number',number,point(base,u+.71,-.043,1.92,yaw),.086,M['white'],yaw)
 box_local(name+' doorbell',base,u+.61,-.025,1.39,(.058,.036,.09),M['white'],yaw,'04 Utilities')
 if couplet:
  for du,txt in [(-.60,'岁岁平安'),(.60,'出入顺心')]:
   box_local(name+' faded paper',base,u+du,-.03,1.32,(.095,.006,.72),M['red'],yaw,'07 Signs',0)
   label(name+' couplet', '\n'.join(txt), point(base,u+du,-.035,1.35,yaw),.065,M['paper'],yaw)
 return {'name':name,'height_m':height,'width_m':width,'position':list(point(base,u,0,0,yaw))}

def window(name,base,u,yaw,z=1.62,style='pvc'):
 box_local(name+' glazing',base,u,-.02,z,(1.08,.038,.86),M['glass'],yaw)
 material=M['white'] if style=='pvc' else M['wood']
 for du in (-.55,0,.55):box_local(name+' mullion',base,u+du,-.05,z,(.052,.09,.94),material,yaw)
 for dz in (-.46,0,.46):box_local(name+' frame',base,u,-.05,z+dz,(1.15,.09,.05),material,yaw)
 box_local(name+' sill',base,u,-.08,z-.5,(1.25,.22,.07),M['cement'],yaw)
 if style=='pvc':
  for du in (-.48,-.24,0,.24,.48):
   line(name+' security bar',[point(base,u+du,-.18,z-.48,yaw),point(base,u+du,-.18,z+.48,yaw)],.013,M['steel'])
  for dz in (-.4,.4):line(name+' horizontal bar',[point(base,u-.6,-.18,z+dz,yaw),point(base,u+.6,-.18,z+dz,yaw)],.013,M['steel'])

def aircon(name,base,u,yaw,z=2.17):
 box_local(name+' unit',base,u,-.25,z,(.76,.35,.50),M['white'],yaw,'04 Utilities',.027)
 # Grille rather than invented brand labels.
 for i in range(17):box_local(name+' grille',base,u-.31+i*.036,-.435,z,(.012,.012,.34),M['steel'],yaw,'04 Utilities',.003)
 for du in (-.25,.25):box_local(name+' bracket',base,u+du,-.23,z-.31,(.055,.5,.055),M['steel'],yaw,'04 Utilities')
 line(name+' refrigerant conduit',[point(base,u+.39,-.2,z,yaw),point(base,u+.52,-.16,z-.12,yaw),point(base,u+.52,-.08,.09,yaw)],.028,M['white'])

def meter(name,base,u,yaw):
 box_local(name+' box',base,u,-.055,1.72,(.28,.13,.37),M['cement'],yaw,'04 Utilities')
 box_local(name+' glass',base,u,-.125,1.78,(.16,.015,.13),M['glass'],yaw,'04 Utilities')
 line(name+' conduit',[point(base,u,-.1,1.53,yaw),point(base,u,-.1,2.65,yaw),point(base,u+.9,-.1,2.65,yaw)],.016,M['black'])

def awning(name,base,u,yaw,width=1.5):
 box_local(name,base,u,-.38,2.27,(width,.90,.045),M['sheet'],yaw,'02 Roofs')
 for du in (-width/2+.1,width/2-.1):line(name+' brace',[point(base,u+du,0,1.94,yaw),point(base,u+du,-.75,2.25,yaw)],.017,M['steel'])

# Lane alignment is deliberately authored as connected property boundaries.
# Each segment ends on the next, so there are no floating facades or black gaps.
left=[(-4,3,-1.95),(3,8,-1.65),(8,13,.3),(13,17,-2.0),(17,20,-1.15),(20,22.7,-1.7)]
right=[(-4,4,1.8),(4,6.5,1.0),(6.5,12,2.95),(12,16.5,2.05),(16.5,19.5,1.45)]
doors=[]
for side,segs in [('L',left),('R',right)]:
 yaw=pi/2 if side=='L' else -pi/2
 for i,(a,b,x) in enumerate(segs):
  base=(x,(a+b)/2);length=b-a
  state=(['old_tile','patched_tile','patched_tile','corrugated','old_tile','flat'] if side=='L' else ['patched_tile','corrugated','old_tile','old_tile','service_flat'])[i]
  h=([2.90,2.45,2.91,2.65,2.87,2.50] if side=='L' else [2.92,2.58,2.43,2.95,2.67])[i]
  material=([M['grey'],M['grey'],M['plaster'],M['redbrick'],M['grey'],M['cement']] if side=='L' else [M['grey'],M['redbrick'],M['cement'],M['plaster'],M['grey']])[i]
  du=(-.65 if side=='L' else .7) if length>3.5 else 0
  op=(du,1.12,2.24) if i in [0,1,3,5] else None
  house(f'{side}{i} resident unit',base,length,h,material,yaw,state,op)
  if op:doors.append(entrance(f'{side}{i} resident door',base,du,yaw,'red' if (side=='L' and i==1) else 'green',str(13+i*2+(0 if side=='L' else 1)),side=='L' and i==1))
  if length>3.8:window(f'{side}{i} replacement window',base,1.15 if du<0 else -1.2,yaw,style='wood' if i==0 else 'pvc')
  if i in (0,2,3):aircon(f'{side}{i} air conditioner',base,-1.3 if i!=2 else .7,yaw)
  if op:meter(f'{side}{i} meter',base,du+.9,yaw)
  if i in (1,3):awning(f'{side}{i} later sheet awning',base,du,yaw)
  # Property step connectors close every offset, while preserving the variation.
  if i:
   prev=segs[i-1][2];delta=abs(prev-x)
   if delta>.001:cube(f'{side}{i} boundary return',((prev+x)/2,a,h/2),(delta+.03,.3,h),material,collide=True)

# Block the distant vista with a continuous property, route turns east at y=21.
house('N end property', (5.5,22.7),14.4,2.68,M['grey'],0,'patched_tile',(-3.3,1.12,2.24))
doors.append(entrance('Quiet north residence',(5.8,22.7),-3.6,0,'green','29'))
window('North replacement window',(5.8,22.7),-.9,0)
house('S workshop addition',(7.6,19.5),10.6,2.48,M['redbrick'],pi,'corrugated',(2.1,1.12,2.24))
doors.append(entrance('Existing livehouse threshold',(7.6,19.5),2.1,pi,'red','D-22'))
meter('Venue meter',(7.6,19.5),3.0,pi); awning('Venue practical shelter',(7.6,19.5),2.1,pi,1.85)
label('Livehouse small door notice','D-22',point((7.6,19.5),2.1,-.05,2.27,pi),.135,M['paper'],pi)
# Final turn into a return alley, with actual mass behind the skyline.
house('East final wall',(14.6,22.0),6.5,2.50,M['cement'],-pi/2,'flat')
house('North beyond return',(12.8,26.0),3.6,2.86,M['plaster'],0,'old_tile')
# One restrained later upper room, set back from the low eave.
cube('Set back later upper room',(-4.0,10.4,3.51),(2.4,3.0,1.35),M['cement'])
cube('Upper room sheet cap',(-4.0,10.4,4.22),(2.7,3.25,.12),M['sheet'],'02 Roofs')

# Continuous road first. Repairs are surface patches, never randomly lifted tiles.
cube('Continuous ground substrate',(2.5,9.0,-.17),(42,55,.32),M['road'],'03 Ground repairs',0)
def patch(name,points,m,z=.001):return mesh(name,[(x,y,z) for x,y in points],[tuple(range(len(points)))],m,'03 Ground repairs')
patch('1990s utility trench repair',[(-.9,-4),(-.38,-4),(-.18,4),(.17,7.8),(.42,10.7),(.34,16),(.12,20),(-.43,20),(-.24,10.4),(-.51,7.8),(-.7,4)],M['asphalt'])
patch('Concrete replacement at courtyard',[(-1.86,13.3),(.2,13.5),(.5,15.4),(-1.7,15.8)],M['repair'],.004)
patch('Irregular resident repair',[(.6,1.2),(1.79,1.0),(1.79,3.5),(.4,3.3)],M['repair'],.004)
patch('Drain moisture strip',[(.84,4.2),(.99,4.2),(.99,7.8),(.75,7.55)],M['damp'],.007)
patch('Later repair at turn',[(.1,20.0),(5.8,20.15),(5.8,20.6),(.1,20.5)],M['asphalt'],.004)
for i in range(26):
 y=random.uniform(-2,21);x=random.uniform(-.8,.8);pts=[(x,y,.012)]
 for j in range(random.randint(3,7)):x+=random.uniform(-.08,.15);y+=random.uniform(.07,.16);pts.append((x,y,.012))
 line('Fine concrete crack',pts,.0018,M['black'],'03 Ground repairs')
for iy in range(9):
 for ix in range(4):cube('240mm salvaged brick threshold',(-1.4+ix*.12,4.1+iy*.25,.025),(.112,.24,.045),M['redbrick'],'03 Ground repairs',.005)
for y in [4.5,9.2,17.2]:
 x=.7 if y<8 else .8
 cube('Drain recessed slot',(x,y,-.009),(.19,.60,.022),M['black'],'03 Ground repairs',.005)
 for j in range(9):cube('Drain iron bar',(x,y-.26+j*.064,.005),(.19,.017,.012),M['steel'],'03 Ground repairs',.003)
cyl('Cast iron manhole',(.1,12.1,.014),.32,.026,M['steel'],'03 Ground repairs')
for j in range(6):line('Manhole grip',[(-.14,11.93+j*.06,.029),(.33,11.93+j*.06,.029)],.005,M['black'],'03 Ground repairs')

# Water downpipes and loose electrical spans create a inhabited roofline.
for x,y in [(-1.59,7.3),(.94,7.7),(1.98,15.7),(.36,12.5)]:
 line('Resident rain pipe',[(x,y,2.55),(x,y,.18),(x+(0.14 if x<0 else -.14),y,.08)],.037,M['cement'])
 for z in (.5,1.6,2.35):cyl('Pipe clamp',(x,y,z),.044,.025,M['steel'])
cyl('Concrete electricity pole',(1.76,13.6,2.9),.075,5.8,M['cement'])
for z in (4.5,5.0):cube('Pole cross arm',(1.76,13.6,z),(.68,.06,.06),M['steel'],'04 Utilities')
for j in range(4):
 pts=[]
 for i in range(25):
  t=i/24;pts.append((-1.8+3.6*t,1.8+21*t,4.3+j*.10-.78*sin(pi*t)))
 line('Sagging overhead utility wire',pts,.008,M['black'])
for j in range(3):line('Service cable to resident',[(1.76,13.6,4.5+j*.05),(-.2,13.2,3.3+j*.05),(-1.95,13.6,2.6+j*.05)],.006,M['black'])

# Shallow residential clutter follows doors and wall edges, not random scatter.
def stool(name,x,y,h=.43):
 cube(name+' seat',(x,y,h),(.3,.28,.05),M['stool'],'05 Doorstep life',.025)
 for dx in (-.105,.105):
  for dy in (-.09,.09):cube(name+' leg',(x+dx,y+dy,h/2),(.035,.035,h),M['stool'],'05 Doorstep life')
stool('Low resident stool',-1.30,6.10);stool('Second courtyard stool',1.7,14.9)
cyl('Plastic wash bucket',(-1.3,5.64,.19),.15,.35,M['stool'],'05 Doorstep life')
cyl('Bucket rim',(-1.3,5.64,.372),.16,.027,M['white'],'05 Doorstep life')
line('Broom handle',[(-1.42,6.5,.08),(-1.57,6.46,1.24)],.013,M['wood'],'05 Doorstep life')
for j in range(18):line('Broom bristle',[(-1.50+j*.008,6.5,.04),(-1.46+j*.004,6.48,.30)],.003,M['wood'],'05 Doorstep life')
for x in (-1.33,-1.16):cube('Resident shoe',(x,5.98,.065),(.11,.255,.12),M['black'],'05 Doorstep life',.045)
cube('Worn small table',(1.63,15.8,.71),(.59,.66,.055),M['wood'],'05 Doorstep life')
for dx in (-.23,.23):
 for dy in (-.26,.26):cube('Table leg',(1.63+dx,15.8+dy,.35),(.035,.035,.7),M['steel'],'05 Doorstep life')

# Meshy objects retain uniform scale, unlike the stretched architectural v2 kit.
assetcache={}
def asset(rel,name,pos,width=None,height=None,yaw=0):
 if rel not in assetcache:
  old=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(ROOT/rel));bpy.context.view_layer.update();added=set(bpy.data.objects)-old
  mm=[o for o in added if o.type=='MESH'];pts=[o.matrix_world@Vector(v) for o in mm for v in o.bound_box];lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)])
  assetcache[rel]=([(o.data,o.matrix_world.copy()) for o in mm],lo,hi)
  for o in added:bpy.data.objects.remove(o,do_unlink=True)
 tmp,lo,hi=assetcache[rel];ext=hi-lo;fac=width/ext.x if width else height/ext.z
 center=Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z));transform=Matrix.Translation(Vector(pos))@Matrix.Rotation(yaw,4,'Z')@Matrix.Diagonal((fac,fac,fac,1))@Matrix.Translation(-center)
 root=bpy.data.objects.new(name,None);collections['06 Meshy props'].objects.link(root);root['source_asset']=rel
 for data,t in tmp:
  o=bpy.data.objects.new(name+' mesh',data);collections['06 Meshy props'].objects.link(o);o.parent=root;o.matrix_world=transform@t;o['provenance']='Meshy';o['source_asset']=rel
 source_records.append({'name':name,'source':rel,'position':pos,'scale':fac,'dimensions_local_m':list(ext*fac),'yaw':yaw})
 return root

bike='assets/hutong/meshy/HT003-OldBicycle.glb'
asset(bike,'Full size roadster bicycle',(-1.61,1.3,.03),width=1.82,yaw=pi/2+.055)
asset(bike,'Second roadster reversed',(1.67,14.65,.03),width=1.82,yaw=-pi/2-.08)
asset(bike,'Third parked bicycle',(7.9,22.39,.03),width=1.78,yaw=pi+.07)
plant='assets/recordshop/v2/meshy/RS208-Pothos.glb'
for i,(x,y,h) in enumerate([(-1.3,5.4,.52),(1.8,14.1,.68),(1.7,16.1,.42),(-1.3,21.6,.65)]):asset(plant,'Resident pot '+str(i),(x,y,.02),height=h,yaw=i*1.4)
asset('assets/recordshop/v4/meshy/RS406-UsedLPCarton.glb','Record shop delivered carton',(-1.51,-.2,.03),width=.38,yaw=.18)
asset('assets/recordshop/v2/meshy/RS207-Thermos.glb','Tea flask on table',(1.65,15.8,.74),height=.32)
# The street facade references the existing shop only. No interior is reconstructed.
label('Record shop exterior sign','唱 片',point((-1.95,-.5),-.65,-.04,2.49,pi/2),.2,M['paper'],pi/2)

# Small street tree, rooted in the pocket; a loose canopy frames open sky.
line('Pocket tree trunk',[(1.76,12.8,0),(1.8,12.82,1.7),(1.55,12.8,3.6)],.09,M['wood'],'05 Doorstep life')
for j in range(9):
 ang=j*2.39;tip=(1.55+cos(ang)*1.1,12.8+sin(ang)*1.0,3.7+random.random()*.6)
 line('Tree branch',[(1.65,12.8,2.5),tip],.025,M['wood'],'05 Doorstep life')
 for k in range(140):
  p=Vector((tip[0]+random.uniform(-.65,.65),tip[1]+random.uniform(-.65,.65),tip[2]+random.uniform(-.2,.4))); ang=random.random()*pi*2; r=random.uniform(.035,.065); tangent=Vector((cos(ang)*r,sin(ang)*r,random.uniform(-.02,.02))); side=Vector((-sin(ang)*r*.42,cos(ang)*r*.42,.006));
  mesh('Small elm leaf',[p-tangent,p+side,p+tangent,p-side],[(0,1,2,3)],M['leaves'],'05 Doorstep life')

# 1.75 m anthropometric mannequin is a separate switchable QA collection.
ref=collections['10 Scale reference 1.75m']
def sphere(name,p,r,scale):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=r,location=p);o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(M['reference']);move(o,ref.name)
x,y=-.48,5.30
sphere('Head • top exactly 1.75m',(x,y,1.635),.115,(.82,1,1))
sphere('Torso',(x,y,1.23),.23,(.85,.52,1.30))
for dx in (-.10,.10):
 line('Reference leg',[(x+dx,y,.10),(x+dx,y,1.02)],.07,M['reference'],ref.name)
 cube('Reference foot',(x+dx,y-.045,.045),(.13,.255,.09),M['reference'],ref.name,.03)
for side in (-1,1):line('Reference arm',[(x+side*.19,y,1.41),(x+side*.25,y,1.1),(x+side*.26,y,.91)],.052,M['reference'],ref.name)
label('Human reference label','1.75 m',(x,y-.17,1.93),.12,M['reference'],0,ref.name)
ref.hide_render=True

def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,p,target,power,color,size=1,kind='AREA'):
 d=bpy.data.lights.new(name,kind);d.energy=power;d.color=color
 if kind=='AREA':d.shape='DISK';d.size=size
 if kind=='SUN':d.angle=.16
 o=bpy.data.objects.new(name,d);collections['08 Lights'].objects.link(o);o.location=p;aim(o,target);return o
world=bpy.data.worlds.new('Open overcast Beijing sky');world.use_nodes=True;s.world=world
bg=world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.45,.59,.74,1);bg.inputs[1].default_value=.5
sun=light('Late afternoon sun',(-14,3,18),(1,10,0),2.1,(1,.91,.77),kind='SUN')
sky=light('Broad sky opening',(0,10,15),(0,10,0),2300,(.67,.80,1),14)
bounce=light('Diffuse sky front',(0,-5,6),(0,9,1.4),700,(.79,.85,1),7)
practicals=[]
for name,p,t,e,col in [('Shop warm threshold',(-1.6,-1.1,2.4),(-.5,.5,1),38,(1,.59,.28)),('Resident door bulb',(-1.35,4.4,2.2),(-.4,4.4,1),22,(1,.63,.34)),('Venue doorway warm',(5.5,19.8,2.2),(5.5,21,1),32,(1,.39,.19))]:
 practicals.append(light(name,p,t,e,col,.35))
 cyl(name+' shade',p,.09,.05,M['white'],'04 Utilities')

def cam(name,p,target,lens=32,ortho=None):
 d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);collections['09 Cameras'].objects.link(o);o.location=p;aim(o,target);d.lens=lens;d.clip_start=.05;d.clip_end=200
 if ortho:d.type='ORTHO';d.ortho_scale=ortho
 return o
cams=[cam('01 Entry • 1.65m',(0,-2.7,1.65),(.37,7.7,1.67),32),cam('02 Resident threshold • 1.65m',(.27,3.65,1.65),(-1.62,5.8,1.35),32),cam('03 Pocket • 1.65m',(1.62,11.65,1.65),(.0,17.3,1.65),30),cam('04 Turn reveal • 1.65m',(-.25,20.7,1.65),(6.2,21,1.65),32),cam('05 Roof states overview',(20,-12,27),(2.3,10,0),40,36)]
s.render.engine='CYCLES';s.cycles.samples=40;s.cycles.use_denoising=True;s.render.resolution_x=1600;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=0
s.render.film_transparent=False;s.camera=cams[0];s['authoring_rule']='User authorized Blender buildings/road/utilities and Meshy detail props on 2026-10-04. Existing interiors locked.'
s['era']='2003–2006 fictional inhabited Beijing residential hutong, not a historical venue address reconstruction.'

# Geometric QA: actual measured doors, explicit lane sections, walk samples.
route=[(0,-3),(-.1,2),(-.32,6),(.15,6.7),(.70,7.15),(1.45,7.75),(1.60,10),(1.65,12.55),(.30,14),(.1,18),(.1,21),(4,21),(9,21),(13.5,21),(13.5,24.6)]
samples=[];collisions=[]
for a,b in zip(route,route[1:]):
 count=max(1,math.ceil(math.dist(a,b)/.15))
 for i in range(count):
  t=i/count;p=(a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1]));samples.append(p)
  for ob in blockers:
   if ob['min'][2]>.18+1.75:continue
   dx=max(ob['min'][0]-p[0],0,p[0]-ob['max'][0]);dy=max(ob['min'][1]-p[1],0,p[1]-ob['max'][1])
   if math.hypot(dx,dy)<.30:collisions.append({'point':p,'obstacle':ob['name']})
sections=[]
for yv in (0,5,10,14,18):
 l=next(v for a,b,v in left if a<=yv<b);r=next(v for a,b,v in right if a<=yv<b);sections.append({'y_m':yv,'left_x':l,'right_x':r,'wall_clear_width_m':round(r-l,3)})
manifest={'scene':s.name,'era':s['era'],'authoring':s['authoring_rule'],'doors':doors,'lane_sections':sections,'route_centerline':route,'roof_states':roof_states,'Meshy_assets':source_records,'dimensions':{'reference_height':1.75,'camera_eye_height':1.65,'door_height':2.1,'door_width':1.02},'status':'Blender review build, not deployed to Unity'}
(DESIGN/'layout.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
qa={'reference_height_m':1.75,'door_height_m':2.1,'wall_clear_width_min_m':min(d['wall_clear_width_m'] for d in sections),'route_sample_count':len(samples),'player_radius_m':.30,'architecture_collisions':collisions,'roof_count':len(roof_states),'nontraditional_roof_count':sum(r['state'] in ['flat','corrugated','service_flat'] for r in roof_states),'geometry_pass':not collisions,'scope':'Architecture capsule-clearance check; props, Unity movement, audio and triggers are not runtime-tested.'}
(DESIGN/'qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2))
sources=sorted({r['source'] for r in source_records});(DESIGN/'mesh-provenance.json').write_text(json.dumps([{'path':p,'sha256':hashlib.sha256((ROOT/p).read_bytes()).hexdigest()} for p in sources],indent=2))

blend=ROOT/'blender/source/D22_HutongOutdoor_v3_Residential.blend'
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(blend))
if os.environ.get('HUTONG_BUILD_ONLY'):
 print(json.dumps(qa));raise SystemExit(0)
render_names=['entry-day','resident-door','pocket-day','turn-day','overview']
for c,n in zip(cams,render_names):
 if os.environ.get('HUTONG_HERO_ONLY') and n!='entry-day':continue
 s.camera=c;s.render.filepath=str(OUT/(n+'.png'));bpy.ops.render.render(write_still=True)
if os.environ.get('HUTONG_HERO_ONLY'):
 print(json.dumps(qa));raise SystemExit(0)
ref.hide_render=False;s.camera=cams[0];s.render.filepath=str(OUT/'scale-1p75m.png');bpy.ops.render.render(write_still=True);ref.hide_render=True
# Twilight is a separate lighting state; saved file opens in readable daylight.
sun.data.energy=.12;sky.data.energy=580;bounce.data.energy=80;bg.inputs[0].default_value=(.20,.32,.52,1);bg.inputs[1].default_value=.30
for l in practicals:l.data.energy*=3
s.camera=cams[0];s.render.filepath=str(OUT/'entry-dusk.png');bpy.ops.render.render(write_still=True)
sun.data.energy=2.1;sky.data.energy=2300;bounce.data.energy=700;bg.inputs[0].default_value=(.45,.59,.74,1);bg.inputs[1].default_value=.5
for l in practicals:l.data.energy/=3
s.camera=cams[0]
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(blend));print(json.dumps(qa))
