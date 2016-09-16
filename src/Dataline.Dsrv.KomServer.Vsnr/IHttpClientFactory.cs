using System;
using System.Net.Http;

namespace Dsrv.KomServer.Vsnr
{
    public interface IHttpClientFactory
    {
        HttpClient CreateHttpClient();
    }
}
