#define USE_GKV_SCHEMA

namespace Dsrv.KomServer.Vsnr
{
    internal static class DsrvConstants
    {
        public const string Betriebsnummer = "66667777";

        public const string RequestUrlTest = "https://itsg.eservicet-drv.de/dsvv/rest";

        public const string RequestUrlProd = "https://itsg.eservice-drv.de/dsvv/rest";

#if USE_GKV_SCHEMA
        public const string ProcedureSend = "DUA";
        
        public const string ProcedureQuery = "DeliveryServer";
        
        public const string ProcedureAcknowledge = "DeliveryServer";
#else
        public const string ProcedureSend = "http://www.extra-standard.pde/procedures/DEUEV";

        public const string ProcedureQuery = "http://www.extra-standard.pde/procedures/DEUEV";

        public const string ProcedureAcknowledge = "http://www.extra-standard.pde/procedures/DEUEV";
#endif
    }
}
