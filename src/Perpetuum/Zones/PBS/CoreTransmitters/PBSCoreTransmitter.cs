using System.Collections.Generic;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.PBS.CoreTransmitters
{
    /// <summary>
    /// Transfers it's own energy to the target nodes
    /// </summary>
    public class PBSCoreTransmitter : PBSActiveObject, IPBSCorePump, IPBSAcceptsCore
    {
        private readonly CorePumpHandler<PBSCoreTransmitter> _corePumpHandler;

        public ICorePumpHandler CorePumpHandler { get { return _corePumpHandler; } }

        public PBSCoreTransmitter()
        {
            _corePumpHandler = new CorePumpHandler<PBSCoreTransmitter>(this);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void PBSActiveObjectAction(IZone zone)
        {
            _corePumpHandler.TransferToConnections();
        }

        public override IDictionary<string, object> GetDebugInfo()
        {
            var info = base.GetDebugInfo();
            _corePumpHandler.AddToDictionary(info);
           
            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            _corePumpHandler.AddToDictionary(info);
            
            return info;
        }
    }
}



