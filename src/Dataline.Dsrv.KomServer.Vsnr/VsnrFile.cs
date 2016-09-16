using System.Collections.Generic;
using System.IO;
using System.Linq;

using BeanIO;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
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

        public VOSZ VorlaufDsrv { get; }
        public VOSZ VorlaufSender { get; }
        public IReadOnlyList<DSVV01> DSVV { get; }
        public NCSZ NachlaufSender { get; }
        public NCSZ NachlaufDsrv { get; }

        public IReadOnlyList<IDatensatz> Datensaetze { get; }

        public static VsnrFile Load(StreamFactory factory, TextReader file)
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
