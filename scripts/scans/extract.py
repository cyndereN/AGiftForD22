"""Recover original SOG scans from the archived SuperSplat web exports."""
import base64, hashlib, json, re, zipfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
out = root / 'assets/scans'
out.mkdir(exist_ok=True, parents=True)
records=[]
for src in sorted((root/'legacy/web').glob('scene_*.html')):
    text=src.read_text()
    matches=re.findall(r'contents:\s*fetch\("data:application/octet-stream;base64,([A-Za-z0-9+/=]+)',text)
    if len(matches)!=1: raise ValueError(f'{src.name}: expected one SOG, got {len(matches)}')
    data=base64.b64decode(matches[0], validate=True)
    target=out/(src.stem+'.sog'); target.write_bytes(data)
    with zipfile.ZipFile(target) as z:
        meta=json.loads(z.read('meta.json'))
    records.append({'id':src.stem,'source':src.relative_to(root).as_posix(),'sog':target.relative_to(root).as_posix(),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'count':meta['count']})
(out/'manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(records,indent=2))
