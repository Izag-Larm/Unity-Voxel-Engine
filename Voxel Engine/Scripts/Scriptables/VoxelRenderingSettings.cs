using UnityEngine;

namespace VoxelEngine.Rendering
{
    [CreateAssetMenu(fileName = "Rendering Settings", menuName = "Voxel Engine/Rendering Settings")]
    public class VoxelRenderingSettings : ScriptableObject
    {
        [field: SerializeField] public ComputeShader MeshBuilder { get; private set; }

        public ComputeBuffer PrimitivesBuffer { get; private set; }
        public ComputeBuffer ItsBuffer { get; private set; }
        public ComputeBuffer UnitsBuffer { get; private set; }
        public ComputeBuffer TrianglesBuffer { get; private set; }

        private static ComputeBuffer InitBuffer(int count, int stride, ComputeBuffer buffer = null)
        {
            if (buffer == null || buffer.count != count || buffer.stride != stride)
            {
                if (count > 0)
                {
                    buffer?.Release();
                    buffer = new ComputeBuffer(count, stride);
                }
                else { buffer?.Dispose(); }
            }

            return buffer;
        }

        private static ComputeBuffer InitBuffer<T>(T[] data, int stride, ComputeBuffer buffer = null)
        {
            buffer = InitBuffer(data.Length, stride, buffer);
            buffer?.SetData(data);
            return buffer;
        }

        public void InitBuffers(VoxelPrimitive[] primitives, int unitsLen, int trisLen)
        {
            PrimitivesBuffer = InitBuffer(primitives, VoxelPrimitive.STRIDE, PrimitivesBuffer);
            ItsBuffer = InitBuffer(primitives.Length * unitsLen, 4 * sizeof(float), ItsBuffer);

            UnitsBuffer = InitBuffer(unitsLen, VoxelUnit.STRIDE, UnitsBuffer);
            TrianglesBuffer = InitBuffer(trisLen, VoxelTriangle.STRIDE, TrianglesBuffer);
        }
    }
}