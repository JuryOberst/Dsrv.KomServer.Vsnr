using System.Collections.Generic;
using System.IO;
using System.Linq;

using BeanIO;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Die Datei für die Versicherungsnummernabfrage
    /// </summary>
    public class VsnrFile
    {
        private VsnrFile(VOSZ vorlaufDsrv, VOSZ vorlaufSender, IReadOnlyList<DSVV01> dsvv, NCSZ nachlaufSender, NCSZ nachlaufDsrv)
        {
            VorlaufDsrv = vorlaufDsrv;
            VorlaufSender = vorlaufSender;
            DSVV = dsvv;
            NachlaufSender = nachlaufSender;
            NachlaufDsrv = nachlaufDsrv;
            Datensaetze =
                new IDatensatz[] { vorlaufDsrv, vorlaufSender }
                    .Concat(dsvv)
                    .Concat(new IDatensatz[] { nachlaufSender, nachlaufDsrv })
                    .ToList();
        }

        /// <summary>
        /// Holt den Vorlaufsatz von der DSRV
        /// </summary>
        public VOSZ VorlaufDsrv { get; }

        /// <summary>
        /// Holt den ursprünglichen Vorlaufsatz (vom ursprünglichen Absender)
        /// </summary>
        public VOSZ VorlaufSender { get; }

        /// <summary>
        /// Die Datensätze für die Versicherungsnummernabfrage
        /// </summary>
        public IReadOnlyList<DSVV01> DSVV { get; }

        /// <summary>
        /// Holt den ursprünglichen Nachlaufsatz (vom ursprünglichen Absender)
        /// </summary>
        public NCSZ NachlaufSender { get; }

        /// <summary>
        /// Holt den Nachlaufsatz von der DSRV
        /// </summary>
        public NCSZ NachlaufDsrv { get; }

        /// <summary>
        /// Holt alle Datensätze die in der Rückmeldung vorhanden waren
        /// </summary>
        public IReadOnlyList<IDatensatz> Datensaetze { get; }

        internal static VsnrFile Load(StreamFactory factory, TextReader file)
        {
            var reader = factory.CreateReader("dsvv-deuev-v01", file);
            var vorlaufDsrv = (VOSZ)reader.Read();
            var vorlaufSender = (VOSZ)reader.Read();
            var dsvv = new List<DSVV01>();
            var record = reader.Read();
            while (reader.RecordName == "DSVV")
            {
                dsvv.Add((DSVV01)record);
                record = reader.Read();
            }
            var nachlaufSender = (NCSZ)record;
            var nachlaufDsrv = (NCSZ)reader.Read();
            return new VsnrFile(vorlaufDsrv, vorlaufSender, dsvv, nachlaufSender, nachlaufDsrv);
        }
    }
}
