using System.Collections.Generic;

namespace Dsrv.KomServer.Vsnr
{
    public interface IRequestMessageValidator
    {
        IReadOnlyCollection<string> Validate(string voszRecord, IReadOnlyCollection<string> dsvvRecords);
    }
}
