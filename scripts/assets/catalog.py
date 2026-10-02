"""Validate archived GLBs and generate the portable source-asset catalog."""
import hashlib
import json
import struct
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
LIBRARY = ROOT / "assets/d22"


def records(value):
    if isinstance(value, dict):
        yield value
        for child in value.values():
            yield from records(child)
    elif isinstance(value, list):
        for child in value:
            yield from records(child)


def main():
    provenance = {}
    for manifest in [LIBRARY / "generation-manifest.json", *LIBRARY.glob("v*/manifest.json")]:
        for record in records(json.loads(manifest.read_text())):
            path = record.get("file", record.get("model_file"))
            if isinstance(path, str) and path.endswith(".glb"):
                provenance[path] = {
                    "manifest": str(manifest.relative_to(ROOT)),
                    "task_id": record.get("task_id", record.get("geometry_task_id")),
                    "status": record.get("status"),
                    "quality_status": record.get("quality_status", record.get("asset_quality_status")),
                    "expected_sha256": record.get("sha256"),
                }
    assets = []
    for path in sorted(LIBRARY.rglob("*.glb")):
        data = path.read_bytes()
        magic, version, size = struct.unpack_from("<4sII", data)
        if magic != b"glTF" or version != 2 or size != len(data):
            raise ValueError(f"Invalid GLB header: {path}")
        length, chunk_type = struct.unpack_from("<II", data, 12)
        if chunk_type != 0x4E4F534A:
            raise ValueError(f"Missing GLB JSON chunk: {path}")
        gltf = json.loads(data[20:20 + length])
        for resource in gltf.get("images", []) + gltf.get("buffers", []):
            uri = resource.get("uri", "")
            if uri and not uri.startswith("data:") and not (path.parent / unquote(uri)).is_file():
                raise ValueError(f"Missing external resource {uri} in {path}")
        relative = path.relative_to(ROOT).as_posix()
        digest = hashlib.sha256(data).hexdigest()
        source = provenance.get(relative, {})
        expected = source.pop("expected_sha256", None)
        if expected and digest != expected:
            raise ValueError(f"Source manifest checksum mismatch: {relative}")
        assets.append({
            "path": relative,
            "origin": "modular_architecture" if "architecture" in path.parts else "meshy",
            "bytes": len(data), "sha256": digest,
            "mesh_count": len(gltf.get("meshes", [])),
            "material_count": len(gltf.get("materials", [])),
            "embedded_images": sum("bufferView" in image for image in gltf.get("images", [])),
            "provenance": source or {"status": "See versioned design handoff; task record not recovered"},
        })
    output = {
        "schema_version": 1, "generated_by": "python3 scripts/assets/catalog.py",
        "glb_count": len(assets),
        "meshy_glb_count": sum(a["origin"] == "meshy" for a in assets),
        "note": "Archive includes rejected iterations. Authoritative placement is the v18 Blender source; this catalog does not imply final game-asset acceptance.",
        "assets": assets,
    }
    (LIBRARY / "catalog.json").write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n")
    print(f"Validated {len(assets)} GLBs; {output['meshy_glb_count']} Meshy assets; wrote assets/d22/catalog.json")


if __name__ == "__main__":
    main()
