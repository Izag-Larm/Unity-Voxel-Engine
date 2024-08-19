using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelEngine.Rendering
{
    [System.Serializable]
    public struct VoxelVolume
    {
        public const int STRIDE = 38 * sizeof(float);

        public Matrix4x4 ltwm;
        public Matrix4x4 wtlm;
        public Vector3 size;
        public Vector3Int sampling;
    }

    [System.Serializable]
    public struct VoxelUnit
    {
        public const int STRIDE = 3 * sizeof(int) + 4 * sizeof(float);

        public Vector3Int id;
        public Vector3 uv;
        public float distance;
    }

    [System.Serializable]
    public struct VoxelVertex
    {
        public const int STRIDE = 9 * sizeof(float);

        public Vector3 position, normal, uv;
    }

    [System.Serializable]
    public struct VoxelTriangle
    {
        public const int STRIDE = sizeof(uint) + 3 * VoxelVertex.STRIDE;

        public uint isValid;
        public VoxelVertex a, b, c;

        public readonly bool IsValid { get { return isValid > 0; } }
        public readonly VoxelVertex[] Vertices { get { return new VoxelVertex[] { a, b, c }; } }
    }

    public class VoxelShape : MonoBehaviour
    {
        public VoxelRenderingSettings Settings;
        public bool AutoRegenerate;

        [Header("Shape Modeling")]
        [Min(0)] public Vector3 Size = Vector3.one;
        [Min(0)] public float Round = 0f;
        public float Smooth = 0f;

        [Header("Shape Volume")]
        public Vector3 VolumeCenter = Vector3.zero;
        [Min(0)] public Vector3 VolumeSize = Vector3.one;
        [Min(1)] public Vector3Int VolumeSampling = Vector3Int.one;

        private VoxelPrimitive[] m_Primitives = new VoxelPrimitive[0];
        private VoxelUnit[] m_Units = new VoxelUnit[0];
        private VoxelTriangle[] m_Triangles = new VoxelTriangle[0];

        private MeshFilter m_Filter;

        public VoxelVolume Volume
        {
            get
            {
                Matrix4x4 ltwm = transform.localToWorldMatrix;
                ltwm.SetColumn(3, ltwm.GetColumn(3) + new Vector4(VolumeCenter.x, VolumeCenter.y, VolumeCenter.z, 0f));

                return new()
                {
                    ltwm = ltwm,
                    wtlm = ltwm.inverse,
                    size = VolumeSize,
                    sampling = VolumeSampling,
                };
            }
        }

        public VoxelPrimitive[] Primitives
        {
            get { return m_Primitives.ToArray(); }
        }

        public MeshFilter Filter
        {
            get
            {
                if (m_Filter == null)
                {
                    m_Filter = GetComponent<MeshFilter>();
                }
                return m_Filter;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(VolumeCenter, VolumeSize);
        }
#endif

        public VoxelPrimitive[] GetShapePrimitives()
        {
            List<VoxelPrimitive> primitives = new();
            GetShapePrimitives(this, transform, 0, -1, -1, ref primitives);

            primitives = primitives.OrderBy(shape => shape.index).ToList();

            for (int i = 0; i < primitives.Count; i++)
            {
                int index = primitives[i].index;
                for (int j = 0; j < primitives.Count; j++)
                {
                    VoxelPrimitive primitive = primitives[j];

                    primitive.model.SetRow(0, new()
                    {
                        x = primitive.model.GetRow(0).x * Size.x,
                        y = primitive.model.GetRow(0).y * Size.y,
                        z = primitive.model.GetRow(0).z * Size.z,
                        w = primitive.model.GetRow(0).w * 1f,
                    });

                    primitive.model[1, 0] += Round;

                    primitive.index = primitive.index == index ? i : primitive.index;
                    primitive.up = primitive.up == index ? i : primitive.up;
                    primitive.down = primitive.down == index ? i : primitive.down;
                    primitive.left = primitive.left == index ? i : primitive.left;
                    primitive.right = primitive.right == index ? i : primitive.right;

                    primitives[j] = primitive;
                }
            }

            m_Primitives = primitives.ToArray();

            return m_Primitives;
        }

        public static void GetShapePrimitives(VoxelShape shape, Transform pivot, int index, int up, int right, ref List<VoxelPrimitive> primitives)
        {
            if (pivot != shape.transform && pivot.GetComponent<VoxelShape>() != null)
            {
                return;
            }

            int count = primitives.Count;

            VoxelPrimitive primitive = new()
            {
                type = (uint)VoxelPrimitiveType.None,
                ltwm = pivot.localToWorldMatrix,
                wtlm = pivot.worldToLocalMatrix,
                index = index,
                up = up,
                down = -1,
                left = -1,
                right = right,
            };

            VoxelRenderer[] renderers = pivot.GetComponents<VoxelRenderer>().Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length > 0)
            {
                int dIndex = index;
                for (int i = 0; i < renderers.Length; i++)
                {
                    primitive = renderers[i].Primitive;
                    primitive.index = dIndex;
                    primitive.up = i == 0 ? up : (dIndex - 1) / 3;
                    primitive.right = i == 0 ? right : -1;
                    primitive.left = -1;

                    dIndex = (3 * dIndex) + 2;
                    primitive.down = i + 1 < renderers.Length ? dIndex : -1;

                    primitives.Add(primitive);
                }
            }
            else { primitives.Add(primitive); }

            Transform[] childs = new bool[pivot.childCount].Select((_, i) => pivot.GetChild(i))
                .Where(child => child.gameObject.activeInHierarchy).ToArray();
            if (childs.Length > 0)
            {
                int rIndex = (3 * index) + 1;
                for (int i = 0; i < childs.Length; i++)
                {
                    if (i == 0)
                    {
                        primitive = primitives[count];
                        primitive.left = rIndex;
                        primitives[count] = primitive;
                    }

                    int nextRIndex = i + 1 < childs.Length ? (3 * rIndex) + 3 : -1;
                    GetShapePrimitives(shape, childs[i], rIndex, (rIndex - 1) / 3, nextRIndex, ref primitives);

                    rIndex = nextRIndex;
                }
            }
        }

        public void GenerateMesh()
        {
            if (Filter == null)
            {
                Debug.LogWarning($"Component {typeof(MeshFilter)} has required to generate mesh");
                return;
            }

            if (Settings == null || Settings.MeshBuilder == null)
            {
                Debug.LogWarning($"Settings ({typeof(VoxelRenderingSettings)}) not set completly.");
                return;
            }

            VoxelVolume volume = Volume;

            VoxelPrimitive[] primitives = GetShapePrimitives();
            int unitsLen = (1 + volume.sampling.x) * (1 + volume.sampling.y) * (1 + volume.sampling.z);
            int trisLen = 5 * volume.sampling.x * volume.sampling.y * volume.sampling.z;
            Settings.InitBuffers(primitives, unitsLen, trisLen);

            Settings.MeshBuilder.SetVector("_VolumeSize", volume.size);
            Settings.MeshBuilder.SetVector("_VolumeCenter", transform.position + VolumeCenter);
            Settings.MeshBuilder.SetVector("_VolumeSampling", new (volume.sampling.x, volume.sampling.y, volume.sampling.z));
            Settings.MeshBuilder.SetMatrix("_LocalToWorld", volume.ltwm);
            Settings.MeshBuilder.SetMatrix("_WorldToLocal", volume.wtlm);

            int kindex = Settings.MeshBuilder.FindKernel("Voxeler");

            Settings.MeshBuilder.SetBuffer(kindex, "_Primitives", Settings.PrimitivesBuffer);
            Settings.MeshBuilder.SetBuffer(kindex, "_Its", Settings.ItsBuffer);
            Settings.MeshBuilder.SetBuffer(kindex, "_Units", Settings.UnitsBuffer);

            Vector3Int threadGroupSize = new()
            {
                x = Mathf.CeilToInt((volume.sampling.x + 1) / 4f),
                y = Mathf.CeilToInt((volume.sampling.y + 1) / 4f),
                z = Mathf.CeilToInt((volume.sampling.z + 1) / 4f),
            };

            Settings.MeshBuilder.Dispatch(kindex, threadGroupSize.x, threadGroupSize.y, threadGroupSize.z);

            m_Units = new VoxelUnit[unitsLen];
            Settings.UnitsBuffer.GetData(m_Units);

            kindex = Settings.MeshBuilder.FindKernel("MarchingCube");

            Settings.MeshBuilder.SetBuffer(kindex, "_Primitives", Settings.PrimitivesBuffer);
            Settings.MeshBuilder.SetBuffer(kindex, "_Its", Settings.ItsBuffer);
            Settings.MeshBuilder.SetBuffer(kindex, "_Units", Settings.UnitsBuffer);
            Settings.MeshBuilder.SetBuffer(kindex, "_Triangles", Settings.TrianglesBuffer);

            Settings.MeshBuilder.Dispatch(kindex, threadGroupSize.x, threadGroupSize.y, threadGroupSize.z);

            m_Triangles = new VoxelTriangle[trisLen];
            Settings.TrianglesBuffer.GetData(m_Triangles);

#if UNITY_EDITOR
            DestroyImmediate(Filter.sharedMesh);
#else
            Destroy(Filter.sharedMesh);
#endif
            
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> tris = new();

            foreach (VoxelTriangle triangle in m_Triangles.Where(tri => tri.IsValid))
            {
                int count = vertices.Count;

                vertices.AddRange(triangle.Vertices.Select(v => v.position + VolumeCenter));
                normals.AddRange(triangle.Vertices.Select(v => v.normal));
                uvs.AddRange(triangle.Vertices.Select(v => new Vector2(v.uv.x, v.uv.y)));

                tris.AddRange(new int[] { count, count + 1, count + 2 });
            }

            Mesh mesh = new()
            {
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                triangles = tris.ToArray()
            };
            mesh.RecalculateBounds();

            Filter.sharedMesh = mesh;
        }
    }
}