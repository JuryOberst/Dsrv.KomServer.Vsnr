using System;

namespace Dsrv.KomServer.Vsnr
{
    /// <summary>
    /// Die Basis-Exception für die Bibliothek für die Versicherungsnummernabfrage
    /// </summary>
    public class VsnrException : Exception
    {
        /// <summary>
        /// Initialisiert eine neue <see cref="VsnrException"/> Klasse.
        /// </summary>
        public VsnrException()
        {
        }

        /// <summary>
        /// Initialisiert eine neue <see cref="VsnrException"/> Klasse.
        /// </summary>
        /// <param name="message">Die Fehlermeldung</param>
        public VsnrException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initialisiert eine neue <see cref="VsnrException"/> Klasse.
        /// </summary>
        /// <param name="message">Die Fehlermeldung</param>
        /// <param name="inner">Die innere Exception</param>
        public VsnrException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
