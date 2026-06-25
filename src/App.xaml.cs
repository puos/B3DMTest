using B3DMTest.Tools;
using SIMMETA;
using System.Windows;

namespace B3DMTest;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LasCustomUtil.AppName = "B3DMTest";
        string outputFolder = LasCustomUtil.GetAppPath();
        SIMMETALogUtil.IsConsoleLog = true;
        SIMMETALogUtil.Init(outputFolder);


        // 자체 서명 HTTPS 서버 접속을 위해 인증서 검증 우회 HttpClient 주입
        SslBypass.Apply();

        var viewer = new Viewer3D();
        viewer.Show();
    }
}
