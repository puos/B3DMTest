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
        
        //viewer.LoadB3dmUrl("https://211.178.39.228:22480/terra_b3dms_HyeopjaeBeach_prev/tileset.json");
        //viewer.GlobeSetPosition(33.398628, 126.243173);

        viewer.LoadB3dmUrl("https://211.178.39.228:22480/terra_b3dms_test/4/tileset.json");
        viewer.GlobeSetPosition(37.40697, 126.63336);

        viewer.Show();
    }
}
