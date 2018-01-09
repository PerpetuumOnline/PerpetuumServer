using Perpetuum.Deployers;
using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.PunchBags
{
    public class PunchBagDeployer : ItemDeployer
    {
        public PunchBagDeployer(IEntityServices entityServices) : base(entityServices)
        {
        }
    }
}