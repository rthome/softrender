using SharpDX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender.Engine
{
    class RenderDevice
    {
        private byte[] backBuffer;
        private WriteableBitmap renderTarget;

        public Color4 Color { get; set; }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int i = 0; i < backBuffer.Length; i += 4)
            {
                backBuffer[i] = b;
                backBuffer[i + 1] = g;
                backBuffer[i + 2] = r;
                backBuffer[i + 3] = a;
            }
        }

        public void PutPixel(int x, int y)
        {
            var index = (x + y * renderTarget.PixelWidth) * 4;
            backBuffer[index] = (byte)(Color.Blue * 255);
            backBuffer[index + 1] = (byte)(Color.Green * 255);
            backBuffer[index + 2] = (byte)(Color.Red * 255);
            backBuffer[index + 3] = (byte)(Color.Alpha * 255);
        }

        public Vector2 Project(Vector3 position, Matrix matrix)
        {
            var point = Vector3.TransformCoordinate(position, matrix);
            var x = point.X * renderTarget.PixelWidth + renderTarget.PixelWidth / 2f;
            var y = -point.Y * renderTarget.PixelHeight + renderTarget.PixelHeight / 2f;
            return new Vector2(x, y);
        }

        public void DrawPoint(Vector2 point)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderTarget.PixelWidth && point.Y < renderTarget.PixelHeight)
                PutPixel((int)point.X, (int)point.Y);
        }

        public void DrawLine(Vector2 p0, Vector2 p1)
        {
            int x0 = (int)p0.X;
            int y0 = (int)p0.Y;
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = (x0 < x1) ? 1 : -1;
            var sy = (y0 < y1) ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                DrawPoint(new Vector2(x0, y0));

                if ((x0 == x1) && (y0 == y1))
                    break;
                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy; 
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx; 
                    y0 += sy;
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
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)renderTarget.PixelWidth / renderTarget.PixelHeight, 0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var face in mesh.Faces)
                {
                    var vertexA = mesh.Vertices[face.V0];
                    var vertexB = mesh.Vertices[face.V1];
                    var vertexC = mesh.Vertices[face.V2];

                    var pix0 = Project(vertexA, transformMatrix);
                    var pix1 = Project(vertexB, transformMatrix);
                    var pix2 = Project(vertexC, transformMatrix);

                    DrawLine(pix0, pix1);
                    DrawLine(pix1, pix2);
                    DrawLine(pix2, pix0);
                }
            }
        }

        public RenderDevice(WriteableBitmap bmp)
        {
            renderTarget = bmp;
            backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
        }
    }
}
