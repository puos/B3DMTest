using A4L.Mapprime3DNet;
using A4L.Mapprime3DNet.Common;
using A4L.Mapprime3DNet.Common.Workspaces;
using A4L.Mapprime3DNet.IO.DataSource;
using A4L.Mapprime3DNet.IO.World;
using A4L.Mapprime3DNet.View;
using A4L.MP3DCore.Common.DebugUtils;
using A4L.MP3DCore.Common.Math;
using A4L.MP3DCore.Scene;
using A4L.MP3DCore.Scene.InputHandler.HUDInputHandler;
using A4L.MP3DCore.Scene.InputHandler.ViewingInputHandler;
using A4L.MP3DCore.Scene.Renderer;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace B3DMTest;

public partial class Viewer3D : Window
{
    private ObjectsViewWPF? _objectsViewWPF;
    private ViewController? ViewController => _objectsViewWPF?.ViewController;
    public SceneView? SceneView => _objectsViewWPF?.SceneView;

    private HUD3AxisInputHandler? _axisHud;
    private B3dmDataSource? _b3dmDataSource;
    private bool _pendingZoomFit;

    private string targetUrl = "https://211.178.39.228:22480/terra_b3dms_HyeopjaeBeach_prev/tileset.json"; 
    
    private double targetLatitude = 33.398628;
    private double targetLongitude = 126.243173;


    public Viewer3D()
    {
        InitializeComponent();
        InitializeScene();
    }

    private void InitializeScene()
    {
        _objectsViewWPF = new ObjectsViewWPF();
        _objectsViewWPF.SetController(new ViewController());
        ConfigureNearFar();

        SetSize(_objectsViewWPF, new Size(811, 361));
        clientViewGrid.Children.Add(_objectsViewWPF);
        _objectsViewWPF.HorizontalAlignment = HorizontalAlignment.Stretch;
        _objectsViewWPF.VerticalAlignment = VerticalAlignment.Stretch;
        clientViewGrid.SizeChanged += (s, e) =>
        {
            SetSize(_objectsViewWPF, e.NewSize);
        };

        SceneView.DebugHelper = GlobalContext.GetInstance().DebugHelper;

        SceneView.DebugHelper.Enable = true;
        SceneView.DebugHelper.Visible = true;
        SceneView.DebugHelper.LockObject = new object();
        _objectsViewWPF.BackFaceCulling = false;
        SceneView.ShowDebugFPS = true;

        Workspace.Instance.Owner = this;
        Workspace.Instance.ViewControl = _objectsViewWPF;
        Workspace.Instance.ViewController = ViewController;
        Workspace.Instance.CommandUi = new CommandUI(_objectsViewWPF, ViewController);
        Workspace.Instance.CommandUi.SetDefaultMouseMode();

        if(_axisHud == null)
        {
            _axisHud = new HUD3AxisInputHandler("axis", GlobalOption.ViewAxisPixelSize);
            _axisHud.Visible = true;
            _axisHud.SetDock(HUDInputHandlerBase.DockingPositions.LeftBottom);
            SceneView.InputHandlers.AddHudHandler(_axisHud);
        }

        GlobalOption.BackgroundColor = new ColorF(0.06f, 0.10f, 0.20f, 1.0f);
        ViewController!.BackgroundColor.Set(GlobalOption.BackgroundColor);

        ViewController.PolygonMode = PolygonModes.Fill;
        ViewController.SceneView.Renderer.ShadingMode = GlobalOption.ShadingMode;
        

        var cameraController = new CustomCameraController();
        cameraController.ZoomMinDistance = 1.0;

        ViewController.InputHandlers.Remove(cameraController);
        ViewController.InputHandlers.Add(cameraController);
        ViewController.InputHandlers.SetViewingMode(cameraController);
    }

    private void ConfigureNearFar()
    {
        if (SceneView?.NearFarUpdater == null) return;

        GlobalOption.Near = 0.1;
        SceneView.NearFarUpdater.NearUpdateEnable = false;
        SceneView.NearFarUpdater.TargetNear = GlobalOption.Near;
        ViewController!.View.Near = GlobalOption.Near;
        GlobalOption.Far = 10000;
    }

    private void LoadB3DM()
    {
        try
        {
            SceneView.NearFarUpdater.FarUpdateEnable = false;
            SceneView.NearFarUpdater.NearUpdateEnable = false;
            SceneView.NearFarUpdater.TargetFar = 200000;

            SceneView.SceneGroups.Clear();
            Workspace.Instance.DataSources.Clear();

            WorldGlobe.Instance.Initialize(SceneView.View, targetLatitude, targetLongitude , 0); 
            _b3dmDataSource = new B3dmDataSource(WorldGlobe.Instance);

            _b3dmDataSource.StartTile(targetUrl);

            Workspace.Instance.DataSources.Add(_b3dmDataSource);
            var group = _b3dmDataSource.CreateRenderableGroup();
            SceneView.SceneGroups.Add(group);

            Workspace.Instance.CommandUi.RefreshSceneGroup();

            SceneView.View.Camera.LookAt = new Vector3d(0, 0, 0);
            SceneView.View.Camera.Quat = Quaterniond.IDENTITY;
            SceneView.View.Camera.Distance = 100;


            txtStatus.Text = $"로드 완료: {targetUrl}";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"오류: {ex.Message}";
            MessageBox.Show(ex.Message, "B3DM 로드 실패");
        }
    }

    private void ZoomFit()
    {
        var bb = Workspace.Instance.BoundingBox;
        if (bb == null || ViewController == null) return;
        // 타일 콘텐츠가 아직 로드되지 않아 bbox 가 비어 있으면(Valid=false / MaxLength<=0) 적용하지 않는다.
        if (!bb.Valid || bb.MaxLength <= 0 || double.IsNaN(bb.MaxLength)) return;

        double fovY = ViewController.View.FovY;
        double distance = 1.0 / (2.0 * Math.Tan(fovY / 2.0 * (Math.PI / 180.0)) / bb.MaxLength) + 50;
        GlobalOption.Far = Math.Clamp(distance * 2, 10000, 1_000_000);

        ViewController.View.Camera.LookAt = bb.GetCenter();
        ViewController.View.Camera.Quat = Quaterniond.IDENTITY;
        ViewController.View.Camera.Distance = distance;
    }

    private static void SetSize(FrameworkElement element, Size value)
    {
        element.Width = value.Width;
        element.Height = value.Height;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) 
    {
        txtUrl.Text = targetUrl;
        LoadB3DM();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void Window_Closed(object sender, EventArgs e)
    {
        //_fileServer?.Dispose();

        if (SceneView != null)
        {
            SceneView.SceneGroups.Clear();
        }

        Workspace.Instance.DataSources.Clear();
    }

    private void Border_KeyDown(object sender, KeyEventArgs e) { }

    private void Border_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Z:
                ZoomFit();
                break;
            case Key.F:
                if (SceneView != null)
                    SceneView.ShowDebugFPS = !SceneView.ShowDebugFPS;
                break;
        }
    }

    public void LoadB3dmUrl(string urlPath) => targetUrl = urlPath;

    public void GlobeSetPosition(double latitude, double longitude)
    {
        targetLatitude = latitude;
        targetLongitude = longitude;
    }

}
