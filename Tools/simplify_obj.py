import numpy as np
import os
import sys

def main():
    obj_path = r"b16/tinker.obj"
    output_obj = "honda_b16_engine.obj"
    
    if not os.path.exists(obj_path):
        print(f"Error: {obj_path} not found.")
        return
        
    print(f"Reading {obj_path}...")
    verts = []
    faces = []
    
    with open(obj_path, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            if line.startswith("v "):
                parts = line.strip().split()
                if len(parts) >= 4:
                    verts.append([float(parts[1]), float(parts[2]), float(parts[3])])
            elif line.startswith("f "):
                parts = line.strip().split()
                if len(parts) >= 4:
                    idx = []
                    # Handle face indices like f v1/vt1/vn1 v2/vt2/vn2 ... or f v1 v2 v3
                    # OBJ is 1-based, we extract just the vertex index (first part before '/')
                    for p in parts[1:]:
                        v_idx = int(p.split('/')[0])
                        # Handle negative indices if any
                        if v_idx < 0:
                            idx.append(v_idx) # will adjust later when we have total vertices counted
                        else:
                            idx.append(v_idx - 1)
                    # We expect triangles, if polygon has more than 3 vertices, triangulate (fan triangulation)
                    if len(idx) == 3:
                        faces.append(idx)
                    elif len(idx) > 3:
                        for i in range(1, len(idx) - 1):
                            faces.append([idx[0], idx[i], idx[i+1]])

    total_orig_verts = len(verts)
    print(f"Loaded {total_orig_verts} vertices, {len(faces)} faces.")
    
    # Adjust negative indices
    adjusted_faces = []
    for f_idx in faces:
        adj = []
        for v in f_idx:
            if v < 0:
                adj.append(total_orig_verts + v)
            else:
                adj.append(v)
        adjusted_faces.append(adj)
        
    all_verts = np.array(verts, dtype=np.float32)
    all_faces = np.array(adjusted_faces, dtype=np.int32)
    
    # ── METRIC DECIMATION (VERTEX CLUSTERING) ──
    print("Decimating mesh via Voxel Clustering...")
    # Bounding box
    min_b = all_verts.min(axis=0)
    max_b = all_verts.max(axis=0)
    size_b = max_b - min_b
    print(f"Original Bounding Box: Min {min_b}, Max {max_b}, Size {size_b}")
    
    # We want a target representation of around 3000-8000 faces/vertices.
    # We can adjust voxel resolution (res) to get the best density.
    # For a high quality but lightweight mesh, res = 38 is ideal.
    res = 38
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
    with open(output_obj, "w", encoding="utf-8") as out:
        out.write("# Honda B16 Engine FWD Low-Poly (From user tinker.obj)\n")
        out.write(f"# Vertices: {len(repr_verts)}, Faces: {len(dec_faces)}\n")
        for v in repr_verts:
            out.write(f"v {v[0]:.4f} {v[1]:.4f} {v[2]:.4f}\n")
        for f in dec_faces:
            # OBJ indices are 1-based
            out.write(f"f {f[0]+1} {f[1]+1} {f[2]+1}\n")
            
    print("Done! Optimization complete.")

if __name__ == "__main__":
    main()
