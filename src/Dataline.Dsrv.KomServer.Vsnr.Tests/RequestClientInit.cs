using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Autofac;

using Dataline.Entities;

using ExtraStandard;
using ExtraStandard.DrvKomServer.Extra14.Dsv;
using ExtraStandard.DrvKomServer.Validation.Extra14.Dsv;
using ExtraStandard.Encryption;

using Itsg.Ostc.Certificates;

using Org.BouncyCastle.Pkcs;

using Xunit;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public class RequestClientInit : IAsyncLifetime
    {
        public ReceiverCertificates ReceiverCertificates { get; private set; }

        public IContainer Container { get; private set; }

        public async Task InitializeAsync()
        {
            ReceiverCertificates = await ReceiverCertificates.Load(CustomerData.SenderCertificate.IsSha256()).ConfigureAwait(false);

            var cb = new ContainerBuilder();

            cb.RegisterType<MyHttpClientFactory>().AsImplementedInterfaces();
            cb.RegisterType<DrvExtraValidatorFactory>().As<IDrvDsvExtra14ValidatorFactory>();
            cb.RegisterType<MyRequestMessageValidator>().As<IRequestMessageValidator>();
            cb.RegisterType<RequestClient>().AsSelf()
              .WithParameter(
                  (pi, ctx) => pi.ParameterType == typeof(AbsenderOnlineversand),
                  (pi, ctx) => CustomerData.Sender)
              .WithParameter(
                  (pi, ctx) => pi.ParameterType == typeof(Firma),
                  (pi, ctx) => CustomerData.Company)
              .WithParameter(
                  (pi, ctx) => pi.Name == "isTest" && pi.ParameterType == typeof(bool),
                  (pi, ctx) => true)
              .WithParameter(
                  (pi, ctx) => pi.ParameterType == typeof(IExtraEncryptionHandler),
                  (pi, ctx) =>
                  {
                      var receiverCert = ReceiverCertificates.Certificates[CustomerData.DsrvBn];
                      var senderPkcsStore = new Pkcs12Store(new MemoryStream(CustomerData.SenderCertificate.Export(X509ContentType.Pkcs12)), new char[0]);
                      var receiverCertBc = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(receiverCert.RawData);
                      var encryption = new Pkcs7EncryptionHandler(senderPkcsStore, receiverCertBc);
                      return encryption;
                  });

            Container = cb.Build();
        }

        public Task DisposeAsync()
        {
            Container.Dispose();
            return Task.FromResult(0);
        }
    }
}
