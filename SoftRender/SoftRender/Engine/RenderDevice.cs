using SharpDX;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender.Engine
{
    class RenderDevice
    {
        private byte[] backBuffer;
        private WriteableBitmap renderTarget;

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

        public void PutPixel(int x, int y, Color4 color)
        {
            var index = (x + y * renderTarget.PixelWidth) * 4;
            backBuffer[index] = (byte)(color.Blue * 255);
            backBuffer[index + 1] = (byte)(color.Green * 255);
            backBuffer[index + 2] = (byte)(color.Red * 255);
            backBuffer[index + 3] = (byte)(color.Alpha * 255);
        }

        public Vector2 Project(Vector3 position, Matrix matrix)
        {
            var point = Vector3.TransformCoordinate(position, matrix);
            var x = point.X * renderTarget.PixelWidth + renderTarget.PixelWidth / 2f;
            var y = -point.Y * renderTarget.PixelHeight + renderTarget.PixelHeight / 2f;
            return new Vector2(x, y);
        }

        public void DrawPoint(Vector2 point, Color color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderTarget.PixelWidth && point.Y < renderTarget.PixelHeight)
            {
                PutPixel((int)point.X, (int)point.Y, color);
            }
        }

        public void Present()
        {
            using (var stream = renderTarget.PixelBuffer.AsStream())
                stream.Write(backBuffer, 0, backBuffer.Length);
            renderTarget.Invalidate();
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)renderTarget.PixelWidth / renderTarget.PixelHeight, 0.01f, 1.0f);

            foreach (Mesh mesh in meshes) 
            { 
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) * 
                                  Matrix.Translation(mesh.Position);
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var vertex in mesh.Vertices)
                {
                    var point = Project(vertex, transformMatrix);
                    DrawPoint(point, Color.Yellow);
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
