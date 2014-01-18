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

        void ProcessScanline(int y, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Color4 color)
        {
            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            var gradient1 = p0.Y != p1.Y ? (y - p0.Y) / (p1.Y - p0.Y) : 1;
            var gradient2 = p2.Y != p3.Y ? (y - p2.Y) / (p3.Y - p2.Y) : 1;

            var sx = (int)p0.X.Interpolate(p1.X, gradient1);
            var ex = (int)p2.X.Interpolate(p3.X, gradient2);

            // starting Z & ending Z
            var z1 = p0.Z.Interpolate(p1.Z, gradient1);
            var z2 = p2.Z.Interpolate(p3.Z, gradient2);

            // drawing a line from left (sx) to right (ex) 
            for (var x = sx; x < ex; x++)
            {
                var gradient = (x - sx) / (float)(ex - sx);
                var z = z1.Interpolate(z2, gradient);
                DrawPoint(new Vector3(x, y, z), color);
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

        public Vector3 Project(Vector3 position, Matrix matrix)
        {
            var point = Vector3.TransformCoordinate(position, matrix);
            var x = point.X * renderWidth + renderWidth / 2f;
            var y = -point.Y * renderHeight + renderHeight / 2f;
            return new Vector3(x, y, point.Z);
        }

        public void DrawPoint(Vector3 point, Color4 color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderWidth && point.Y < renderHeight)
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
        }

        public void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color4 color)
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            if (p2.Y > p3.Y)
            {
                var temp = p2;
                p2 = p3;
                p3 = temp;
            }

            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            // inverse slopes
            float dP1P2, dP1P3;
            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

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
            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanline(y, p1, p3, p1, p2, color);
                    }
                    else
                    {
                        ProcessScanline(y, p1, p3, p2, p3, color);
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
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                    {
                        ProcessScanline(y, p1, p2, p1, p3, color);
                    }
                    else
                    {
                        ProcessScanline(y, p2, p3, p1, p3, color);
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
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)renderWidth / renderHeight, 0.01f, 1.0f);

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

                    var pix0 = Project(vertexA, transformMatrix);
                    var pix1 = Project(vertexB, transformMatrix);
                    var pix2 = Project(vertexC, transformMatrix);

                    var color = 0.25f + (i % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                    DrawTriangle(pix0, pix1, pix2, new Color4(color, color, color, 1));
                });
            }
        }

        public RenderDevice(WriteableBitmap bmp)
        {
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
