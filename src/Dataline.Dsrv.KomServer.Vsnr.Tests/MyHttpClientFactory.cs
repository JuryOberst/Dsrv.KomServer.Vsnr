using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public class MyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateHttpClient()
        {
            var messageHandler = CreateHttpClientHandler(true);
            var client = new HttpClient(messageHandler)
            {
                DefaultRequestHeaders =
                {
                    UserAgent =
                    {
                        new ProductInfoHeaderValue("DatalineLohnabzug", "26.03.00")
                    }
                }
            };

            return client;
        }

        public static HttpClientHandler CreateHttpClientHandler(bool withClientCertificate)
        {
            var messageHandler = new WebRequestHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };

            if (withClientCertificate)
            {
                messageHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                messageHandler.ClientCertificates.Add(CustomerData.SenderCertificate);
            }

            return messageHandler;
        }
    }
}
