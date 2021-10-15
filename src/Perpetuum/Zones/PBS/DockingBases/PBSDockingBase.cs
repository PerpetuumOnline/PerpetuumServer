using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.ControlTower;

namespace Perpetuum.Zones.PBS.DockingBases
{
    /// <summary>
    /// Player built docking base
    /// </summary>
    public class PBSDockingBase : DockingBase, IPBSObject,  IStandingController
    {
        private readonly MarketHelper _marketHelper;
        private readonly ICorporationManager _corporationManager;
        private readonly SparkTeleportHelper _sparkTeleportHelper;
        private readonly PBSStandingController<PBSDockingBase> _standingController;
        protected readonly PBSObjectHelper<PBSDockingBase> _pbsObjectHelper;
        private readonly PBSReinforceHandler<PBSDockingBase> _pbsReinforceHandler;
        private readonly PBSTerritorialVisibilityHelper _pbsTerritorialVisibilityHelper;

        public PBSDockingBase(MarketHelper marketHelper,ICorporationManager corporationManager,IChannelManager channelManager,ICentralBank centralBank,IRobotTemplateRelations robotTemplateRelations,DockingBaseHelper dockingBaseHelper,SparkTeleportHelper sparkTeleportHelper,PBSObjectHelper<PBSDockingBase>.Factory pbsObjectHelperFactory) : base(channelManager,centralBank,robotTemplateRelations,dockingBaseHelper)
        {
            _marketHelper = marketHelper;
            _corporationManager = corporationManager;
            _sparkTeleportHelper = sparkTeleportHelper;
            _pbsObjectHelper = pbsObjectHelperFactory(this);
            _pbsReinforceHandler = new PBSReinforceHandler<PBSDockingBase>(this);
            _standingController = new PBSStandingController<PBSDockingBase>(this);
            _pbsTerritorialVisibilityHelper = new PBSTerritorialVisibilityHelper(this);
        }

        public IPBSReinforceHandler ReinforceHandler => _pbsReinforceHandler;
        public IPBSConnectionHandler ConnectionHandler => _pbsObjectHelper.ConnectionHandler;

        public int ZoneIdCached { get; private set; }

        public ErrorCodes ModifyConstructionLevel(int amount, bool force = false)
        {
            return _pbsObjectHelper.ModifyConstructionLevel(amount, force);
        }

        public int ConstructionLevelMax => _pbsObjectHelper.ConstructionLevelMax;
        public int ConstructionLevelCurrent => _pbsObjectHelper.ConstructionLevelCurrent;

        public bool IsOrphaned
        {
            get => _pbsObjectHelper.IsOrphaned;
            set => _pbsObjectHelper.IsOrphaned = value;
        }

        public event Action<Unit,bool> OrphanedStateChanged
        {
            add => _pbsObjectHelper.OrphanedStateChanged += value;
            remove => _pbsObjectHelper.OrphanedStateChanged -= value;
        }

        public void SendNodeUpdate(PBSEventType eventType = PBSEventType.nodeUpdate)
        {
            _pbsObjectHelper.SendNodeUpdate(eventType);
        }

        public bool IsLootGenerating { get; set; }

        public double StandingLimit
        {
            get { return _standingController.StandingLimit; }
            set { _standingController.StandingLimit = value; }
        }

        public bool StandingEnabled
        {
            get { return _standingController.Enabled; }
            set { _standingController.Enabled = value; }
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override ErrorCodes IsAttackable
        {
            get
            {
                //no reinforce
                if (_pbsReinforceHandler.CurrentState.IsReinforced) 
                    return ErrorCodes.TargetIsNonAttackable_Reinforced;

                //no connections
                //true if only production stuff is connected ONLY
                //anything else -> false -> kill that node first

                var anyControlTower = _pbsObjectHelper.ConnectionHandler.GetConnections().Any(c => c.TargetPbsObject is PBSControlTower);

                if (anyControlTower)
                    return ErrorCodes.TargetIsNonAttackable_ControlTowerConnected;

                return ErrorCodes.NoError; //itt tilos a base-t meghivni, mert az mar docking base
            }
        }


        public override bool IsLockable
        {
            get { return true; }
        }

        public PBSDockingBaseVisibility DockingBaseMapVisibility
        {
            get { return _pbsTerritorialVisibilityHelper.DockingBaseMapVisibility(); }
            set { _pbsTerritorialVisibilityHelper.SetDockingBaseVisibleOnMap(value); } 
        }


        public PBSDockingBaseVisibility NetworkMapVisibility
        {
            get { return _pbsTerritorialVisibilityHelper.NetworkMapVisibility(); }
            set { _pbsTerritorialVisibilityHelper.SetNetworkVisibleOnTerritoryMap(value); }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            _pbsObjectHelper.Init();
            _pbsReinforceHandler.Init();
            _pbsTerritorialVisibilityHelper.Init();

            ZoneIdCached = zone.Id; //OPP: make sure this is set!

            //OPP: The following line is commented because it broke the BaseListFacilities command.
            // ClearChildren(); //ez azert kell, hogy a zonan ne legyenek gyerekei semmikepp
            Parent = 0; //ez azert kell, hogy a bazison levo kontenerek megtalaljak, mint root
            base.OnEnterZone(zone, enterType);
        }

        public override void OnLoadFromDb()
        {
            base.OnLoadFromDb();
            
            _pbsObjectHelper.Init();

            _pbsTerritorialVisibilityHelper.Init();
        }


        public override void OnInsertToDb()
        {
            _pbsObjectHelper.Init();

            DynamicProperties.Update(k.creation,DateTime.Now);
            
            base.OnInsertToDb();

            var market = GetMarket();
            _marketHelper.InsertGammaPlasmaOrders(market);
            
            Logger.Info("A new PBSDockingbase is created " + this);

        }

        public void OnDockingBaseDeployed()
        {
            PBSHelper.SendPBSDockingBaseCreatedToProduction(Eid);
            ChannelManager.CreateChannel(ChannelType.Station,ChannelName);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            var zone = Zone;

            if (zone != null)
            {
                _pbsObjectHelper.AddToDictionary(info);
                _pbsReinforceHandler.AddToDictionary(info);
                _pbsTerritorialVisibilityHelper.AddToDictionary(info);
                info.Add(k.bandwidthLoad, GetBandwithLoad());
            }

            return info;
        }


        public override IDictionary<string, object> GetDebugInfo()
        {
            //var info = base.GetDebugInfo();
            var info = this.GetMiniDebugInfo();

            _pbsObjectHelper.AddToDictionary(info);
            _pbsReinforceHandler.AddToDictionary(info);
            _pbsTerritorialVisibilityHelper.AddToDictionary(info);

            return info;
        }

        public override void OnUpdateToDb()
        {
            _pbsReinforceHandler.OnSave();
            _pbsObjectHelper.OnSave();
            _pbsTerritorialVisibilityHelper.OnSave();
            base.OnUpdateToDb();
        }

        public override void OnDeleteFromDb()
        {
            //NO BASE CLASS CALL -> szandekos
            Logger.DebugInfo($"[{InfoString}] docking base on delete");
            Logger.DebugInfo($"[{InfoString}] zonaid jo, helperes cucc jon");
            PBSHelper.DeletePBSDockingBase(ZoneIdCached, this).ThrowIfError();
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            Logger.DebugInfo($"[{InfoString}] pbsbase remove from zone");

            _pbsObjectHelper.RemoveFromZone(zone);

            PBSHelper.SendPBSDockingBaseDeleteToProduction(Eid);

            base.OnRemovedFromZone(zone);
        }

        protected override void OnDead(Unit killer)
        {
            IsLootGenerating = true;
            _trashWasKilled = true; //signal trash
            Logger.DebugInfo($"[{InfoString}] loot generating -> true");

            var zone = Zone;
            _pbsObjectHelper.DropLootToZoneFromBase(zone, this, killer);

            base.OnDead(killer);
        }

        public override ErrorCodes IsDockingAllowed(Character issuerCharacter)
        {
            if (!IsFullyConstructed)
                return ErrorCodes.ObjectNotFullyConstructed;

            if (!OnlineStatus)
                return ErrorCodes.NodeOffline;

            if (!StandingEnabled)
                return ErrorCodes.NoError;

            if (!_corporationManager.IsStandingMatch(Owner, issuerCharacter.CorporationEid, StandingLimit))
                return ErrorCodes.StandingTooLowForDocking;

            return ErrorCodes.NoError;
        }

        protected override void DoExplosion()
        {
            //NO base call!!!
        }

        private bool IsFullyConstructed
        {
            get { return _pbsObjectHelper.IsFullyConstructed; }
        }


        public void SetOnlineStatus(bool state, bool checkNofBase, bool forcedByServer = false)
        {
            _pbsObjectHelper.SetOnlineStatus(state,checkNofBase,forcedByServer);
        }

        public void TakeOver(long newOwner)
        {
            _pbsObjectHelper.TakeOver(newOwner);
        }

        public bool OnlineStatus => _pbsObjectHelper.OnlineStatus;

        protected override void OnUpdate(TimeSpan time)
        {
            _pbsReinforceHandler.OnUpdate(time);
            _pbsObjectHelper.OnUpdate(time);

            base.OnUpdate(time);
        }

        private Dictionary<string, object> _cacheTerritoryDictionary;
        private DateTime _lastTDRequest;

        public Dictionary<string, object> GetTerritorialDictionary()
        {
            if (_lastTDRequest == default(DateTime) || DateTime.Now.Subtract(_lastTDRequest).TotalMinutes > 60)
            {
                _lastTDRequest = DateTime.Now.AddMinutes(FastRandom.NextInt(10));

                var ctd = GenerateTerritoryDictionary();
                _cacheTerritoryDictionary = ctd;
            }

            return _cacheTerritoryDictionary;
        }

        private Dictionary<string,object> GenerateTerritoryDictionary()
        {
            var info = new Dictionary<string, object>
                           {
                               {k.corporationEID, Owner},
                               {k.x, CurrentPosition.intX},
                               {k.y, CurrentPosition.intY},
                           };

            var nodes = _pbsObjectHelper.ConnectionHandler.NetworkNodes
                                         .Cast<Unit>()
                                         .ToDictionary("n", unit => 
                                          new Dictionary<string, object>
                                          {
                                            {k.x, unit.CurrentPosition.intX},
                                            {k.y, unit.CurrentPosition.intY},
                                            {k.constructionRadius, unit.GetConstructionRadius()}
                                          });
            info.Add("nodes", nodes);
            return info;
        }

        public ErrorCodes SetDeconstructionRight(Character issuer, bool state)
        {
            this.CheckAccessAndThrowIfFailed(issuer);

            var role = Corporation.GetRoleFromSql(issuer);

            if (!role.IsAnyRole(CorporationRole.CEO,CorporationRole.DeputyCEO))
            {
                return ErrorCodes.InsufficientPrivileges;
            }

            if (!IsFullyConstructed)
            {
                return ErrorCodes.ObjectNotFullyConstructed;
            }
            
            if (state)
            {
                DynamicProperties.Update(k.allowDeconstruction,1);
            }
            else
            {
                DynamicProperties.Remove(k.allowDeconstruction);
            }

            this.Save();

            return ErrorCodes.NoError;
        }

        /// <summary>
        /// If property is present then it's set to deconstruct
        /// </summary>
        /// <returns></returns>
        public virtual ErrorCodes IsDeconstructAllowed()
        {
            
            if (DynamicProperties.Contains(k.allowDeconstruction))
            {
                return ErrorCodes.NoError;
            }

            return ErrorCodes.DockingBaseNotSetToDeconstruct;
        }

        public override double GetOwnerRefundMultiplier(TransactionType transactionType)
        {
            var multiplier = 0.0;
            switch (transactionType)
            {
                case TransactionType.hangarRent:
                case TransactionType.hangarRentAuto:
                    multiplier = 1.0;
                    break;
                case TransactionType.marketFee:
                    multiplier = 1.0;
                    break;
                case TransactionType.ProductionManufacture:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionResearch:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionMultiItemRepair:
                case TransactionType.ItemRepair:
                    multiplier = 0.75;
                    break;
                case TransactionType.ProductionPrototype:
                    multiplier = 0.5;
                    break;
                case TransactionType.ProductionMassProduction:
                    multiplier = 0.5;
                    break;
                case TransactionType.MarketTax:
                    multiplier = 1.0;
                    break;
            }

            return multiplier;
        }

       
        

        private int _bandwidthCapacity;

        public int GetBandwidthCapacity
        {
            get
            {

                if (_bandwidthCapacity <= 0)
                {
                    if (ED.Config.bandwidthCapacity != null)
                    {
                        _bandwidthCapacity = (int)ED.Config.bandwidthCapacity;
                    }
                    else
                    {
                        Logger.Error("no bandwidthCapacity defined for " + this);
                        _bandwidthCapacity = 1000;
                    }

                    
                }

                return _bandwidthCapacity;

            }
        }

        private int GetBandwithLoad()
        {
            return _pbsObjectHelper.ConnectionHandler.NetworkNodes.Where(n => !(n is PBSDockingBase)).Sum(n => n.GetBandwidthUsage());
        }


        public override bool IsOnGammaZone()
        {
            return true;
        }

        public override bool IsVisible(Character character)
        {
            if (DockingBaseMapVisibility == PBSDockingBaseVisibility.open)
                return true;

            Corporation.GetCorporationEidAndRoleFromSql(character, out long corporationEid, out CorporationRole role);
            if (Owner == corporationEid)
            {
                if (DockingBaseMapVisibility == PBSDockingBaseVisibility.corporation)
                {
                    return true;
                }
                else if (DockingBaseMapVisibility == PBSDockingBaseVisibility.hidden)
                {
                    return role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.viewPBS);
                }
            }
            return false;
        }

        private bool _trashWasKilled;
        public void TrashMe()
        {
            var trash = SystemContainer.GetByName("pbs_trash");

            Parent = trash.Eid;

            

            Db.Query().CommandText("INSERT dbo.pbstrash (baseeid, waskilled) VALUES (@baseeid, @waskilled)")
                .SetParameter("@baseeid", Eid)
                .SetParameter("@waskilled", _trashWasKilled)
                .ExecuteNonQuery();

            this.Save();
        }

        public int GetNetworkNodeRange()
        {
            if (ED.Config.network_node_range != null)
                return (int) ED.Config.network_node_range;

            Logger.Error("no network_node_range defined for " + ED.Name );
            return 0;
        }

        public ErrorCodes DoCleanUpWork(int zone)
        {
            var ec = ErrorCodes.NoError;

            Logger.Info(" >>>>>>    docking base SQL DELETE Start ");

            //ezeket elkeszitjuk most mert kesobb nem lesz mar kontenere a base-nek
            var infoHomeBaseCleared = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadHomeBaseCleared);
            var infoBaseDeadWhileDocked = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadWhileDocked);
            var infoBaseDeadWhileOnZone = PBSHelper.GetUpdateDictionary(zone, this, PBSEventType.baseDeadWhileOnZone);

            //---market cleanup
            var market = GetMarketOrThrow();

            var marketOrdersDeleted = Db.Query().CommandText("delete marketitems where marketeid=@marketEID")
                .SetParameter("@marketEID", market.Eid)
                .ExecuteNonQuery();

            Logger.Info(marketOrdersDeleted + " market orders deleted from market: " + market.Eid + " base:" + Eid);

            //---spark teleport cleanup
            _sparkTeleportHelper.DeleteAllSparkTeleports(this);

            //---------------------------------------------

            TrashMe();
            

            //itt lehet pucolni vagy logolni vagy valami

            //itt nem zonazunk, elintezzuk a bedokkolt playereket stb, natur sql

            //plugineknek szolni stb

            //----------ezeknek van beallitva homebasenek a bazis ami meghalt
            var charactersHomeBaseCleared = Db.Query().CommandText("select characterid from characters where homebaseeid=@eid and active=1 and inuse=1")
                .SetParameter("@eid", Eid)
                .Execute()
                .Select(r => Character.Get(r.GetValue<int>(0))).ToArray();

            //clear homebase settings
            var homeBasesCleared = Db.Query().CommandText("update characters set homebaseeid=null where homebaseeid=@eid")
                .SetParameter("@eid", Eid)
                .ExecuteNonQuery();

            Logger.Info(homeBasesCleared + " homebases cleared. for base:" + Eid);
            //------------------------------------------------------



            //clean up insured robots
            var insurancesCleared = Db.Query().CommandText("cleanUpInsuranceByBaseEid")
                .SetParameter("@baseEid", Eid)
                .ExecuteScalar<int>();

            Logger.Info(insurancesCleared + " insurances clear for base: " + Eid);


            //ezek azok akik onnan jottek, vagy epp ott vannak bedokkolva
            var affectedCharacters = GetCharacters();

            var charactersToInform = new List<Tuple<Character, long, bool>>();

            foreach (var affectedCharacter in affectedCharacters)
            {
                var character = affectedCharacter;
                long? revertedBaseEid = null;

                var homeBaseEid = character.HomeBaseEid;
                if (homeBaseEid != Eid)
                {
                    //ezeknek volt beallitva valami homebase, rakjuk oket oda
                    revertedBaseEid = homeBaseEid;
                }

                if (revertedBaseEid == null)
                {
                    //ezeknek nem volt oket faj alapjan deportaljuk
                    revertedBaseEid = DefaultCorporation.GetDockingBaseEid(character);
                }

                Logger.Info("reverted base for characterID:" + character.Id + " base:" + revertedBaseEid);

                //set reverted base
                character.CurrentDockingBaseEid = (long)revertedBaseEid;

                //be van dockolva, aktivchassis ugrott
                var isDocked = character.IsDocked;
                if (isDocked)
                {
                    character.SetActiveRobot(null);
                }

                if (character.IsOnline)
                {
                    //ezalatt kilottek a base-t, informaljuk
                    charactersToInform.Add(new Tuple<Character, long, bool>(character, (long)revertedBaseEid, isDocked));
                }
            }

            Logger.Info(" sql administration for docking base delete is done. " + this);

            Logger.Info(" >>>>>>    docking base SQL DELETE   STOP ");

            Transaction.Current.OnCommited(() =>
            {
                ChannelManager.DeleteChannel(ChannelName);
                PBSHelper.SendBaseDestroyed(infoBaseDeadWhileDocked, infoBaseDeadWhileOnZone, charactersToInform);
                Message.Builder.SetCommand(Commands.PbsEvent).WithData(infoHomeBaseCleared).ToCharacters(charactersHomeBaseCleared).Send();
                SendNodeUpdate(PBSEventType.baseDead);
            });

            return ec;
        }
    }

}
