using SharpDX;

namespace SoftRender.Engine
{
    class Mesh
    {
        public string Name { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vertex[] Vertices { get; private set; }

        public Face[] Faces { get; private set; }

        public Mesh(string name, int vertexCount, int faceCount)
        {
            Name = name;
            Vertices = new Vertex[vertexCount];
            Faces = new Face[faceCount];
        }

        public Mesh(string name, Vertex[] vertices, Face[] faces)
            :this(name, vertices.Length, faces.Length)
        {
            vertices.CopyTo(Vertices, 0);
            faces.CopyTo(Faces, 0);
        }
    }
}
