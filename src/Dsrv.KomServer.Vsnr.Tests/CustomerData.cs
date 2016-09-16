#define USE_GKV_SCHEMA

using System;
using System.Security.Cryptography.X509Certificates;

using Dataline.Entities;

using Microsoft.Extensions.Configuration;
using System.IO;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public static class CustomerData
    {
        public const string DsrvBn = "66667777";

        public const string RequestUrlTest = "https://itsg.eservicet-drv.de/dsvv/rest";

        public const string RequestUrlProd = "https://itsg.eservice-drv.de/dsvv/rest";

#if USE_GKV_SCHEMA
        public const string ProcedureSend = "DUA";
        
        public const string ProcedureQuery = "DeliveryServer";
        
        public const string ProcedureAcknowledge = "DeliveryServer";
#else
        public const string ProcedureSend = "http://www.extra-standard.pde/procedures/DEUEV";
        
        public const string ProcedureQuery = "http://www.extra-standard.pde/procedures/DEUEV";
        
        public const string ProcedureAcknowledge = "http://www.extra-standard.pde/procedures/DEUEV";
#endif

        public static readonly Firma Company;

        public static readonly AbsenderOnlineversand Sender;

        private static readonly Lazy<X509Certificate2> _senderCertificate;

        static CustomerData()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("test-data.json", true);

            var testDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DATALINE", "Test-Daten");
            if (Directory.Exists(testDataPath))
            {
                configBuilder = configBuilder.AddJsonFile(Path.Combine(testDataPath, "dsrv-komserver-vsnr-test.json"), true);
            }

            var config = configBuilder
                .Build();

            var senderCertData = config.GetSection("sender")["certificate"];
            if (string.IsNullOrEmpty(senderCertData))
                throw new Exception("Es wurde kein Absender-Zertifikat hinterlegt");

            _senderCertificate = new Lazy<X509Certificate2>(() => new X509Certificate2(Convert.FromBase64String(senderCertData), (string)null, X509KeyStorageFlags.Exportable));

            var senderContact = config.GetSection("sender").GetSection("contact");
            Sender = new AbsenderOnlineversand
            {
                Betriebsnummer = senderContact["betriebsnummer"],
                Name1 = senderContact["name1"],
                Name2 = senderContact["name2"],
                Strasse = senderContact["strasse"],
                PLZ = senderContact["plz"],
                Ort = senderContact["ort"],
                Ansprechpartner = senderContact["ansprechpartner"],
                IstFrau = senderContact.GetValue<bool>("istFrau"),
                Telefon = senderContact["telefon"],
                Fax = senderContact["fax"],
                Email = senderContact["email"]
            };

            if (Sender.Betriebsnummer == null)
                throw new Exception("Es wurden keine Absender-Kontaktdaten hinterlegt");

            var companyContact = config.GetSection("company").GetSection("contact");
            Company = new Firma
            {
                Betriebsnummer = companyContact["betriebsnummer"],
                Name1 = companyContact["name1"],
                Strasse = companyContact["strasse"],
                PLZ = companyContact["plz"],
                Ort = companyContact["ort"],
            };

            if (Company.Betriebsnummer == null)
                throw new Exception("Es wurden keine Firmen-Kontaktdaten hinterlegt");
        }

        public static X509Certificate2 SenderCertificate => _senderCertificate.Value;
    }
}
