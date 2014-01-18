using SharpDX;

namespace SoftRender.Engine
{
    class Mesh
    {
        public string Name { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3[] Vertices { get; private set; }

        public Mesh(string name, int vertexCount)
        {
            Name = name;
            Vertices = new Vector3[vertexCount];
        }

        public Mesh(string name, Vector3[] vertices)
            :this(name, vertices.Length)
        {
            vertices.CopyTo(Vertices, 0);
        }
    }
}
