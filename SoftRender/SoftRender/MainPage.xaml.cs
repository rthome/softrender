using SharpDX;
using SoftRender.Engine;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender
{
    public sealed partial class MainPage : Page
    {
        RenderDevice device;
        Mesh mesh;
        Camera camera;

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            var bmp = new WriteableBitmap(640, 480);
            imageControl.Source = bmp;

            device = new RenderDevice(bmp);
            camera = new Camera
            {
                Position = new Vector3(0, 0, 10.0f),
                Target = Vector3.Zero
            };
            mesh = new Mesh("Cube", new Vector3[]
            {
                new Vector3(-1, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(-1, -1, 1),
                new Vector3(-1, -1, -1),
                new Vector3(-1, 1, -1),
                new Vector3(1, 1, -1),
                new Vector3(1, -1, 1),
                new Vector3(1, -1, -1),
            });
        }

        void Render(object sender, object e)
        {
            device.Clear(0, 0, 0, 255);
            mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            device.Render(camera, mesh);
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
