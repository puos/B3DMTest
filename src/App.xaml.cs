using System.Windows;

namespace B3DMTest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewer = new Viewer3D();
        viewer.Show();
    }
}
