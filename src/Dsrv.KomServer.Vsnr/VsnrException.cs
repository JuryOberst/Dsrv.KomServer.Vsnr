using System;

namespace Dsrv.KomServer.Vsnr
{
    public class VsnrException : Exception
    {
        public VsnrException()
        {
        }

        public VsnrException(string message) : base(message)
        {
        }

        public VsnrException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
