using SharpDX;
using SoftRender.Engine;
using System;
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
            var bmp = new WriteableBitmap(1024, 768);
            imageControl.Source = bmp;

            device = new RenderDevice(bmp);
            camera = new Camera
            {
                Position = new Vector3(0, 0, 10.0f),
                Target = Vector3.Zero
            };

            var importer = new BabylonImporter();
            var importedMeshes = await importer.LoadFileAsync("model.babylon");

            meshes = new List<Mesh>();
            foreach(var mesh in importedMeshes)
                meshes.Add(mesh);
            foreach(var mesh in importedMeshes)
            {
                var newMesh = new Mesh("MonkeyClone", mesh.Vertices, mesh.Faces);
                newMesh.Position = new Vector3(2.65f, 0, 0);
                meshes.Add(newMesh);
            }

            CompositionTarget.Rendering += Render;
        }

        void Render(object sender, object e)
        {
            device.Clear(0, 0, 0, 255);
            foreach (var mesh in meshes)
                mesh.Rotation = new Vector3(mesh.Rotation.X, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            device.Render(camera, meshes);
            device.Present();
        }

        public MainPage()
        {
            InitializeComponent();

            Loaded += PageLoaded;
        }
    }
}
