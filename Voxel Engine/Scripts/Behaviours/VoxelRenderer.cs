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
        [Min(0)] public Vector3 size;
        [Min(0)] public float round;
        [Min(0)] public float onion;
        [Min(0)] public float bend;
        [Min(0)] public float twist;
        [Min(0)] public Vector3 material;

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
                matrix.SetRow(2, new(Mathf.Clamp01(material.x), Mathf.Clamp01(material.y), Mathf.Clamp01(material.z)));
                return matrix;
            }
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

    public class VoxelRenderer : MonoBehaviour
    {
        public VoxelModeling Modeling = VoxelModeling.Init;
        public VoxelComposition Composition = VoxelComposition.Init;

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
    }
}