using System;
using System.Text;

using SocialInsurance.Germany.Messages.Pocos;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Fehler einer Validierung der DSVV-Meldung
    /// </summary>
    public class ValidationError
    {
        private static readonly char[] _whitespace = {' '};

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationError"/> Klasse.
        /// </summary>
        /// <param name="dbfe">Der rohe DBFE-Datenbaustein</param>
        public ValidationError(string dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.Substring(4));
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = null;
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationError"/> Klasse.
        /// </summary>
        /// <param name="dbfe">Der bereits dekodierte DBFE-Datenbaustein</param>
        public ValidationError(DBFE dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.FE);
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = null;
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="ValidationError"/> Klasse.
        /// </summary>
        /// <param name="dbfe">Die lange Form des bereits dekodierten DBFE-Datenbausteins</param>
        public ValidationError(DBFELANG dbfe)
        {
            var codeAndMessage = GetCodeAndMessage(dbfe.FE);
            Code = codeAndMessage.Item1;
            Message = codeAndMessage.Item2.TrimEnd();
            Description = dbfe.FELANG.TrimEnd();
        }

        /// <summary>
        /// Holt den Fehler-Code
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Holt die Fehler-Meldung
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Holt die Fehler-Beschreibung (kann leer sein)
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Holt einen Wert, der angibt, ob dieser Eintrag ein Fehler ist - oder nur zur Information
        /// </summary>
        public bool IsError => Code != "NCSZH10";

        private static Tuple<string, string> GetCodeAndMessage(string fe)
        {
            var parts = fe.Split(_whitespace, 2);
            return Tuple.Create(parts[0], parts[1]);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var result = new StringBuilder($"{Code}: {Message}");
            if (!string.IsNullOrEmpty(Description))
                result.AppendLine().Append(Description);
            return result.ToString();
        }
    }
}
