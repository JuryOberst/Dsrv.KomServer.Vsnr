using System;
using System.Text;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
    public class ValidationError
    {
        private static readonly char[] _whitespace = {' '};

        public ValidationError(string dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.Substring(4));
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = null;
        }

        public ValidationError(DBFE dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.FE);
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = null;
        }

        public ValidationError(DBFELANG dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.FE);
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = dbfe.FELANG.TrimEnd();
        }

        public string Code { get; }
        public string Message { get; }
        public string Description { get; }
        public bool IsError => Code != "NCSZH10";

        private static Tuple<string, string> GetCodeAndMessage(string fe)
        {
            var parts = fe.Split(_whitespace, 2);
            return Tuple.Create(parts[0], parts[1]);
        }

        public override string ToString()
        {
            var result = new StringBuilder($"{Code}: {Message}");
            if (!string.IsNullOrEmpty(Description))
                result.AppendLine().Append(Description);
            return result.ToString();
        }
    }
}
