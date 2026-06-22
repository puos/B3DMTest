using System.Net;
using System.IO;


namespace B3DMTest.Tools;

/// <summary>
/// 로컬 디렉터리를 HTTP로 서빙하는 경량 파일 서버.
/// StartTile() 이 HTTP URL만 받을 때 로컬 파일을 브리징하기 위해 사용.
/// </summary>
internal class LocalFileServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _rootDir;
    private readonly int _port;
    private CancellationTokenSource? _cts;
    

    public int Port => _port;

    public LocalFileServer(string rootDir, int port = 18765)
    {
        _rootDir = rootDir;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public void Start()
    {
        _listener.Start();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ServeLoop(_cts.Token));
    }

    private async Task ServeLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; }

            _ = Task.Run(() => HandleRequest(ctx), ct);
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        try
        {
            var relPath = req.Url!.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_rootDir, relPath);

            if (!File.Exists(fullPath))
            {
                res.StatusCode = 404;
                res.Close();
                return;
            }

            res.ContentType = GetMimeType(fullPath);
            res.Headers.Add("Access-Control-Allow-Origin", "*");

            var bytes = File.ReadAllBytes(fullPath);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch
        {
            res.StatusCode = 500;
        }
        finally
        {
            res.Close();
        }
    }

    private static string GetMimeType(string path) => Path.GetExtension(path).ToLower() switch
    {
        ".json" => "application/json",
        ".b3dm" => "application/octet-stream",
        ".glb"  => "model/gltf-binary",
        ".gltf" => "model/gltf+json",
        ".bin"  => "application/octet-stream",
        _       => "application/octet-stream",
    };

    public void Dispose()
    {
        _cts?.Cancel();
        _listener.Stop();
        _listener.Close();
    }
}
