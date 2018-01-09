using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Services.MarketEngine
{
    public class ProfitingOwnerSelector : IEntityVisitor<Outpost>,IEntityVisitor<PBSDockingBase>
    {
        private Corporation _owner;

        public void Visit(Outpost outpost)
        {
            _owner = outpost.GetSiteOwner();
        }

        public void Visit(PBSDockingBase dockingBase)
        {
            _owner = Corporation.Get(dockingBase.Owner);
        }

        [CanBeNull]
        public static Corporation GetProfitingOwner(DockingBase dockingBase)
        {
            if (dockingBase == null)
                return null;

            var selector = new ProfitingOwnerSelector();
            dockingBase.AcceptVisitor(selector);
            return selector._owner;
        }
    }
}