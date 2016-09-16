using System.Collections.Generic;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Schnittstelle für die Implementation einer Validierung der Anfrage-Meldung (DSVV)
    /// </summary>
    public interface IRequestMessageValidator
    {
        /// <summary>
        /// Validierung der DSVV-Datensätze
        /// </summary>
        /// <param name="voszRecord">Der Vorlaufsatz</param>
        /// <param name="dsvvRecords">Die DSVV-Datensätze</param>
        /// <returns>Die Meldungen mit den Validierungsfehlern</returns>
        IReadOnlyCollection<string> Validate(string voszRecord, IReadOnlyCollection<string> dsvvRecords);
    }
}
