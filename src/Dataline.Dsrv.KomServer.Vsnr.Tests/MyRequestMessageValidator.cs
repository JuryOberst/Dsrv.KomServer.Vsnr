using System.Collections.Generic;

namespace Dsrv.KomServer.Vsnr.Tests
{
    public class MyRequestMessageValidator : IRequestMessageValidator
    {
        public IReadOnlyCollection<string> Validate(string voszRecord, IReadOnlyCollection<string> dsvvRecords)
        {
            var pruef = new de.drv.dsrv.kernpruefung.adapter.impl.KernpruefungAufrufImpl();
            var errorMessages = new List<string>();
            foreach (var dsvvTest in dsvvRecords)
            {
                var ret = pruef.pruefe(dsvvTest, voszRecord);
                if (ret.getReturnCode() != 0)
                {
                    errorMessages.AddRange(ret.getRueckgabeMeldungen());
                }
            }

            return errorMessages;
        }
    }
}
