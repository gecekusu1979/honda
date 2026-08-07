import json, struct

with open("honda_b16_engine_fwd.glb", "rb") as f:
    f.read(12)  # Skip header
    chunk_len, chunk_type = struct.unpack("<II", f.read(8))
    json_bytes = f.read(chunk_len)
    data = json.loads(json_bytes.decode("utf-8"))

total_vertices = 0
total_indices = 0

accessors = data["accessors"]
bufferViews = data["bufferViews"]

for mesh in data["meshes"]:
    for prim in mesh.get("primitives", []):
        pos_acc_idx = prim["attributes"]["POSITION"]
        pos_acc = accessors[pos_acc_idx]
        total_vertices += pos_acc["count"]
        
        if "indices" in prim:
            ind_acc_idx = prim["indices"]
            ind_acc = accessors[ind_acc_idx]
            total_indices += ind_acc["count"]

print(f"Total Vertices: {total_vertices}")
print(f"Total Indices (triangles x 3): {total_indices}")
