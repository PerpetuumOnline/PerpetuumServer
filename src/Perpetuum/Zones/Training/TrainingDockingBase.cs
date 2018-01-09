using System.Linq;
using Perpetuum.Common;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Services.Channels;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.Training
{
    public class TrainingDockingBase : DockingBase
    {
        public TrainingDockingBase(IChannelManager channelManager,ICentralBank centralBank,IRobotTemplateRelations robotTemplateRelations,DockingBaseHelper dockingBaseHelper) : base(channelManager,centralBank,robotTemplateRelations,dockingBaseHelper)
        {
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public DefaultCorporation GetTrainingCorporation()
        {
            var eid = DefaultCorporationDataCache.GetByAlliance(Owner).FirstOrDefault();
            return (DefaultCorporation) Corporation.GetOrThrow(eid);
        }
    }
}