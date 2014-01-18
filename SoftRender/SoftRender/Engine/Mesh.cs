using SharpDX;

namespace SoftRender.Engine
{
    class Mesh
    {
        #region Static Meshes

        public static Mesh Cube
        {
            get
            {
                return new Mesh("Cube",
                    new Vector3[]
                    {
                        new Vector3(-1, 1, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(-1, -1, 1),
                        new Vector3(1, -1, 1),
                        new Vector3(-1, 1, -1),
                        new Vector3(1, 1, -1),
                        new Vector3(1, -1, -1),
                        new Vector3(-1, -1, -1),
                    },
                    new Face[]
                    {
                        new Face(0, 1, 2),
                        new Face(1, 2, 3),
                        new Face(1, 3, 6),
                        new Face(1, 5, 6),
                        new Face(0, 1, 4),
                        new Face(1, 4, 5),
                        new Face(2, 3, 7),
                        new Face(3, 6, 7),
                        new Face(0, 2, 7),
                        new Face(0, 4, 7),
                        new Face(4, 5, 6),
                        new Face(4, 6, 7),
                    });
            }
        }

        #endregion

        public string Name { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3[] Vertices { get; private set; }

        public Face[] Faces { get; private set; }

        public Mesh(string name, int vertexCount, int faceCount)
        {
            Name = name;
            Vertices = new Vector3[vertexCount];
            Faces = new Face[faceCount];
        }

        public Mesh(string name, Vector3[] vertices, Face[] faces)
            :this(name, vertices.Length, faces.Length)
        {
            vertices.CopyTo(Vertices, 0);
            faces.CopyTo(Faces, 0);
        }
    }
}
