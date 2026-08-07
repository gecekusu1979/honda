import json, struct

with open("honda_b16_engine_fwd.glb", "rb") as f:
    magic = f.read(4)
    version, length = struct.unpack("<II", f.read(8))
    print(f"Magic: {magic}, Version: {version}, Length: {length}")
    
    # Chunk 0
    chunk_len, chunk_type = struct.unpack("<II", f.read(8))
    print(f"Chunk 0 Length: {chunk_len}, Type: {chunk_type:08X}")
    json_bytes = f.read(chunk_len)
    json_str = json_bytes.decode("utf-8", errors="ignore")
    data = json.loads(json_str)
    
    print("Keys in GLTF:", list(data.keys()))
    if "meshes" in data:
        print("Number of meshes:", len(data["meshes"]))
        for i, mesh in enumerate(data["meshes"][:5]):
            print(f"Mesh {i}: {mesh.get('name', '')}")
            for j, prim in enumerate(mesh.get("primitives", [])):
                print(f"  Primitive {j}: Attributes {list(prim.get('attributes', {}).keys())}, Indices {prim.get('indices', '')}")
