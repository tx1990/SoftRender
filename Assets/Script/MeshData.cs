using UnityEngine;

namespace SoftRender
{
    public struct Vertex
    {
        public Vector4 Pos;
        public Vector4 Normal;
        public Vector2 Texcoord;
    }

    public class MeshData
    {
        public Vertex[] Vertex { get; private set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Position { get; set; }
        public int[,] Triangle { get; set; }
        public Color[] Colors { get; set; }

        public MeshData(Vertex[] vertex, Vector3 pos, Vector3 rotation, int[,] index, Color[] colors)
        {
            Vertex = vertex;
            Position = pos;
            Rotation = rotation;
            Triangle = index;
            Colors = colors;
        }

        public Matrix4x4 GetLocalToWorld()
        {
            var matrix = Matrix4x4.Translate(Position) * Matrix4x4.Rotate(Quaternion.Euler(0, Rotation.y, 0)) *
                         Matrix4x4.Rotate(Quaternion.Euler(Rotation.x, 0, 0)) *
                         Matrix4x4.Rotate(Quaternion.Euler(0, 0, Rotation.z));
            return matrix;
        }
    }
}
