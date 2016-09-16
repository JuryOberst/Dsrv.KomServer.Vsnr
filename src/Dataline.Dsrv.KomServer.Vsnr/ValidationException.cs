using System;
using System.Collections.Generic;
using System.Linq;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Eine Exception, falls die Validierung der DSVV-Meldungen fehlschlägt
    /// </summary>
    public class ValidationException : VsnrException
    {
        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationException"/> Klasse.
        /// </summary>
        /// <param name="dbfeRecords">Die rohen DBFE-Datenbausteine</param>
        public ValidationException(IEnumerable<string> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationException"/> Klasse.
        /// </summary>
        /// <param name="dbfeRecords">Die bereits dekodierten DBFE-Datenbausteine</param>
        public ValidationException(IEnumerable<DBFE> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationException"/> Klasse.
        /// </summary>
        /// <param name="dbfeRecords">Die lange Form der bereits dekodierten DBFE-Datenbausteine</param>
        public ValidationException(IEnumerable<DBFELANG> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationException"/> Klasse.
        /// </summary>
        /// <param name="validationErrors">Die Fehler-Informationen</param>
        public ValidationException(IEnumerable<ValidationError> validationErrors)
        {
            Errors = (validationErrors as IReadOnlyCollection<ValidationError>) ?? validationErrors.ToList();
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationException"/> Klasse.
        /// </summary>
        /// <param name="validationErrors">Die Fehler-Informationen</param>
        public ValidationException(IReadOnlyCollection<ValidationError> validationErrors)
        {
            Errors = validationErrors;
        }

        /// <summary>
        /// Holt die Fehlerinformationen
        /// </summary>
        public IReadOnlyCollection<ValidationError> Errors { get; }

        /// <inheritdoc />
        public override string Message => string.Join(Environment.NewLine, Errors.Select(x => x.ToString()));
    }
}
