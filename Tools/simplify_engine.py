import json
import struct
import numpy as np
import os

def main():
    glb_path = "honda_b16_engine_fwd.glb"
    output_obj = "honda_b16_engine.obj"
    
    if not os.path.exists(glb_path):
        print(f"Error: {glb_path} not found.")
        return
        
    print(f"Reading {glb_path}...")
    with open(glb_path, "rb") as f:
        # GLB Header
        magic = f.read(4)
        version, length = struct.unpack("<II", f.read(8))
        if magic != b"glTF":
            print("Invalid GLB file.")
            return
            
        # JSON Chunk
        j_len, j_type = struct.unpack("<II", f.read(8))
        json_bytes = f.read(j_len)
        data = json.loads(json_bytes.decode("utf-8", errors="ignore"))
        
        # Seek relative to padded JSON length
        j_padded_len = (j_len + 3) & ~3
        f.seek(12 + 8 + j_padded_len)
        
        # BIN Chunk
        b_len, b_type = struct.unpack("<II", f.read(8))
        bin_chunk = f.read(b_len)

    print("Success reading GLB chunks.")
    
    accessors = data["accessors"]
    bufferViews = data["bufferViews"]
    
    def get_buffer_data(acc_idx):
        acc = accessors[acc_idx]
        bv = bufferViews[acc["bufferView"]]
        offset = bv.get("byteOffset", 0) + acc.get("byteOffset", 0)
        length = acc["count"] * get_element_size(acc)
        return bin_chunk[offset : offset + length]

    def get_element_size(acc):
        # Component sizes: 5120/5121 (byte)=1, 5122/5123 (short)=2, 5125/5126 (int/float)=4
        comp_sizes = {5120:1, 5121:1, 5122:2, 5123:2, 5125:4, 5126:4}
        csize = comp_sizes.get(acc["componentType"], 4)
        type_mults = {"SCALAR":1, "VEC2":2, "VEC3":3, "VEC4":4, "MAT4":16}
        tmult = type_mults.get(acc["type"], 1)
        return csize * tmult

    global_verts = []
    global_faces = []
    v_offset = 0

    def process_primitive(prim, world_matrix):
        nonlocal v_offset
        pos_acc_idx = prim["attributes"]["POSITION"]
        pos_acc = accessors[pos_acc_idx]
        pos_data = get_buffer_data(pos_acc_idx)
        
        # Read vertices
        count = pos_acc["count"]
        # POSITION accessor is always 3D float32
        verts = np.frombuffer(pos_data, dtype=np.float32, count=3*count).reshape((count, 3))
        
        # Apply World Transformation Matrix
        verts_hom = np.hstack([verts, np.ones((count, 1), dtype=np.float32)])
        world_verts = (verts_hom @ world_matrix.T)[:, :3]
        
        # Read indices
        if "indices" in prim:
            ind_idx = prim["indices"]
            ind_acc = accessors[ind_idx]
            ind_data = get_buffer_data(ind_idx)
            ctype = ind_acc["componentType"]
            if ctype == 5121:
                itype = np.uint8
            elif ctype == 5123:
                itype = np.uint16
            elif ctype == 5125:
                itype = np.uint32
            else:
                itype = np.uint16
            indices = np.frombuffer(ind_data, dtype=itype, count=ind_acc["count"])
        else:
            indices = np.arange(count, dtype=np.int32)
            
        faces = indices.reshape((-1, 3)) + v_offset
        global_verts.append(world_verts)
        global_faces.append(faces)
        v_offset += count

    def process_mesh(mesh_idx, world_matrix):
        mesh = data["meshes"][mesh_idx]
        for prim in mesh.get("primitives", []):
            if "POSITION" in prim.get("attributes", {}):
                process_primitive(prim, world_matrix)

    def traverse(node_idx, parent_matrix):
        node = data["nodes"][node_idx]
        
        # Compute local matrix
        local = np.eye(4)
        if "matrix" in node:
            local = np.array(node["matrix"]).reshape(4, 4, order="F")
        else:
            if "translation" in node:
                T = np.eye(4)
                T[:3, 3] = node["translation"]
                local = local @ T
            if "rotation" in node:
                R = np.eye(4)
                q = node["rotation"] # x, y, z, w
                x, y, z, w = q
                R[:3, :3] = [
                    [1 - 2*y*y - 2*z*z,     2*x*y - 2*z*w,       2*x*z + 2*y*w],
                    [2*x*y + 2*z*w,         1 - 2*x*x - 2*z*z,   2*y*z - 2*x*w],
                    [2*x*z - 2*y*w,         2*y*z + 2*x*w,       1 - 2*x*x - 2*y*y]
                ]
                local = local @ R
            if "scale" in node:
                S = np.eye(4)
                S[0,0], S[1,1], S[2,2] = node["scale"]
                local = local @ S
                
        world = parent_matrix @ local
        
        if "mesh" in node:
            process_mesh(node["mesh"], world)
            
        if "children" in node:
            for child_idx in node["children"]:
                traverse(child_idx, world)

    print("Traversing scene hierarchy...")
    scene_idx = data.get("scene", 0)
    scene = data["scenes"][scene_idx]
    for root_idx in scene["nodes"]:
        traverse(root_idx, np.eye(4))

    # Concatenate all parts
    if len(global_verts) == 0:
        print("No vertices found in GLB.")
        return
        
    all_verts = np.vstack(global_verts)
    all_faces = np.vstack(global_faces)
    print(f"Original mesh: Vertices: {len(all_verts)}, Faces: {len(all_faces)}")
    
    # ── METRIC DECIMATION (VERTEX CLUSTERING) ──
    print("Decimating mesh via Voxel Clustering...")
    # Bounding box
    min_b = all_verts.min(axis=0)
    max_b = all_verts.max(axis=0)
    size_b = max_b - min_b
    print(f"Bounding Box: Min {min_b}, Max {max_b}, Size {size_b}")
    
    # We want a target representation of around 3000-8000 faces/vertices.
    # voxel resolution of 35x35x35 is ideal.
    res = 35
    voxel_idx = np.floor((all_verts - min_b) / (size_b + 1e-6) * (res - 1)).astype(np.int32)
    voxel_1d = voxel_idx[:, 0] + voxel_idx[:, 1] * res + voxel_idx[:, 2] * (res * res)
    
    # Find unique voxels
    unique_voxels, inverse_idx = np.unique(voxel_1d, return_inverse=True)
    num_repr = len(unique_voxels)
    print(f"Clustered down to {num_repr} representative vertices.")
    
    # Calculate average positions for each voxel
    repr_verts = np.zeros((num_repr, 3), dtype=np.float32)
    counts = np.zeros(num_repr, dtype=np.int32)
    np.add.at(repr_verts, inverse_idx, all_verts)
    np.add.at(counts, inverse_idx, 1)
    repr_verts /= counts[:, None]
    
    # Update faces
    dec_faces = inverse_idx[all_faces]
    
    # Filter degenerate faces (where at least two vertices are matching)
    valid = (dec_faces[:, 0] != dec_faces[:, 1]) & \
            (dec_faces[:, 1] != dec_faces[:, 2]) & \
            (dec_faces[:, 2] != dec_faces[:, 0])
    dec_faces = dec_faces[valid]
    
    # Keep only unique faces (independent of vertex order)
    sorted_dec_faces = np.sort(dec_faces, axis=1)
    _, u_indices = np.unique(sorted_dec_faces, axis=0, return_index=True)
    dec_faces = dec_faces[u_indices]
    
    print(f"Decimated mesh size: Vertices: {len(repr_verts)}, Faces: {len(dec_faces)}")
    
    # Center and normalize coordinates so it fits nicely in the 3D control
    dec_min = repr_verts.min(axis=0)
    dec_max = repr_verts.max(axis=0)
    dec_center = (dec_min + dec_max) / 2.0
    dec_size = dec_max - dec_min
    max_dim = dec_size.max()
    
    # Target size of the model in the viewer is about 120 units
    scale_factor = 120.0 / max_dim
    # Apply centering and scaling
    repr_verts = (repr_verts - dec_center) * scale_factor
    
    # Write to OBJ format
    print(f"Writing output to {output_obj}...")
    with open(output_obj, "w") as out:
        out.write("# Honda B16 Engine FWD Low-Poly\n")
        out.write(f"# Vertices: {len(repr_verts)}, Faces: {len(dec_faces)}\n")
        for v in repr_verts:
            out.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
        for f in dec_faces:
            # OBJ indices are 1-based
            out.write(f"f {f[0]+1} {f[1]+1} {f[2]+1}\n")
            
    print("Done! Optimization complete.")

if __name__ == "__main__":
    main()
