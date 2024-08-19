using UnityEngine;

namespace VoxelEngine.Rendering
{
    public enum VoxelPrimitiveType
    {
        None = 0,
        Box = 1,
        Sphere = 2,
        Cylinder = 3,
        Cone = 4,
        SolidAngle = 5,
        Torus = 6,
        TriPrism = 7,
        HexPrism = 8,
        Pyramid = 9,
        Octahedron = 10,
    }

    public enum VoxelComposeType
    {
        Union = 0,
        Intersect = 1,
        Subtract = 2,
        Xor = 3,
    }

    [System.Serializable]
    public struct VoxelModeling
    {
        public VoxelPrimitiveType type;
        [Min(0f)] public Vector3 size;
        [Min(0f)] public float round;
        [Min(0f)] public float onion;
        [Min(0f)] public float bend;
        [Min(0f)] public float twist;
        [Range(0f, 1f)] public float muvx;
        [Range(0f, 1f)] public float muvy;
        [Range(0f, 1f)] public float muvz;

        public static VoxelModeling Init
        {
            get
            {
                return new()
                {
                    type = VoxelPrimitiveType.Box,
                    size = Vector3.one,
                };
            }
        }

        public readonly Matrix4x4 Matrix
        {
            get
            {
                Matrix4x4 matrix = new();
                matrix.SetRow(0, size);
                matrix.SetRow(1, new(round, onion, bend, twist));
                matrix.SetRow(2, new(Mathf.Clamp01(muvx), Mathf.Clamp01(muvy), Mathf.Clamp01(muvz)));
                return matrix;
            }
        }

        public readonly override int GetHashCode()
        {
            return System.HashCode.Combine(type, Matrix);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is VoxelModeling modeling &&
                type == modeling.type &&
                size == modeling.size &&
                round == modeling.round &&
                onion == modeling.onion &&
                bend == modeling.bend &&
                twist == modeling.twist &&
                muvx == modeling.muvx &&
                muvy == modeling.muvy &&
                muvz == modeling.muvz;
        }

        public static bool operator ==(VoxelModeling left, VoxelModeling right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VoxelModeling left, VoxelModeling right)
        {
            return !(left == right);
        }
    }

    [System.Serializable]
    public struct VoxelComposition
    {
        public VoxelComposeType dtype;
        public float dsmooth;

        public VoxelComposeType ltype;
        public float lsmooth;

        public VoxelComposeType rtype;
        public float rsmooth;

        public static VoxelComposition Init
        {
            get { return new(); }
        }

        public readonly override int GetHashCode()
        {
            return System.HashCode.Combine(dtype, dsmooth, ltype, lsmooth, rtype, rsmooth);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is VoxelComposition composition &&
                   dtype == composition.dtype &&
                   dsmooth == composition.dsmooth &&
                   ltype == composition.ltype &&
                   lsmooth == composition.lsmooth &&
                   rtype == composition.rtype &&
                   rsmooth == composition.rsmooth;
        }

        public static bool operator ==(VoxelComposition left, VoxelComposition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VoxelComposition left, VoxelComposition right)
        {
            return !(left == right);
        }
    }

    [System.Serializable]
    public struct VoxelPrimitive
    {
        public const int STRIDE = 4 * sizeof(uint) + 5 * sizeof(int) + 51 * sizeof(float);

        public uint type;
        public Matrix4x4 ltwm;
        public Matrix4x4 wtlm;
        public Matrix4x4 model;

        public int index;
        public int up;
        public int down;
        public int left;
        public int right;

        public uint dctype;
        public uint lctype;
        public uint rctype;
        public float dsmooth;
        public float lsmooth;
        public float rsmooth;
    }

    [ExecuteInEditMode]
    public sealed class VoxelRenderer : MonoBehaviour
    {
        public VoxelModeling Modeling = VoxelModeling.Init;
        public VoxelComposition Composition = VoxelComposition.Init;
        
        private bool m_HasChanged = false;

        private VoxelModeling m_LastModeling = VoxelModeling.Init;
        private VoxelComposition m_LastComposition = VoxelComposition.Init;

        public VoxelPrimitive Primitive
        {
            get
            {
                return new()
                {
                    type = (uint)Modeling.type,
                    ltwm = transform.localToWorldMatrix,
                    wtlm = transform.worldToLocalMatrix,
                    model = Modeling.Matrix,

                    dctype = (uint)Composition.dtype,
                    lctype = (uint)Composition.ltype,
                    rctype = (uint)Composition.rtype,
                    dsmooth = Composition.dsmooth,
                    lsmooth = Composition.lsmooth,
                    rsmooth = Composition.rsmooth,
                };
            }
        }

        public bool HasChanged { get { return m_HasChanged; } }

        private void Update()
        {
            DetectChanges();
        }

        private void DetectChanges()
        {
            m_HasChanged = m_LastModeling != Modeling || m_LastComposition != Composition;

            if (m_HasChanged)
            {
                m_LastModeling = Modeling;
                m_LastComposition = Composition;
            }
        }
    }
}
