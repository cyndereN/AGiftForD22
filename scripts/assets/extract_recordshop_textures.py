"""Recover embedded GLB textures for the reused record-shop props."""
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
layout = json.loads((ROOT/'design/recordshop-v1/layout.json').read_text())
records = {}
for asset in layout['reused_meshy']:
    path = ROOT/asset['path']
    if asset['path'] in records:
        continue
    data = path.read_bytes()
    size, kind = struct.unpack_from('<II', data, 12)
    gltf = json.loads(data[20:20+size])
    offset = 20+size
    binary = data[offset+8:]
    dest = ROOT/'assets/recordshop/reused-textures'/path.stem
    dest.mkdir(parents=True, exist_ok=True)
    images=[]
    for i, image in enumerate(gltf.get('images', [])):
        view=gltf['bufferViews'][image['bufferView']]
        start=view.get('byteOffset',0)
        ext='.jpg' if image.get('mimeType')=='image/jpeg' else '.png'
        target=dest/('image_%d'%i+ext)
        target.write_bytes(binary[start:start+view['byteLength']])
        images.append(str(target.relative_to(ROOT)))
    records[asset['path']]=images
(ROOT/'design/recordshop-v1/reused-textures.json').write_text(json.dumps(records,indent=2)+'\n')
print('Extracted textures for',len(records),'existing Meshy assets')
