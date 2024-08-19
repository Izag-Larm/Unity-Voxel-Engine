using UnityEngine;
using UnityEditor;

namespace VoxelEngine.Rendering
{
    [CustomEditor(typeof(VoxelShape))]
    public class VoxelShapeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VoxelShape shape = (VoxelShape)target;

            if (GUILayout.Button("Generate Mesh"))
            {
                shape.GenerateMesh();
            }

            if (DrawDefaultInspector())
            {
                if (shape.AutoRegenerate)
                {
                    shape.GenerateMesh();
                }
            }
        }
    }
}