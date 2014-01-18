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
            var cube0 = Mesh.Cube;
            var cube1 = Mesh.Cube;
            cube0.Position = new Vector3(0, 2.25f, 0); 
            cube1.Position = new Vector3(0, -2.25f, 0);
            meshes.Add(cube0);
            meshes.Add(cube1);
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
