using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using A4L.Mapprime3DNet.IO.B3DM.GLTF.Loader;

namespace B3DMTest.Tools;

/// <summary>
/// MapPrimeNet 의 WebRequestLoader 는 내부적으로 private static readonly HttpClient(SocketsHttpHandler) 를
/// 사용한다. SocketsHttpHandler 는 ServicePointManager 의 전역 인증서 콜백을 무시하므로,
/// 자체 서명(self-signed) HTTPS 서버에 접속하면 "SSL connection could not be established" 가 발생한다.
///
/// MapPrimeNet 소스를 수정하지 않기 위해, 인증서 검증을 우회하는 HttpClient 를 만들어
/// 리플렉션으로 해당 static 필드에 주입한다.
///
/// ⚠ 인증서 검증을 전면 우회하므로 내부 테스트 용도로만 사용할 것.
/// </summary>
internal static class SslBypass
{
    private static bool _applied;

    public static void Apply()
    {
        if (_applied) return;

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 2,
            UseProxy = false,
            EnableMultipleHttp2Connections = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                   | System.Net.DecompressionMethods.Deflate,
            SslOptions = new SslClientAuthenticationOptions
            {
                // 자체 서명 인증서 검증 우회
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
            }
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // static 필드 이니셜라이저(원본 httpClient 생성)를 먼저 실행시킨 뒤 덮어쓴다.
        RuntimeHelpers.RunClassConstructor(typeof(WebRequestLoader).TypeHandle);

        var field = typeof(WebRequestLoader).GetField(
            "httpClient", BindingFlags.NonPublic | BindingFlags.Static);

        if (field == null)
            throw new InvalidOperationException(
                "WebRequestLoader.httpClient 필드를 찾지 못했습니다. MapPrimeNet 구현이 변경되었을 수 있습니다.");

        // .NET 8 에서는 initonly(static readonly) 필드를 Reflection SetValue 로 변경할 수 없으므로
        // DynamicMethod 로 stsfld IL 을 직접 emit 하여 우회한다.
        var dm = new DynamicMethod(
            "__set_httpClient", null, new[] { typeof(HttpClient) },
            typeof(WebRequestLoader), skipVisibility: true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
        var setter = (Action<HttpClient>)dm.CreateDelegate(typeof(Action<HttpClient>));

        setter(client);
        _applied = true;
    }
}
