using System;
using System.Collections.Generic;
using System.Linq;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
    public class ValidationException : VsnrException
    {
        public ValidationException(IEnumerable<string> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        public ValidationException(IEnumerable<DBFE> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        public ValidationException(IEnumerable<DBFELANG> dbfeRecords)
        {
            Errors = dbfeRecords.Select(x => new ValidationError(x)).ToList();
        }

        public ValidationException(IEnumerable<ValidationError> validationErrors)
        {
            Errors = (validationErrors as IReadOnlyCollection<ValidationError>) ?? validationErrors.ToList();
        }

        public ValidationException(IReadOnlyCollection<ValidationError> validationErrors)
        {
            Errors = validationErrors;
        }

        public IReadOnlyCollection<ValidationError> Errors { get; }

        public override string Message => string.Join(Environment.NewLine, Errors.Select(x => x.ToString()));
    }
}
