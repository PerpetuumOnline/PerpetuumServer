using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Presences.GrowingPresences;

namespace Perpetuum.Zones.PBS
{
    /// <summary>
    /// Deployer for a PBSEgg
    /// 
    /// This is the item in the inventory. -> capsule
    /// </summary>
    public class PBSDeployer : ItemDeployer
    {
        public PBSDeployer(IEntityServices entityServices) : base(entityServices)
        {
        }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            var corporationEid = player.CorporationEid;
            DefaultCorporationDataCache.IsCorporationDefault(corporationEid).ThrowIfTrue(ErrorCodes.CharacterMustBeInPrivateCorporation);

            var pbsEgg = (PBSEgg)base.CreateDeployableItem(zone, spawnPosition, player);
            
            //let the egg check the conditions by type
            pbsEgg.CheckDefinitionRelatedConditionsOrThrow(zone, spawnPosition, corporationEid);

            var pbsObjectDefinition = PBSHelper.GetPBSObjectDefinitionFromCapsule(ED);

            var pbsEd = EntityDefault.Get(pbsObjectDefinition);

            //check zone for conditions
            PBSHelper.CheckZoneForDeployment(zone, spawnPosition,pbsEd).ThrowIfError();

            //Check distance to NPC Base spawns
            NPCBasePresenceUtils.WithinRangeOfNPCBase(zone, spawnPosition).ThrowIfTrue(ErrorCodes.TooCloseToNPCBase);

            //pass owner
            pbsEgg.DeployerPlayer = player;

            //all conditions match return the egg and continue placing it to the zone
            return pbsEgg;
        }
    }
    
}
