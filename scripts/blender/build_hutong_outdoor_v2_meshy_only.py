"""Outdoor Beijing hutong connector assembled only from existing Meshy GLBs.

This file intentionally does not build the record shop or the live house interiors.
Those are existing scene anchors.  The Blender scene is an exterior lane with open
sky, a 90-degree corner, a small courtyard pocket, and two doorway thresholds.
"""
import bpy, hashlib, json, math
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "blender/verification/hutong-outdoor-v2"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = "D22_HutongOutdoor_v2_MeshyOnly"
scene.unit_settings.system = "METRIC"

COL = {}
for name in ["Architecture", "StreetLife", "RecordShopAnchor", "LiveHouseAnchor", "Paper", "Lights", "Cameras"]:
    col = bpy.data.collections.new("Meshy " + name)
    scene.collection.children.link(col)
    COL[name] = col

cache = {}
records = []
STYLE = {}

def style_material(name, color, roughness=0.78, metallic=0.0):
    if name in STYLE:
        return STYLE[name]
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
    STYLE[name] = mat
    return mat

MAT_GREY = style_material("Hutong grey brick", (0.28, 0.30, 0.31), 0.88)
MAT_MORTAR = style_material("Hutong faded plaster", (0.47, 0.46, 0.44), 0.92)
MAT_TILE = style_material("Hutong charcoal tile", (0.075, 0.085, 0.095), 0.90)
MAT_RED = style_material("Faded vermilion door", (0.42, 0.035, 0.025), 0.68)
MAT_BLACK = style_material("Old bicycle black metal", (0.025, 0.03, 0.035), 0.38, 0.45)
MAT_STEEL = style_material("Utility galvanised steel", (0.24, 0.27, 0.29), 0.55, 0.35)

def template(path):
    path = str(path)
    if path in cache:
        return cache[path]
    old = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    bpy.context.view_layer.update()
    added = set(bpy.data.objects) - old
    meshes = [o for o in added if o.type == "MESH"]
    pts = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    lo = Vector(tuple(min(p[i] for p in pts) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in pts) for i in range(3)))
    tmp = [(o.data, o.matrix_world.copy()) for o in meshes]
    for obj in added:
        bpy.data.objects.remove(obj, do_unlink=True)
    cache[path] = (tmp, lo, hi)
    return cache[path]

def bounds(root):
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(v) for o in root.children for v in o.bound_box]
    return [min(p[i] for p in pts) for i in range(3)], [max(p[i] for p in pts) for i in range(3)]

def place(rel, name, pos, group="Architecture", dims=None, width=None, height=None, yaw=0, rotation=None, collision=False):
    path = ROOT / rel
    tmp, lo, hi = template(path)
    ext = hi - lo
    fac = width / ext.x if width else height / ext.z if height else 1
    scale = tuple(dims[i] / ext[i] for i in range(3)) if dims else (fac, fac, fac)
    rot = Euler(rotation or (0, 0, yaw)).to_matrix().to_4x4()
    mat = rot @ Matrix.Diagonal((*scale, 1)) @ Matrix.Translation(-Vector(((hi.x + lo.x) / 2, (hi.y + lo.y) / 2, lo.z)))
    pts = [mat @ Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
    fix = Vector((pos[0] - (min(v.x for v in pts) + max(v.x for v in pts)) / 2,
                  pos[1] - (min(v.y for v in pts) + max(v.y for v in pts)) / 2,
                  pos[2] - min(v.z for v in pts)))
    root = bpy.data.objects.new(name, None)
    COL[group].objects.link(root)
    root.matrix_world = Matrix.Translation(fix) @ mat
    root["source_asset"] = rel
    root["meshy_asset"] = True
    root["collision"] = collision
    for i, (data, matrix) in enumerate(tmp):
        obj = bpy.data.objects.new(name + "__mesh" + str(i), data)
        COL[group].objects.link(obj)
        obj.parent = root
        obj.matrix_parent_inverse = Matrix.Identity(4)
        obj.matrix_basis = matrix
        obj["source_asset"] = rel
        obj["meshy_asset"] = True
        # The new Meshy exterior props are untextured previews.  Reuse simple
        # materials so the same geometry reads as grey brick, dark tiled eaves,
        # red doors and black bicycle metal in Blender/Unity lighting.
        if rel == WALL and len(obj.data.materials) == 0:
            obj.data.materials.append(MAT_GREY)
            obj.data.materials.append(MAT_TILE)
            zmax = max((v.co.z for v in obj.data.vertices), default=1.0)
            for poly in obj.data.polygons:
                poly.material_index = 1 if poly.center.z > zmax * 0.70 else 0
        elif rel == GATE and len(obj.data.materials) == 0:
            obj.data.materials.append(MAT_RED)
            obj.data.materials.append(MAT_TILE)
            zmax = max((v.co.z for v in obj.data.vertices), default=1.0)
            for poly in obj.data.polygons:
                poly.material_index = 1 if poly.center.z > zmax * 0.78 else 0
        elif rel == BICYCLE and len(obj.data.materials) == 0:
            obj.data.materials.append(MAT_BLACK)
        elif rel == UTILITY and len(obj.data.materials) == 0:
            obj.data.materials.append(MAT_STEEL)
    lo2, hi2 = bounds(root)
    records.append(dict(label=name, source=rel, group=group, position=list(pos), bounds_min=lo2, bounds_max=hi2, collision=collision))
    return root

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()

def light(name, pos, target, energy, color, size, kind="AREA"):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    data.color = color
    data.shadow_soft_size = size
    if kind == "AREA":
        data.shape = "DISK"
        data.size = size
    obj = bpy.data.objects.new(name, data)
    COL["Lights"].objects.link(obj)
    obj.location = pos
    aim(obj, target)
    return obj

def cam(name, pos, target, lens):
    data = bpy.data.cameras.new(name)
    obj = bpy.data.objects.new(name, data)
    COL["Cameras"].objects.link(obj)
    obj.location = pos
    aim(obj, target)
    data.lens = lens
    data.clip_start = 0.03
    return obj

FLOOR = "assets/d22/v9/meshy/B02-TileFloor.glb"
WALL = "assets/hutong/meshy/HT001-GreyBrickEaveWall.glb"
GATE = "assets/hutong/meshy/HT002-RedGateShadowWall.glb"
BICYCLE = "assets/hutong/meshy/HT003-OldBicycle.glb"
UTILITY = "assets/hutong/meshy/HT004-UtilityCorner.glb"
DOOR = "assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb"
WINDOW = "assets/recordshop/v3/retextured/RS302-HutongWindowBay.glb"

def floor_tile(name, x, y, yaw=0.0, sx=1.0, sy=1.0):
    place(FLOOR, name, (x + 0.5, y + 0.5, -0.06), "Architecture", dims=(sx, sy, 0.10), yaw=yaw)

def pave_area(prefix, x0, x1, y0, y1):
    """Pave a lane patch with small offsets and rotations, avoiding a plaza grid."""
    for iy, y in enumerate(range(y0, y1)):
        for ix, x in enumerate(range(x0, x1)):
            jitter = 0.06 * math.sin((ix + 2) * 1.7 + iy)
            yaw = 0.025 * math.sin(ix * 2.1 + iy * 0.7)
            floor_tile(f"{prefix} {ix}_{iy}", x + jitter, y, yaw, 1.03, 1.03)

def wall_piece(prefix, pos, dims, yaw=0, group="Architecture", height=2.75):
    # HT001 already contains the weathered brick face and shallow tiled eave;
    # do not stack the old white/red wall panels on top of it.
    place(WALL, prefix + " grey brick eave", pos, group, dims=(dims[0], dims[1], height), yaw=yaw)

def wall_run(prefix, x0, x1, y, step=1.9, yaw=0):
    n = int((x1 - x0) / step)
    for i in range(n):
        wall_piece(prefix + f" {i}", (x0 + step * i + step / 2, y, 0), (step, 0.22), yaw)

def side_run(prefix, y0, y1, x, step=1.9, yaw=math.pi / 2):
    n = int((y1 - y0) / step)
    for i in range(n):
        wall_piece(prefix + f" {i}", (x, y0 + step * i + step / 2, 0), (step, 0.22), yaw)

# Outdoor plan: width changes from roughly 4.4m at the mouth to 2.5–3m in
# the squeeze, then opens into a small pocket before turning east.  Walls are
# staggered and stop/start like courtyard boundaries, never a parallel shell.
pave_area("Wide hutong mouth", -3, 3, -1, 3)
pave_area("Narrow squeeze", -2, 2, 3, 8)
pave_area("Courtyard pocket", -3, 3, 8, 12)
pave_area("Turn lane", 0, 10, 8, 11)
pave_area("Livehouse approach", 9, 13, 8, 11)

# West edge: the record shop threshold, a recessed wall, then a protruding
# corner.  East edge: low house wall, utility clutter, and a visual blocker.
for i, (y, length, x, h) in enumerate([(0.0, 2.2, -2.25, 2.55), (2.9, 1.8, -1.65, 2.35), (5.2, 2.0, -1.55, 3.05), (8.8, 1.7, -2.30, 2.40), (10.4, 1.8, -2.65, 2.85)]):
    wall_piece(f"West stagger {i}", (x, y, 0), (length, 0.30), math.pi / 2, height=h)
for i, (y, length, x, h) in enumerate([(0.3, 2.0, 2.00, 2.55), (3.0, 1.6, 1.20, 2.25), (4.9, 2.1, 1.05, 2.70), (7.8, 2.0, 2.35, 2.45), (10.4, 1.7, 2.70, 2.75)]):
    wall_piece(f"East stagger {i}", (x, y, 0), (length, 0.30), math.pi / 2, height=h)

# The 90-degree turn is hidden by an offset shadow wall. The player must move
# left or right around it, and the livehouse entrance is only seen later.
place(GATE, "Offset shadow wall at turn", (0.45, 7.65, 0), "Architecture", dims=(1.85, 0.60, 2.65), yaw=0)
for i, (x, length, y, h) in enumerate([(3.1, 2.4, 10.85, 2.55), (5.8, 1.8, 10.65, 2.95), (8.0, 1.6, 10.90, 2.40), (10.2, 1.9, 10.55, 2.80)]):
    wall_piece(f"North turn wall {i}", (x, y, 0), (length, 0.30), height=h)
for i, (x, length, y, h) in enumerate([(3.2, 1.9, 7.45, 2.35), (5.4, 2.1, 7.55, 2.65), (8.0, 1.4, 7.35, 2.25)]):
    wall_piece(f"South turn wall {i}", (x, y, 0), (length, 0.30), height=h)

# Existing scenes are represented only by exterior threshold assets and light
# cues. Their interiors are not duplicated or modified here.
place(DOOR, "Existing RecordShop v4 exterior door", (-1.66, 2.9, 0), "RecordShopAnchor", dims=(1.05, 0.18, 2.15), yaw=math.pi / 2)
place(WINDOW, "Existing RecordShop v4 display window", (-1.57, 4.45, 0.5), "RecordShopAnchor", dims=(1.35, 0.20, 1.50), yaw=math.pi / 2)
place(DOOR, "Existing LiveHouse exterior door", (8.25, 10.55, 0), "LiveHouseAnchor", dims=(1.10, 0.18, 2.20), yaw=0)
place(GATE, "LiveHouse exterior gate shadow wall", (9.65, 10.55, 0), "LiveHouseAnchor", dims=(1.45, 0.55, 2.75), yaw=0)

# Public-life props: bicycles are represented by available Meshy guitar/bike
# language only when present; crates, plants, flyers and flight cases remain
# at thresholds so the lane reads as lived-in rather than a prop showroom.
place("assets/recordshop/v2/meshy/RS213-TapeCrate.glb", "Record shop delivery crate", (-0.85, 3.6, 0), "StreetLife", dims=(0.55, 0.50, 0.55), collision=True)
place("assets/recordshop/v2/meshy/RS208-Pothos.glb", "Doorstep plant", (-1.20, 4.25, 0), "StreetLife", height=0.62)
place("assets/d22/v9/meshy/B11-FlightCase.glb", "Courtyard equipment case", (3.8, 8.35, 0), "StreetLife", width=0.70, collision=True)
place("assets/d22/v9/meshy/B11-FlightCase.glb", "LiveHouse delivery case", (7.6, 9.0, 0), "StreetLife", width=0.62, collision=True)
for i, x in enumerate((4.3, 5.5, 6.7)):
    place("assets/d22/architecture/E09_OriginalFlyer.glb", f"Courtyard flyer {i}", (x, 10.38, 1.0), "Paper", height=0.75, yaw=0)
place("assets/recordshop/v2/meshy/RS205-DeskFan.glb", "Courtyard fan", (5.0, 8.6, 0), "StreetLife", height=0.55)
place("assets/d22/v9/meshy/B06-BarStool.glb", "Plastic stool beside wall", (6.0, 8.0, 0), "StreetLife", height=0.65, collision=True)
place(BICYCLE, "Old black hutong bicycle", (0.95, 4.05, 0), "StreetLife", width=1.25, yaw=math.pi / 2)
place(UTILITY, "Utility pole and wire corner", (1.45, 5.85, 0), "StreetLife", width=1.25, yaw=math.pi / 2)

# Separate light cues tell the player which existing scene sits behind each
# threshold without placing its interior geometry in this exterior file.
light("Open sky cool fill", (0, 3.5, 7.0), (0, 6.0, 0.0), 500, (0.42, 0.62, 1.0), 5.0)
light("Record shop threshold warm", (-1.9, 3.0, 2.2), (-0.6, 3.3, 0.8), 180, (1.0, 0.52, 0.22), 1.1)
light("Courtyard sodium practical", (3.8, 8.8, 3.0), (4.5, 9.0, 0.7), 220, (1.0, 0.42, 0.16), 1.4)
light("Livehouse red doorway", (8.0, 10.45, 2.0), (8.3, 10.2, 0.8), 260, (1.0, 0.10, 0.05), 1.0)
light("East exit blue spill", (11.5, 9.5, 4.0), (10.0, 9.8, 0.7), 150, (0.30, 0.52, 1.0), 2.5)

world = bpy.data.worlds.new("Open Beijing night sky")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.035, 0.075, 0.14, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.22
scene.world = world

cameras = [
    cam("hutong_entry", (0.0, -0.8, 1.60), (0.0, 3.7, 1.30), 30),
    cam("recordshop_threshold", (-1.0, 2.2, 1.55), (0.4, 4.7, 1.15), 32),
    cam("corner_90_degree", (0.15, 6.6, 1.60), (3.2, 8.9, 1.20), 30),
    cam("courtyard_pocket", (3.5, 8.1, 1.65), (7.7, 10.4, 1.15), 30),
    cam("livehouse_threshold", (7.0, 9.15, 1.58), (8.5, 10.5, 1.15), 33),
    cam("open_exit", (11.3, 9.1, 1.65), (9.2, 10.3, 1.9), 32),
]
scene.camera = cameras[0]
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 1440
scene.render.resolution_y = 900
scene.render.resolution_percentage = 80
scene.render.image_settings.file_format = "PNG"
scene.view_settings.look = "AgX - Medium High Contrast"
scene.view_settings.exposure = -0.25
scene["rule"] = "Outdoor hutong connector only. All visible geometry comes from Meshy GLBs; Blender only assembles, scales, rotates, lights and cameras. Existing RecordShop v4 and LiveHouse scenes are preserved. No ceiling."

manifest = {
    "version": 2,
    "scene": scene.name,
    "type": "outdoor_hutong_connector",
    "time_anchor": "2006 Beijing hutong music block",
    "open_sky": True,
    "ceiling": False,
    "dimensions_m": [19, 16, 2.9],
    "anchors": [
        {"id": "recordshop_v4_exit", "scene": "D22_RecordShopV4", "position": [-2.66, 4.7, 0]},
        {"id": "livehouse_existing_exit", "scene": "existing_livehouse_scene", "position": [9.9, 10.0, 0]},
    ],
    "route": ["recordshop_v4_exit", "lane_entry", "corner_90_degree", "courtyard_pocket", "livehouse_existing_exit", "open_exit"],
    "assets": records,
    "sources": sorted({r["source"] for r in records}),
    "cameras": [dict(name=o.name, position=list(o.location), lens=o.data.lens) for o in cameras],
    "rule": scene["rule"],
}
folder = ROOT / "design/hutong-livehouse-v1"
(folder / "outdoor-layout.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2))
(folder / "outdoor-sources.json").write_text(json.dumps([dict(path=p, sha256=hashlib.sha256((ROOT / p).read_bytes()).hexdigest()) for p in sorted({r["source"] for r in records})], ensure_ascii=False, indent=2))
blend_file = ROOT / "blender/source/D22_HutongOutdoor_v2_MeshyOnly.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(blend_file))
for camera, filename in zip(cameras, ["entry", "recordshop-threshold", "corner-90", "courtyard", "livehouse-threshold", "open-exit"]):
    scene.camera = camera
    scene.render.filepath = str(OUT / (filename + ".png"))
    bpy.ops.render.render(write_still=True)
scene.camera = cameras[0]
bpy.ops.wm.save_as_mainfile(filepath=str(blend_file))
print(json.dumps({"scene": scene.name, "mesh_instances": sum(o.type == "MESH" for o in scene.objects), "unique_meshy_assets": len(set(r["source"] for r in records)), "provenance": "PASS", "open_sky": True, "ceiling": False}))
