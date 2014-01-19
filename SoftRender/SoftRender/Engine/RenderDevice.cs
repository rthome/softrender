using SharpDX;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender.Engine
{
    class RenderDevice
    {
        private readonly byte[] backBuffer;
        private readonly float[] depthBuffer;
        private readonly object[] lockBuffer;

        private readonly WriteableBitmap renderTarget;

        private readonly int renderWidth, renderHeight;

        public Vector3 LightPosition;

        void ProcessScanline(ScanlineData d, Vertex v0, Vertex v1, Vertex v2, Vertex v3, Color4 color)
        {
            var p0 = v0.Coordinates;
            var p1 = v1.Coordinates;
            var p2 = v2.Coordinates;
            var p3 = v3.Coordinates;

            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            var gradient1 = p0.Y != p1.Y ? (d.Y - p0.Y) / (p1.Y - p0.Y) : 1;
            var gradient2 = p2.Y != p3.Y ? (d.Y - p2.Y) / (p3.Y - p2.Y) : 1;

            var sx = (int)gradient1.Interpolate(p0.X, p1.X);
            var ex = (int)gradient2.Interpolate(p2.X, p3.X);

            // starting Z & ending Z
            var z1 = gradient1.Interpolate(p0.Z, p1.Z);
            var z2 = gradient2.Interpolate(p2.Z, p3.Z);

            var snl = gradient1.Interpolate(d.NDotL0, d.NDotL1);
            var enl = gradient2.Interpolate(d.NDotL2, d.NDotL3);

            // drawing a line from left (sx) to right (ex) 
            for (var x = sx; x < ex; x++)
            {
                var gradient = (x - sx) / (float)(ex - sx);
                var z = gradient.Interpolate(z1, z2);
                var ndotl = gradient.Interpolate(snl, enl);
                DrawPoint(new Vector3(x, d.Y, z), color * ndotl);
            }
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int i = 0; i < backBuffer.Length; i += 4)
            {
                backBuffer[i] = b;
                backBuffer[i + 1] = g;
                backBuffer[i + 2] = r;
                backBuffer[i + 3] = a;
            }
            for (int i = 0; i < depthBuffer.Length; i++)
                depthBuffer[i] = float.MaxValue;
        }

        public void PutPixel(int x, int y, float z, Color4 color)
        {
            var index = (x + y * renderWidth);
            var index4 = index * 4;

            // Enter critical section
            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                    return;

                depthBuffer[index] = z;
                backBuffer[index4] = (byte)(color.Blue * 255);
                backBuffer[index4 + 1] = (byte)(color.Green * 255);
                backBuffer[index4 + 2] = (byte)(color.Red * 255);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
            }
        }

        public Vertex Project(Vertex vertex, Matrix transformation, Matrix world)
        {
            // transforming the coordinates into 2D space
            var point2d = Vector3.TransformCoordinate(vertex.Coordinates, transformation);
            // transforming the coordinates & the normal to the vertex in the 3D world
            var point3dWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);
            var normal3dWorld = Vector3.TransformCoordinate(vertex.Normal, world);

            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point2d.X * renderWidth + renderWidth / 2f;
            var y = -point2d.Y * renderHeight + renderHeight / 2f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld
            };
        }

        public void DrawPoint(Vector3 point, Color4 color)
        {
            if (point.X >= 0 && point.X < renderWidth && point.Y >= 0 && point.Y < renderHeight)
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
        }

        public void DrawTriangle(Vertex v0, Vertex v1, Vertex v2, Color4 color)
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (v0.Coordinates.Y > v1.Coordinates.Y)
                MathExtensions.Swap(ref v0, ref v1);
            if (v1.Coordinates.Y > v2.Coordinates.Y)
                MathExtensions.Swap(ref v1, ref v2);
            if (v0.Coordinates.Y > v1.Coordinates.Y)
                MathExtensions.Swap(ref v0, ref v1);

            Vector3 p0 = v0.Coordinates;
            Vector3 p1 = v1.Coordinates;
            Vector3 p2 = v2.Coordinates;

            // computing the cos of the angle between the light vector and the normal vector
            // it will return a value between 0 and 1 that will be used as the intensity of the color
            var nl0 = MathExtensions.ComputeNDotL(ref v0.WorldCoordinates, ref v0.Normal, ref LightPosition);
            var nl1 = MathExtensions.ComputeNDotL(ref v1.WorldCoordinates, ref v1.Normal, ref LightPosition);
            var nl2 = MathExtensions.ComputeNDotL(ref v2.WorldCoordinates, ref v2.Normal, ref LightPosition);

            // computing lines' directions
            // http://en.wikipedia.org/wiki/Slope
            // Computing slopes
            var d1 = (p1.Y - p0.Y > 0) ? (p1.X - p0.X) / (p1.Y - p0.Y) : 0;
            var d2 = (p2.Y - p0.Y > 0) ? (p2.X - p0.X) / (p2.Y - p0.Y) : 0;

            var data = new ScanlineData();
            // First case where triangles are like that:
            // P1
            // -
            // -- 
            // - -
            // -  -
            // -   - P2
            // -  -
            // - -
            // -
            // P3
            if (d1 > d2)
            {
                for (var y = (int)p0.Y; y <= (int)p2.Y; y++)
                {
                    data.Y = y;

                    if (y < p1.Y)
                    {
                        data.NDotL0 = nl0;
                        data.NDotL1 = nl2;
                        data.NDotL2 = nl0;
                        data.NDotL3 = nl1;
                        ProcessScanline(data, v0, v2, v0, v1, color);
                    }
                    else
                    {
                        data.NDotL0 = nl0;
                        data.NDotL1 = nl2;
                        data.NDotL2 = nl1;
                        data.NDotL3 = nl2;
                        ProcessScanline(data, v0, v2, v1, v2, color);
                    }
                }
            }
            // First case where triangles are like that:
            //       P1
            //        -
            //       -- 
            //      - -
            //     -  -
            // P2 -   - 
            //     -  -
            //      - -
            //        -
            //       P3
            else
            {
                for (var y = (int)p0.Y; y <= (int)p2.Y; y++)
                {
                    data.Y = y;

                    if (y < p1.Y)
                    {
                        data.NDotL0 = nl0;
                        data.NDotL1 = nl1;
                        data.NDotL2 = nl0;
                        data.NDotL3 = nl2;
                        ProcessScanline(data, v0, v1, v0, v2, color);
                    }
                    else
                    {
                        data.NDotL0 = nl1;
                        data.NDotL1 = nl2;
                        data.NDotL2 = nl0;
                        data.NDotL3 = nl2;
                        ProcessScanline(data, v1, v2, v0, v2, color);
                    }
                }
            }
        }

        public void Present()
        {
            using (var stream = renderTarget.PixelBuffer.AsStream())
                stream.Write(backBuffer, 0, backBuffer.Length);
            renderTarget.Invalidate();
        }

        public void Render(Camera camera, List<Mesh> meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)renderWidth / renderHeight, 0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                Parallel.For(0, mesh.Faces.Length, i =>
                {
                    var face = mesh.Faces[i];

                    var vertexA = mesh.Vertices[face.V0];
                    var vertexB = mesh.Vertices[face.V1];
                    var vertexC = mesh.Vertices[face.V2];

                    var pix0 = Project(vertexA, transformMatrix, worldMatrix);
                    var pix1 = Project(vertexB, transformMatrix, worldMatrix);
                    var pix2 = Project(vertexC, transformMatrix, worldMatrix);

                    DrawTriangle(pix0, pix1, pix2, new Color4(1.0f));
                });
            }
        }

        public RenderDevice(WriteableBitmap bmp)
        {
            LightPosition = new Vector3(0, 10, 10);

            renderTarget = bmp;
            renderWidth = bmp.PixelWidth;
            renderHeight = bmp.PixelHeight;

            backBuffer = new byte[renderWidth * renderHeight * 4];
            depthBuffer = new float[renderWidth * renderHeight];
            lockBuffer = new object[renderWidth * renderHeight];
            for (int i = 0; i < lockBuffer.Length; i++)
                lockBuffer[i] = new object();
        }
    }
}
