using SharpDX;
using SoftRender.Engine;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender
{
    public sealed partial class MainPage : Page
    {
        RenderDevice device;
        Camera camera;
        List<Mesh> meshes;

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            var bmp = new WriteableBitmap(640, 480);
            imageControl.Source = bmp;

            device = new RenderDevice(bmp)
            {
                Color = Color4.White,
            };
            camera = new Camera
            {
                Position = new Vector3(0, 0, 10.0f),
                Target = Vector3.Zero
            };

            var importer = new BabylonImporter();
            var importedMeshes = await importer.LoadFileAsync("icosphere.babylon");

            meshes = new List<Mesh>();
            foreach(var mesh in importedMeshes)
                meshes.Add(mesh);
            meshes.Add(new Mesh("Cube",
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
                }));
        }

        void Render(object sender, object e)
        {
            device.Clear(0, 0, 0, 255);
            foreach (var mesh in meshes)
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            device.Render(camera, meshes);
            device.Present();
        }

        public MainPage()
        {
            InitializeComponent();

            Loaded += PageLoaded;
            CompositionTarget.Rendering += Render;
        }
    }
}
