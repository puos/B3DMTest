using A4L.Mapprime3DNet;
using A4L.Mapprime3DNet.Common;
using A4L.Mapprime3DNet.Common.Workspaces;
using A4L.Mapprime3DNet.IO.DataSource;
using A4L.Mapprime3DNet.IO.World;
using A4L.Mapprime3DNet.View;
using A4L.MP3DCore.Common.Math;
using A4L.MP3DCore.Scene;
using A4L.MP3DCore.Scene.Camera;
using A4L.MP3DCore.Scene.InputHandler.HUDInputHandler;
using A4L.MP3DCore.Scene.InputHandler.ViewingInputHandler;
using A4L.MP3DCore.Scene.Renderer;
using Microsoft.Win32;
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
    private DispatcherTimer? _updateTimer;
    private B3dmDataSource? _b3dmDataSource;

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

        _objectsViewWPF.Width = 811;
        _objectsViewWPF.Height = 361;
        clientViewGrid.Children.Add(_objectsViewWPF);
        _objectsViewWPF.HorizontalAlignment = HorizontalAlignment.Stretch;
        _objectsViewWPF.VerticalAlignment = VerticalAlignment.Stretch;
        clientViewGrid.SizeChanged += (s, e) =>
        {
            _objectsViewWPF.Width = e.NewSize.Width;
            _objectsViewWPF.Height = e.NewSize.Height;
        };

        SceneView!.ShowDebugFPS = true;
        _objectsViewWPF.BackFaceCulling = true;

        SceneView.RenderAbort = (sta, dist) =>
            sta.DrawCallCount >= 18000 && dist >= 1000;

        Workspace.Instance.Owner = this;
        Workspace.Instance.ViewControl = _objectsViewWPF;
        Workspace.Instance.ViewController = ViewController;
        Workspace.Instance.CommandUi = new CommandUI(_objectsViewWPF, ViewController);
        Workspace.Instance.CommandUi.SetDefaultMouseMode();

        _axisHud = new HUD3AxisInputHandler("axis", GlobalOption.ViewAxisPixelSize);
        _axisHud.Visible = true;
        _axisHud.SetDock(HUDInputHandlerBase.DockingPositions.LeftBottom);
        SceneView.InputHandlers.AddHudHandler(_axisHud);

        GlobalOption.BackgroundColor = new ColorF(0.06f, 0.10f, 0.20f, 1.0f);
        ViewController!.BackgroundColor.Set(GlobalOption.BackgroundColor);
        ViewController.PolygonMode = PolygonModes.Fill;

        var cameraController = new CustomCameraController();
        cameraController.ZoomMinDistance = 1.0;
        ViewController.InputHandlers.Remove(cameraController);
        ViewController.InputHandlers.Add(cameraController);
        ViewController.InputHandlers.SetViewingMode(cameraController);

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
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

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (SceneView?.View?.Camera != null)
        {
            var cam = SceneView.View.Camera;
            txtCameraPos.Text = $"Camera: ({cam.Position.X:F2}, {cam.Position.Y:F2}, {cam.Position.Z:F2})";
        }

        if (_b3dmDataSource != null)
        {
            WorldGlobe.Instance.OnUpdate(_b3dmDataSource, tileObjects =>
            {
                // 타일 갱신 콜백 — B3mdRenderGroup이 내부 처리
            });
        }
    }

    private void btnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "B3DM files (*.b3dm)|*.b3dm|All files (*.*)|*.*",
            Title = "B3DM 파일 선택"
        };

        if (dlg.ShowDialog() != true) return;

        LoadB3DM(dlg.FileName);
    }

    private void LoadB3DM(string filePath)
    {
        try
        {
            txtStatus.Text = $"로딩 중... {System.IO.Path.GetFileName(filePath)}";

            SceneView!.NearFarUpdater.FarUpdateEnable = false;
            SceneView.NearFarUpdater.TargetFar = 200000;

            SceneView.SceneGroups.Clear();
            Workspace.Instance.DataSources.Clear();

            // 지구 좌표 원점 설정 (서울 기본값)
            WorldGlobe.Instance.Initialize(SceneView.View, 37.5, 126.9, 100);

            _b3dmDataSource = new B3dmDataSource(WorldGlobe.Instance);
            _b3dmDataSource.StartTile(filePath);   // async void — fire and forget

            Workspace.Instance.DataSources.Add(_b3dmDataSource);
            var group = _b3dmDataSource.CreateRenderableGroup();
            SceneView.SceneGroups.Add(group);

            Workspace.Instance.CommandUi.RefreshSceneGroup();

            ZoomFit();

            txtStatus.Text = $"로드 완료: {System.IO.Path.GetFileName(filePath)}";
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

        double fovY = ViewController.View.FovY;
        double distance = 1.0 / (2.0 * Math.Tan(fovY / 2.0 * (Math.PI / 180.0)) / bb.MaxLength) + 50;
        GlobalOption.Far = Math.Clamp(distance * 2, 10000, 1_000_000);

        ViewController.View.Camera.LookAt = bb.GetCenter();
        ViewController.View.Camera.Quat = Quaterniond.IDENTITY;
        ViewController.View.Camera.Distance = distance;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) { }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { }

    private void Window_Closed(object sender, EventArgs e)
    {
        _updateTimer?.Stop();

        if (SceneView != null)
        {
            for (int i = SceneView.SceneGroups.Count - 1; i >= 0; i--)
                SceneView.SceneGroups[i]?.Dispose();
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

    private void clientView_MouseWheel(object sender, MouseWheelEventArgs e) { }
}
