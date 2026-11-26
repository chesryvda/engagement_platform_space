import bpy, numpy as np, math
P="/Users/lasur/Documents/GitHub/engagement_platform_space/Engagement Platform"
OUT=P+"/Assets/Textures/Technopolis/"
FONT="/System/Library/Fonts/Supplemental/Arial Black.ttf"
def lin(h):
    c=[int(h[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(x/12.92 if x<=0.04045 else ((x+0.055)/1.055)**2.4 for x in c)+(1,)
WHITE=(1,1,1,1); MAGENTA=lin("#E600FF"); BLUE=lin("#007DFF"); BLUE_LIGHT=lin("#3AA6FF")
# ---------- trace logo (same as badge) ----------
img=bpy.data.images.load(P+"/Blender/Technopolis_logo.png"); w,h=img.size
a=np.array(img.pixels[:],dtype=np.float32).reshape(h,w,4); ink=a[...,3]*(1-a[...,:3].mean(-1))
def blur(m):
    p=np.pad(m,1,mode='edge'); return (p[:-2,:-2]+2*p[:-2,1:-1]+p[:-2,2:]+2*p[1:-1,:-2]+4*p[1:-1,1:-1]+2*p[1:-1,2:]+p[2:,:-2]+2*p[2:,1:-1]+p[2:,2:])/16
f=np.pad(blur(blur(ink)),2)
def pt(e):
    r,c,d=e
    if d==0: v0,v1=f[r,c],f[r,c+1]; return (c+(0.5-v0)/(v1-v0), r)
    v0,v1=f[r,c],f[r+1,c]; return (c, r+(0.5-v0)/(v1-v0))
b=(f>0.5); cases=(b[:-1,:-1]*1)|(b[:-1,1:]*2)|(b[1:,1:]*4)|(b[1:,:-1]*8); nxt={}
for r,c in zip(*[x.tolist() for x in np.nonzero((cases>0)&(cases<15))]):
    k=cases[r,c]; B=(r,c,0); R=(r,c+1,1); T=(r+1,c,0); L=(r,c,1)
    tb={1:[(L,B)],2:[(B,R)],3:[(L,R)],4:[(R,T)],5:[(L,T),(R,B)],6:[(B,T)],7:[(L,T)],8:[(T,L)],9:[(T,B)],10:[(B,L),(T,R)],11:[(T,R)],12:[(R,L)],13:[(R,B)],14:[(B,L)]}
    for s,e in tb[k]: nxt[s]=e
loops=[];seen=set()
for s in list(nxt):
    if s in seen: continue
    lp=[];cur=s
    while cur not in seen and cur in nxt: seen.add(cur); lp.append(pt(cur)); cur=nxt[cur]
    if len(lp)>8: loops.append(np.array(lp))
allp=np.vstack(loops); mn=allp.min(0); mx=allp.max(0)
LOGO=[(L-(mn+mx)/2)/(mx[0]-mn[0]) for L in loops]   # width 1, centred
LOGO_ASPECT=(mx[1]-mn[1])/(mx[0]-mn[0])

def reset():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    for c in list(bpy.data.curves): bpy.data.curves.remove(c)
def poly_obj(name,loop_list,color,z):
    cu=bpy.data.curves.new(name,"CURVE"); cu.dimensions='2D'; cu.fill_mode='BOTH'
    for s in loop_list:
        sp=cu.splines.new('POLY'); sp.points.add(len(s)-1)
        for i,(x,y) in enumerate(s): sp.points[i].co=(x,y,0,1)
        sp.use_cyclic_u=True
    ob=bpy.data.objects.new(name,cu); bpy.context.scene.collection.objects.link(ob); ob.location.z=z; ob.color=color; return ob
def logo(width,cx,cy,color,z):
    return poly_obj("logo",[L*width+np.array([cx,cy]) for L in LOGO],color,z)
def rrect(W,H,R,cx=0,cy=0,seg=16):
    pts=[]
    for x0,y0,a0 in [(W/2-R,H/2-R,0),(-W/2+R,H/2-R,90),(-W/2+R,-H/2+R,180),(W/2-R,-H/2+R,270)]:
        for i in range(seg+1):
            t=math.radians(a0+90*i/seg); pts.append((cx+x0+R*math.cos(t),cy+y0+R*math.sin(t)))
    return np.array(pts)
def text(body,size,cx,cy,color,z,spacing=1.0):
    cu=bpy.data.curves.new("txt","FONT"); cu.body=body; cu.font=bpy.data.fonts.load(FONT,check_existing=True); cu.size=size
    cu.align_x='CENTER'; cu.align_y='CENTER'; cu.space_character=spacing
    ob=bpy.data.objects.new("txt",cu); bpy.context.scene.collection.objects.link(ob); ob.location=(cx,cy,z); ob.color=color; return ob
def render(path,W,H,px):
    sc=bpy.context.scene
    cam=bpy.data.cameras.new("cam"); cam.type='ORTHO'; cam.ortho_scale=max(W,H)
    co=bpy.data.objects.new("cam",cam); sc.collection.objects.link(co); co.location=(0,0,5); sc.camera=co
    sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='FLAT'; sc.display.shading.color_type='OBJECT'
    sc.render.film_transparent=True; sc.view_settings.view_transform='Standard'; sc.display.render_aa='32'
    sc.render.resolution_x=px if W>=H else int(px*W/H); sc.render.resolution_y=int(px*H/W) if W>=H else px; sc.render.resolution_percentage=100
    sc.render.image_settings.file_format='PNG'; sc.render.image_settings.color_mode='RGBA'; sc.render.filepath=path
    bpy.ops.render.render(write_still=True)

# ---------- door decal: 1.10 x 0.40 m ----------
reset()
DW,DH=1.10,0.40
# White logo only, exactly in the centre of the decal (the decal itself is centred on the door).
LW=0.66; logo(LW,0,0,WHITE,0.0)
render(OUT+"T_TechnopolisDoorDecal.png",DW,DH,4096)
print("RESULT ok aspect",LOGO_ASPECT,"logo height",LW*LOGO_ASPECT)
