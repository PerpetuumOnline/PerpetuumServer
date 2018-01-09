using System;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.Turrets;

namespace Perpetuum.Zones.PBS
{

    /// <summary>
    /// Summonable object that players has to activate to build pbs nodes
    /// </summary>
    public class PBSEgg : Egg
    {
        private readonly IEntityServices _entityServices;
        private bool _successfulSummon;
        
        public PBSEgg(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public override void Initialize()
        {
            DespawnTime = TimeSpan.FromHours(1);
            base.Initialize();
        }

        protected override void OnSummonSuccess(IZone zone, Player[] summoners)
        {
            DoSummon(zone);
        }

        private void DoSummon(IZone zone)
        {
            Logger.Info("DoSummon starts on zone:" + zone.Id + " " + this);
            if (DeployerPlayer == null)
            {
                Logger.Error("no deployer player " + this);
                return;
            }

            var corporation = DeployerPlayer.Character.GetPrivateCorporation();
            if (corporation == null)
            {
                Logger.Error("no private corporation was found. Deployer character: " + DeployerPlayer.Character);
                DeployerPlayer.Character.SendErrorMessage(new Command("pbsDeployItem"), ErrorCodes.PrivateCorporationAllowedOnly);
                return;
            }

            var centerTile = CurrentPosition.Center;
            PBSDockingBase dockingBase;
            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var deployableItem = (Unit)_entityServices.Factory.CreateWithRandomEID(TargetPBSNodeDefault);
                    var zoneStorage = zone.Configuration.GetStorage();

                    zoneStorage.AddChild(deployableItem);
                    SetStartCore(deployableItem);

                    dockingBase = deployableItem as PBSDockingBase;
                    if (dockingBase != null)
                        PBSHelper.CreatePBSDockingBase(dockingBase);

                    deployableItem.Owner = corporation.Eid;
                    deployableItem.Orientation = FastRandom.NextInt(0, 3)*0.25;
                    deployableItem.CurrentPosition = CurrentPosition.Center;

                    if (deployableItem is PBSTurret turret)
                    {
                        // csak a turret kell, gyerekek nem
                        Repository.Insert(turret);
                    }
                    else
                    {
                        // itt mindent insertalunk
                        deployableItem.Save();
                    }
                    
                    Logger.Info("node saved to sql " + deployableItem);

                    Logger.Info("pbs insert start in zoneuser entities: " + deployableItem);
                    zone.UnitService.AddUserUnit(deployableItem,centerTile);

                    Logger.Info("pbs log starting " + deployableItem);
                    PBSHelper.WritePBSLog(PBSLogType.deployed, deployableItem.Eid, deployableItem.Definition, deployableItem.Owner, DeployerPlayer.Character.Id, background: false, zoneId: zone.Id);

                    Transaction.Current.OnCompleted((completed) =>
                    {
                        if (!completed)
                        {
                            Logger.Error("DoSummon rollback " + this);
                            return;
                        }

                        Logger.Info("starting zone enter: " + deployableItem);
                        deployableItem.AddToZone(zone, centerTile);
                        Logger.Info("added to zone " + deployableItem);

                        dockingBase?.OnDockingBaseDeployed();

                        //draw terrain stuff
                        PBSHelper.OnPBSObjectDeployed(zone, deployableItem, true, true, true);
                        Logger.Info("terrain stuff done, sending update. " + deployableItem);

                        //send update
                        ((IPBSObject) deployableItem).SendNodeUpdate();

                        zone.CreateBeam(BeamType.red_20sec,
                            builder =>
                                builder.WithPosition(CurrentPosition.Center)
                                    .WithState(BeamState.Hit)
                                    .WithDuration(15000));

                        Logger.Info("pbs node successfully deployed.");
                        _successfulSummon = true;
                    });

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        private void SetStartCore(Unit unit)
        {
            if (unit is IPBSAcceptsCore || unit is IPBSCorePump )
            {
                unit.Core = 0;
            }
        }

        public void CheckDefinitionRelatedConditionsOrThrow(IZone zone, Position spawnPosition, long owner)
        {
            //the egg WILL build this pbsNode after activation
            var pbsNodeEntityDefault = TargetPBSNodeDefault;
            
            if (pbsNodeEntityDefault == EntityDefault.None)
            {
                Logger.Error("pbsNodeDefinition was not found:" + pbsNodeEntityDefault + " in egg:" + Definition + " " + ED.Name);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            
            if (pbsNodeEntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_pbs_docking_base))
            {
                //check deploying of bases
                PBSHelper.ValidatePBSDockingBasePlacement(zone, spawnPosition, owner, pbsNodeEntityDefault).ThrowIfError();
                return;
            }

            //kiveve amiket lehet kivulre pakolni
            if (! PBSHelper.IsPlaceableOutsideOfBase(pbsNodeEntityDefault.CategoryFlags))
            {
                //checks for corporation's docking base in range
                PBSHelper.ValidatePBSNodePlacing(zone, spawnPosition, owner, pbsNodeEntityDefault).ThrowIfError();

            }

            //itt meg lehet mast is megnezni egyelore nem kellett
           
        }

        public EntityDefault TargetPBSNodeDefault
        {
            get { return ED.Config.TargetEntityDefault; }
        }

        protected override void OnDead(Unit killer)
        {
            var zone = Zone;
            PBSHelper.OnPBSEggRemoved(zone, this);

            base.OnDead(killer);
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            if (!_successfulSummon)
            {
                PBSHelper.OnPBSEggRemoved(zone, this);
            }

            base.OnRemovedFromZone(zone);
        }

        private int _constructionRadius;
        public int GetConstructionRadius()
        {
            return PBSHelper.LazyInitConstructionRadiusByEgg(this, ref _constructionRadius);
        }
        

        public Player DeployerPlayer { get; set; }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            if (enterType == ZoneEnterType.Deploy)
            {
                PBSHelper.OnPBSEggDeployed(Zone, this); 
            }

            base.OnEnterZone(zone, enterType);
        }
    }
}
