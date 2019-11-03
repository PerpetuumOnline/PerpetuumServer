using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Services.EventServices;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Services.Looting;
using Perpetuum.Services.Relics;
using Perpetuum.Timers;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Zones.Intrusion
{
    public class Outpost : DockingBase
    {
        private const int EP_WINNER = 120;
        private const int MAX_STABILITY = 150;
        private const int MIN_STABILITY = 0;
        private const int STARTING_STABILITY = 1;
        private const int PRODUCTION_BONUS_THRESHOLD = 100;

        private TimeRange _intrusionWaitTime => IntrusionWaitTime;
        private TimeSpan _timeSinceLastSAP = TimeSpan.Zero;
        private readonly IEntityServices _entityServices;
        private readonly ICorporationManager _corporationManager;
        private readonly ILootService _lootService;
        private static readonly ILookup<long, SAPInfo> _sapInfos;
        private readonly EventListenerService _eventChannel;
        private readonly OutpostDecay _decay;

        public static StabilityBonusThreshold[] StabilityBonusThresholds { get; private set; }
        public static int DefenseNodesStabilityLimit { get; private set; }

        public static DateTimeRange IntrusionPauseTime { get; set; }

        static Outpost()
        {
            StabilityBonusThresholds = Db.Query().CommandText("select * from intrusionsitestabilitythreshold").Execute().Select(r => new StabilityBonusThreshold(r)).ToArray();
            
            _sapInfos = Db.Query().CommandText("select * from intrusionsaps").Execute().ToLookup(r => r.GetValue<long>("siteEid"), r =>
            {
                var x = r.GetValue<int>("x");
                var y = r.GetValue<int>("y");

                return new SAPInfo(EntityDefault.Get(r.GetValue<int>("definition")), new Position(x, y).Center);
            });

            var bonusList = StabilityBonusThresholds.Where(bi => bi.bonusType == StabilityBonusType.DefenseNodes).ToArray();
            DefenseNodesStabilityLimit = bonusList.Length > 0 ? bonusList[0].threshold : 5000;
        }

        public Outpost(IEntityServices entityServices,
                       ICorporationManager corporationManager,
                       IChannelManager channelManager,
                       ILootService lootService,
                       ICentralBank centralBank,
                       IRobotTemplateRelations robotTemplateRelations,
                       EventListenerService eventChannel,
                       DockingBaseHelper dockingBaseHelper) : base(channelManager,centralBank,robotTemplateRelations,dockingBaseHelper)
        {
            _entityServices = entityServices;
            _corporationManager = corporationManager;
            _lootService = lootService;
            _eventChannel = eventChannel;
            _decay = new OutpostDecay(_eventChannel, this);
        }

        public void PublishSAPEvent(StabilityAffectingEvent eventMsg)
        {
            _eventChannel.PublishMessage(eventMsg);
        }

        public TimeRange IntrusionWaitTime { get; set; }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();

            var intrusionInfo = new Dictionary<string, object>
            {
                                        {k.intrusionState, Enabled}
                                    };

            var sapPositions = GetSAPInfoDictionary();
            intrusionInfo.Add(k.sapPositions,sapPositions);

            dictionary.Add(k.intrusion,intrusionInfo);
            return dictionary;
        }

        private IDictionary<string, object> GetSAPInfoDictionary()
        {
            return SAPInfos.ToDictionary("si", sapInfo =>
            {
                var sapdict = new Dictionary<string, object>
                {
                                      {k.definition, sapInfo.EntityDefault.Definition},
                                      {k.x, sapInfo.Position.intX},
                                      {k.y, sapInfo.Position.intY}
                                  };

                return sapdict;
            });
        }

        protected override bool CanCreateEquippedStartRobot
        {
            get { return false; }
        }

        [NotNull]
        public IntrusionSiteInfo GetIntrusionSiteInfo()
        {
            return IntrusionSiteInfo.Get(this);
        }

        private void OnIntrusionSiteInfoUpdated()
        {
            Task.Run(() => RefreshEffectBonus()).LogExceptions();
        }

        private readonly IntervalTimer _timerCheckIntrusionTime = new IntervalTimer(TimeSpan.FromSeconds(30));

        public void AppendSiteInfoToPacket(Packet packet)
        {
            packet.AppendLong(Eid);
            var siteInfo = GetIntrusionSiteInfo();
            var siteOwner = siteInfo.Owner ?? 0L;
            packet.AppendLong(siteOwner);
        }

        private int _checkIntrusionTime;

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _decay.OnUpdate(time);

            if (!Enabled || IntrusionInProgress)
                return;

            _timerCheckIntrusionTime.Update(time);

            if (!_timerCheckIntrusionTime.Passed)
                return;

            _timerCheckIntrusionTime.Reset();

            CheckIntrusionStartTimeAsync();
        }

        private void CheckIntrusionStartTimeAsync()
        {
            if (Interlocked.CompareExchange(ref _checkIntrusionTime, 1, 0) == 1)
                return;

            Task.Run(() => CheckIntrusionStartTime()).ContinueWith(t => _checkIntrusionTime = 0);
        }

        private void CheckIntrusionStartTime()
        {
            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var siteInfo = GetIntrusionSiteInfo();

                    var intrusionStartTime = siteInfo.IntrusionStartTime;
                    if (intrusionStartTime == null)
                    {
                        intrusionStartTime = SelectNextIntrusionStartTime();
                        WriteNextIntrusionStartTimeToDb(intrusionStartTime);
                    }
                    
                    if (DateTime.Now >= intrusionStartTime)
                    {
                        WriteNextIntrusionStartTimeToDb(null);

                        Transaction.Current.OnCommited(() =>
                        {
                            DeploySAP();
                            IntrusionInProgress = true;
                        });
                    }
                    
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        private DateTime SelectNextIntrusionStartTime()
        {
            var waitTime = FastRandom.NextTimeSpan(_intrusionWaitTime);
            var startTime = DateTime.Now + waitTime;

            if (IntrusionPauseTime.IsBetween(startTime))
                startTime += IntrusionPauseTime.Delta;

            return  startTime;
        }

        private void WriteNextIntrusionStartTimeToDb(DateTime? intrusionStartTime)
        {
            Db.Query().CommandText("update intrusionsites set intrusionstarttime = @intrusionStartTime where siteEid = @siteEid")
                .SetParameter("@siteEid", Eid)
                .SetParameter("@intrusionStartTime", intrusionStartTime)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

            Transaction.Current.OnCommited(() =>
            {
                if (intrusionStartTime != null)
                {
                    Logger.Info("Intrusion next SAP deploy time: " + intrusionStartTime);
                }

                OnIntrusionSiteInfoUpdated();
            });
        }

        private void DeploySAP()
        {
            var sapInfo = SAPInfos.RandomElement();
            if (sapInfo == null)
                return;

            var sap = (SAP)_entityServices.Factory.CreateWithRandomEID(sapInfo.EntityDefault);
            sap.Site = this;
            sap.TakeOver += OnSAPTakeOver;
            sap.TimeOut += OnSAPTimeOut;
            sap.AddToZone(Zone, sapInfo.Position);

            const string insertCmd = "insert into intrusionsapdeploylog (siteeid,sapdefinition) values (@siteEid,@sapDefinition)";
            Db.Query().CommandText(insertCmd).SetParameter("@siteEid",Eid).SetParameter("@sapDefinition",sap.Definition).ExecuteNonQuery();

            Logger.Info("Intrusion started. outpost = " + Eid + " sap = " + sap.Eid + " (" + sap.ED.Name + ")");
        }

        public SAPInfo[] SAPInfos => _sapInfos.GetOrEmpty(Eid);

        public override ErrorCodes IsDockingAllowed(Character issuerCharacter)
        {
            var siteInfo = GetIntrusionSiteInfo();

            if (!_corporationManager.IsStandingMatch(siteInfo.Owner ?? 0, issuerCharacter.CorporationEid, siteInfo.DockingStandingLimit))
            {
                return ErrorCodes.StandingTooLowForDocking;
            }

            return base.IsDockingAllowed(issuerCharacter);
        }

        private void RefreshEffectBonus()
        {
            var siteInfo = GetIntrusionSiteInfo();

            var currentEffect = EffectHandler.GetEffectsByCategory(EffectCategory.effcat_intrusion_effect).FirstOrDefault();

            if ( currentEffect != null )
            {
                var threshold = GetEffectBonusStabilityThreshold(currentEffect.Type);

                if ( siteInfo.Stability < threshold )
                {
                    EffectHandler.Remove(currentEffect);
                    Logger.Info($"Intrusion outpost effect removed. outpost = {Eid} effecttype = {currentEffect.Type}");
                }
            }

            var corporationEid = siteInfo.Owner ?? 0L;
            if (corporationEid == 0L)
                return;

            EffectHandler.RemoveEffectsByCategory(EffectCategory.effcat_intrusion_effect);

            if ( siteInfo.ActiveEffect != EffectType.undefined )
            {
                var threshold = GetEffectBonusStabilityThreshold(siteInfo.ActiveEffect);

                if (siteInfo.Stability < threshold)
                    return;

                var builder = NewEffectBuilder().SetType(siteInfo.ActiveEffect).SetOwnerToSource().WithCorporationEid(corporationEid);
                ApplyEffect(builder);
            }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType);
            RefreshEffectBonus();
        }

        private bool _enabled = true;

        public bool Enabled
        {
            private get { return _enabled; }
            set
            {
                _enabled = value;
                if (!_enabled)
                    return;

                IntrusionInProgress = false;
            }
        }

        private void OnSAPTakeOver(SAP sap)
        {
            _decay.OnSAP();
            Task.Run(() => HandleTakeOver(sap)).ContinueWith(t => IntrusionInProgress = false);
        }

        /// <summary>
        /// This function is called when players take over a sap successfully
        /// </summary>
        private void HandleTakeOver(SAP sap)
        {
            Logger.Info($"Intrusion SAP taken. sap = {sap.Eid} {sap.ED.Name} ");

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var gen = new LootGenerator(_lootService.GetIntrusionLootInfos(this, sap));
                    LootContainer.Create().AddLoot(gen).BuildAndAddToZone(Zone, sap.CurrentPosition);
                    processStabilityChange(sap.toStabilityAffectingEvent());
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        /// //////////////////
        /// <summary>
        /// This function is called by external events that affect stability
        /// </summary>
        public void IntrusionEvent(StabilityAffectingEvent sap)
        {
            Logger.Info($"Intrusion Something else... taken.");
            Logger.Info($"this outpost = {this.Eid} {this.ED.Name} ");

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    processStabilityChange(sap);
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
        //

        /// //////////////////
        /// <summary>
        /// Core SAP logic
        /// </summary>
        private void processStabilityChange(StabilityAffectingEvent sap)
        {
            // Check for invalid player-SAPS
            var winnerCorporation = sap.GetWinnerCorporation();
            if (winnerCorporation == null && !sap.IsSystemGenerated())
                return;

            // Check for unowned outposts to not be affected by system-generated events
            var siteInfo = GetIntrusionSiteInfo();
            if (siteInfo.Owner == null && sap.IsSystemGenerated())
                return;

            var oldStability = siteInfo.Stability;
            var newStability = siteInfo.Stability;
            var newOwner = siteInfo.Owner;
            var oldOwner = siteInfo.Owner;

            var logEvent = new IntrusionLogEvent
            {
                OldOwner = siteInfo.Owner,
                NewOwner = siteInfo.Owner,
                SapDefinition = sap.Definition,
                EventType = IntrusionEvents.sapTakeOver,
                WinnerCorporationEid = winnerCorporation.Eid,
                OldStability = oldStability
            };

            if (sap.IsSystemGenerated())
            {
                newStability = (newStability + sap.StabilityChange);
            }
            else if (winnerCorporation is PrivateCorporation)
            {
                //Compare the Owner and Winner corp's relations
                var ownerEid = siteInfo.Owner ?? default(long);
                var ownerAndWinnerGoodRelation = false;

                //Ally relationship threshold
                var friendlyOnly = 10;
                //Ally stability affect
                var allyAffectFactor = 0.0;
                //Compare mutual relation match between corps to determine ally
                ownerAndWinnerGoodRelation = _corporationManager.IsStandingMatch(winnerCorporation.Eid, ownerEid, friendlyOnly);
                ownerAndWinnerGoodRelation = _corporationManager.IsStandingMatch(ownerEid, winnerCorporation.Eid, friendlyOnly) && ownerAndWinnerGoodRelation;

                //Stability increase if winner is owner, 0 increase if ally, else negative
                if (winnerCorporation.Eid == siteInfo.Owner || sap.OverrideRelations)
                {
                    newStability = (newStability + sap.StabilityChange);
                }
                else if (ownerAndWinnerGoodRelation)
                {
                    newStability = (newStability + (int)(sap.StabilityChange * allyAffectFactor));
                }
                else
                {
                    newStability = (newStability - sap.StabilityChange);
                }
            }

            if (siteInfo.Owner == null && winnerCorporation is PrivateCorporation)
            {
                // No owner - winner gets outpost, for any SAP event
                logEvent.EventType = IntrusionEvents.siteOwnershipGain;
                newOwner = winnerCorporation.Eid;
                newStability = STARTING_STABILITY;
            }
            else if (newStability <= 0)
            {
                // Outpost has owner, but new stability hit 0 = owner loses station
                logEvent.EventType = IntrusionEvents.siteOwnershipLost;
                newOwner = null;
            }

            newStability = newStability.Clamp(MIN_STABILITY, MAX_STABILITY);

            //set the resulting values
            SetIntrusionOwnerAndPoints(newOwner, newStability);
            ReactStabilityChanges(siteInfo, oldStability, newStability, newOwner, oldOwner);
            logEvent.NewOwner = newOwner;
            logEvent.NewStability = newStability;
            InsertIntrusionLog(logEvent);

            //Award EP
            foreach (var player in sap.GetPlayers())
            {
                player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Intrusion, EP_WINNER);
            }

            //make dem toast anyways
            Transaction.Current.OnCommited(() =>
            {
                if (oldStability != newStability)
                {
                    OnIntrusionSiteInfoUpdated();
                    InformAllPlayers();
                }
                if (!sap.IsSystemGenerated())
                {
                    InformPlayersOnZone(Commands.ZoneSapActivityEnd, new Dictionary<string, object>
                        {
                            {k.siteEID, Eid},
                            {k.eventType, (int) logEvent.EventType},
                            {k.eid, sap.Eid},
                            {k.winner, winnerCorporation.Eid},
                        });
                }
            });
        }


        private void OnSAPTimeOut(SAP sap)
        {
            IntrusionInProgress = false;
        }

        public bool IntrusionInProgress { get; private set; }

        private readonly Dictionary<TransactionType,double> _refundMultipliers = new Dictionary<TransactionType, double>
        {
            {TransactionType.hangarRent, 0.25},
            {TransactionType.hangarRentAuto, 0.25},
            {TransactionType.marketFee, 0.5},
            {TransactionType.MarketTax, 0.5},
            {TransactionType.ProductionManufacture, 0.25},
            {TransactionType.ProductionResearch, 0.25},
            {TransactionType.ProductionMultiItemRepair, 0.35},
            {TransactionType.ItemRepair, 0.35},
            {TransactionType.ProductionPrototype, 0.25},
            {TransactionType.ProductionMassProduction, 0.25},
        };

        public override double GetOwnerRefundMultiplier(TransactionType transactionType)
        {
            return _refundMultipliers.GetOrDefault(transactionType);
        }

        protected override void OnEffectChanged(Effect effect, bool apply)
        {
            base.OnEffectChanged(effect,apply);
            Logger.Info($"Outpost effect changed ({Eid} = {ED.Name}). type:{effect.Type} apply:{apply}");
        }

        private void InformPlayersOnZone(Command command, Dictionary<string, object> data)
        {
            Zone.SendMessageToPlayers(Message.Builder.SetCommand(command).WithData(data));
        }

        /// <summary>
        /// This function handles the stability and owner changes of an intrusion site
        /// </summary>
        private void ReactStabilityChanges(IntrusionSiteInfo siteInfo, int oldStability, int newStability, long? newOwner, long? oldOwner)
        {
            if (oldStability == newStability) 
                return;
            
            if (oldStability > newStability)
            {
                //stability loss

                //DOCKING RIGHTS 
                var dockingRightsStabilityLimit = GetDockingRightsStabilityLimit();
                if (newStability < dockingRightsStabilityLimit)
                {
                    if (siteInfo.DockingStandingLimit != null)
                    {
                        //clear docking rights
                        SetDockingControlDetails(null, true);
                        InsertDockingRightsLog(null, null, siteInfo.Owner, IntrusionEvents.dockingRightsClearedByServer);
                    }
                }

                //AURA EFFECT
                if (siteInfo.ActiveEffect != EffectType.undefined)
                {
                    var effectStabilityThreshold = GetEffectBonusStabilityThreshold(siteInfo.ActiveEffect);
                    if (effectStabilityThreshold >= 0)
                    {
                        if (effectStabilityThreshold > newStability)
                        {
                            //logoljuk
                            InsertIntrusionEffectLog(null, null, siteInfo.Owner, IntrusionEvents.effectClearedByServer);

                            Db.Query().CommandText("update intrusionsites set activeeffectid = NULL where siteeid = @siteEid")
                                   .SetParameter("@siteEid", Eid)
                                   .ExecuteNonQuery();
                        }
                    }
                }

                ProductionStabilityLoss(newStability,oldStability,newOwner,oldOwner);
            }
            else
            {
                //stability gain
                ProductionStabilityGain(newStability,oldStability,newOwner);
            }
        }

        /// <summary>
        /// Handles the stability gain for production
        /// </summary>
        private void ProductionStabilityGain(int newStability, int oldStability, long? newOwner)
        {
            if (oldStability > PRODUCTION_BONUS_THRESHOLD)
                return; //Do nothing if old stability > 100

            var siteInfo = GetIntrusionSiteInfo();
            var oldProductionPoints = (int) (oldStability/10.0);
            var newProductionPoints = (int) (newStability/10.0);

            if (oldProductionPoints == newProductionPoints) 
                return;

            var pointsToIncrease = newProductionPoints - oldProductionPoints;

            Logger.Info($"intrusion production points gain: {pointsToIncrease} site: {Eid}");

            if (newOwner == null) 
                return;

            var currentPoints = siteInfo.ProductionPoints;
            var spentPoints = GetFacilityPointsSpent();
            
            currentPoints = (pointsToIncrease + currentPoints).Clamp(0, 10);
            currentPoints = (currentPoints - spentPoints).Clamp(0, 10);

            Logger.Info("intrusion new stability points:" + currentPoints + " site: " + Eid);
            SetProductionPoints(currentPoints);
        }

        /// <summary>
        /// Handles the stability loss for production
        /// </summary>
        private void ProductionStabilityLoss(int newStability, int oldStability, long? newOwner, long? oldOwner)
        {
            if (newStability > PRODUCTION_BONUS_THRESHOLD)
                return; //Do nothing if new stability > 100

            var siteInfo = GetIntrusionSiteInfo();
            var oldProductionPoints = (int)(oldStability / 10.0);
            var newProductionPoints = (int)(newStability / 10.0);

            if (oldProductionPoints == newProductionPoints) 
                return;

            var pointsToDecrease = oldProductionPoints - newProductionPoints;

            Logger.Info($"intrusion production points loss: {pointsToDecrease} site: {Eid}");

            if (newOwner == null && oldOwner != null)
            {
                //site lost
                SetProductionPoints(0);
                CleanUpIntrusionProductionStack(oldOwner);
            }
            else if (newOwner != null)
            {
                var currentPoints = siteInfo.ProductionPoints;
                var oldPoints = currentPoints;

                if (currentPoints >= pointsToDecrease)
                {
                    //there was enough points in the pool
                    currentPoints = (currentPoints - pointsToDecrease).Clamp(0, 10);
                    
                    SetProductionPoints(currentPoints);

                    if (oldPoints != currentPoints)
                    {
                        InsertProductionLog(IntrusionEvents.productionPointsDegradeByServer, null, null, null, null, currentPoints, oldPoints, newOwner);
                    }
                }
                else
                {
                    //not enough points in the pool
                    pointsToDecrease = (pointsToDecrease - currentPoints).Clamp(0,10);
                    SetProductionPoints(0);

                    DegradeIntrusionProductionStack(pointsToDecrease, newOwner);

                    if (oldPoints != 0)
                    {
                        InsertProductionLog(IntrusionEvents.productionPointsDegradeByServer, null, null, null, null, 0, oldPoints, newOwner);
                    }
                }
            }
        }

        public const int SETEFFECT_CHANGE_COOLDOWN_MINUTES = 60 * 24;


        public void SetEffectBonus(EffectType effectType,Character issuer)
        {
            var siteInfo = GetIntrusionSiteInfo();

            siteInfo.Owner.ThrowIfNotEqual(issuer.CorporationEid,ErrorCodes.InsufficientPrivileges);

            var setEffectControlTime = siteInfo.SetEffectControlTime ?? default(DateTime);

            DateTime.Now.ThrowIfLess(setEffectControlTime,ErrorCodes.SetEffectChangeCooldownInProgress);

            var role = Corporation.GetRoleFromSql(issuer);
            role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

            var eventType = IntrusionEvents.effectClear;
            if (effectType != EffectType.undefined)
            {
                eventType = IntrusionEvents.effectSet;
                var currentStability = siteInfo.Stability;
                var newEffectStabilityThreshold = GetEffectBonusStabilityThreshold(effectType);

                newEffectStabilityThreshold.ThrowIfLess(0,ErrorCodes.IntrusionSiteEffectBonusNotFound);
                newEffectStabilityThreshold.ThrowIfGreater(currentStability,ErrorCodes.StabilityTooLow);
            }

            setEffectControlTime = DateTime.Now.AddMinutes(SETEFFECT_CHANGE_COOLDOWN_MINUTES);

            Db.Query().CommandText("update intrusionsites set activeeffectid = @effectType,seteffectcontroltime = @setEffectControlTime where siteeid = @siteEid")
                .SetParameter("@siteEid",Eid)
                .SetParameter("@effectType",(int) effectType)
                .SetParameter("@setEffectControlTime",setEffectControlTime)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);

            InsertIntrusionEffectLog(issuer.Id, (int)effectType, siteInfo.Owner,eventType);

            Transaction.Current.OnCommited(OnIntrusionSiteInfoUpdated);
        }

        private static int GetEffectBonusStabilityThreshold(EffectType effectType)
        {
            var thresholdInfo = StabilityBonusThresholds.OrderBy(t => t.threshold).FirstOrDefault(t => t.effectType == effectType);

            if ( thresholdInfo == null )
                return -1;

            return thresholdInfo.threshold;
        }

        private void SetIntrusionOwnerAndPoints(long? newOwner, int stability)
        {
            Db.Query().CommandText("update intrusionsites set owner=@newOwner, stability=@startStability where siteeid=@siteEID")
                .SetParameter("@siteEID",Eid)
                .SetParameter("@newOwner", newOwner)
                .SetParameter("@startStability", stability)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public void SetDefenseStandingLimit(double? standingLimit)
        {
            Db.Query().CommandText("update intrusionsites set defensestandinglimit=@standing where siteeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@standing", standingLimit)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        private void InformAllPlayers()
        {
            SendSiteInfoToOnlineCharacters();
        }

        public class IntrusionLogEvent : ILogEvent
        {
            public IntrusionEvents EventType { get; set; }
            public long? NewOwner { get; set; }
            public long? OldOwner { get; set; }
            public int NewStability { get; set; }
            public int OldStability { get; set; }
            public int SapDefinition { get; set; }
            public long? WinnerCorporationEid { get; set; }
        }

        private void InsertIntrusionLog(long? owner, int newStability, long? winnerCorporationEid, int sapDefinition, int oldStability, long? oldOwner, IntrusionEvents intrusionEvents)
        {
            const string insertStr = @"insert intrusionsitelog (siteeid,owner,stability,winnercorporationeid,sapdefinition,oldstability,oldowner,eventtype) 
                                       values (@siteEID,@owner,@stability,@winnerCorporationEid,@sapDefinition,@oldStability,@oldOwner,@eventType)";
            Db.Query().CommandText(insertStr)
                .SetParameter("@siteEID",Eid)
                .SetParameter("@owner", owner)
                .SetParameter("@stability", newStability)
                .SetParameter("@oldStability", oldStability)
                .SetParameter("@winnerCorporationEid", winnerCorporationEid)
                .SetParameter("@sapDefinition", sapDefinition)
                .SetParameter("@oldOwner", oldOwner)
                .SetParameter("@eventType", intrusionEvents)
                .ExecuteNonQuery();
        }

        private void InsertIntrusionLog(IntrusionLogEvent e)
        {
            const string insertStr = @"insert intrusionsitelog (siteeid,owner,stability,winnercorporationeid,sapdefinition,oldstability,oldowner,eventtype) 
                                       values (@siteEID,@owner,@stability,@winnerCorporationEid,@sapDefinition,@oldStability,@oldOwner,@eventType)";
            Db.Query().CommandText(insertStr)
                .SetParameter("@siteEID",Eid)
                .SetParameter("@eventType", e.EventType)
                .SetParameter("@owner", e.NewOwner)
                .SetParameter("@oldOwner", e.OldOwner)
                .SetParameter("@stability", e.NewStability)
                .SetParameter("@oldStability",e.OldStability)
                .SetParameter("@sapDefinition", e.SapDefinition)
                .SetParameter("@winnerCorporationEid", e.WinnerCorporationEid)
                .ExecuteNonQuery();
        }

        private void InsertIntrusionEffectLog(int? characterId, int? effectId, long? owner,IntrusionEvents intrusionEvents)
        {
            Db.Query().CommandText("insert intrusioneffectlog (siteeid,characterid,effectid,owner,eventtype) values (@siteEID,@characterID,@effectID,@owner,@eventType)")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@characterID", characterId)
                .SetParameter("@effectID", effectId)
                .SetParameter("@owner", owner)
                .SetParameter("@eventType", intrusionEvents)
                .ExecuteNonQuery();
        }




        public const int DOCKINGRIGHTS_CHANGE_COOLDOWN_MINUTES = 60 * 24;

        public void SetDockingControlDetails(double? dockingStandingLimit, bool clearDockingControltime = false)
        {
            DateTime? dockingControlTime = DateTime.Now.AddMinutes(DOCKINGRIGHTS_CHANGE_COOLDOWN_MINUTES);

            if (clearDockingControltime)
                dockingControlTime = null;

            Db.Query().CommandText("update intrusionsites set dockingstandinglimit=@dockingStandingLimit, dockingcontroltime=@now where siteeid=@siteEID")
                .SetParameter("@siteEID",Eid)
                .SetParameter("@dockingStandingLimit", dockingStandingLimit)
                .SetParameter("@now", dockingControlTime)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void InsertDockingRightsLog(Character character, double? dockingStandingLimit, long? owner, IntrusionEvents intrusionEvents)
        {
            Db.Query().CommandText("insert intrusiondockingrightslog (characterid,siteeid,dockingstandinglimit,owner,eventtype) values (@characterID,@siteEID,@dockingStandingLimit,@owner,@eventType)")
                    .SetParameter("@characterID", character?.Id)
                    .SetParameter("@siteEID",Eid)
                    .SetParameter("@dockingStandingLimit", dockingStandingLimit)
                    .SetParameter("@owner", owner)
                    .SetParameter("@eventType", intrusionEvents)
                    .ExecuteNonQuery();
        }

        public void InsertProductionLog(IntrusionEvents eventType, int? facilityDefinition, int? facilityLevel, int? oldFacilityLevel,Character character, int? points, int? oldPoints, long? owner)
        {
            const string insertStr = @"insert intrusionproductionlog (siteeid,eventtype,facilitydefinition,facilitylevel,oldfacilitylevel,characterid,points,oldpoints,owner) 
                                       values (@siteeid,@eventtype,@facilitydefinition,@facilitylevel,@oldfacilitylevel,@characterid,@points,@oldpoints,@owner)";

            Db.Query().CommandText(insertStr)
                .SetParameter("@siteeid", Eid)
                .SetParameter("@eventtype", (int) eventType)
                .SetParameter("@facilitydefinition", facilityDefinition)
                .SetParameter("@facilitylevel", facilityLevel)
                .SetParameter("@oldfacilitylevel", oldFacilityLevel)
                .SetParameter("@characterid", character?.Id)
                .SetParameter("@points", points)
                .SetParameter("@oldpoints", oldPoints)
                .SetParameter("@owner", owner)
                .ExecuteNonQuery();
        }

        public void InsertIntrusionSiteMessageLog(Character character, string message, long? owner, IntrusionEvents intrusionEvents)
        {
            Db.Query().CommandText("insert intrusionsitemessagelog (siteeid,owner,characterid,message,eventtype) values (@siteEID,@owner,@characterID,@message,@eventType)")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@owner", owner)
                .SetParameter("@characterID", character.Id)
                .SetParameter("@message", message)
                .SetParameter("@eventType", intrusionEvents)
                .ExecuteNonQuery();
        }

        public int GetIntrusionSiteStability()
        {
            var stability = Db.Query().CommandText("select stability from intrusionsites where siteeid = @siteEid")
                                   .SetParameter("@siteEid",Eid)
                                   .ExecuteScalar<int>();
            return stability;
        }

        public void UpgradeFacility(long facilityEid)
        {
            Db.Query().CommandText("insert intrusionproductionstack (siteeid,facilityeid) values (@siteEID,@facilityEID)")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@facilityEID", facilityEid)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public void SetProductionPoints(int points)
        {
            Db.Query().CommandText("update intrusionsites set productionpoints=@points where siteeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@points", points)
                .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
        }

        public void SetSiteMessage(string message)
        {
            Db.Query().CommandText("update intrusionsites set message=@message where siteeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .SetParameter("@message", message)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void ClearSiteMessage()
        {
            Db.Query().CommandText("update intrusionsites set message=NULL where siteeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public static int GetDockingRightsStabilityLimit()
        {
            var bonusList = StabilityBonusThresholds.Where(bi => bi.bonusType == StabilityBonusType.DockingRights).ToArray();

            if (bonusList.Length == 0)
            {
                Logger.Error("no docking rights level is defined. falling back to 5000.");
                return 5000;
            }

            if (bonusList.Length != 1)
            {
                Debug.Assert(false, "more than one DockingRights threshold is defined. ");
            }
            
            return bonusList.First().threshold;
        }

        public const int MAXIMUM_PRODUCTION_POINT_INDICES = 3; //maximum level of production facility

        /// <summary>
        /// Clears the production point stack if the site loses its owner
        /// </summary>
        private void CleanUpIntrusionProductionStack(long? owner)
        {
            Logger.Info($"cleaning up intrusion production stack for site: {Eid}");

            //do the log
            var facilityEids =
                Db.Query().CommandText("select facilityeid from intrusionproductionstack where siteeid=@siteEID")
                    .SetParameter("@siteEID", Eid)
                    .Execute()
                    .Select(r => r.GetValue<long>(0))
                    .ToArray();

            foreach (var facilityEid in facilityEids)
            {
                var facilityEntityDefault = EntityDefault.GetByEid(facilityEid);
                var facilityLevel = GetFacilityLevelFromStack(facilityEid);

                InsertProductionLog(IntrusionEvents.productionFacilityDegradedByServer, facilityEntityDefault.Definition, 1, facilityLevel + 1, null, null, null, owner);
            }

            //delete all
            Db.Query().CommandText("delete intrusionproductionstack where siteeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .ExecuteNonQuery();
        }

        /// <summary>
        /// Degrades the production stack, occures when a site loses stability points
        /// </summary>
        private void DegradeIntrusionProductionStack(int pointsToDegrade, long? owner)
        {
            Logger.Info($"degrading intrusion production stack with {pointsToDegrade} on site: {Eid}");

            var queryStr = $"select top {pointsToDegrade} id from intrusionproductionstack where siteeid=@siteEID order by eventtime desc";
            var indices =
                Db.Query().CommandText(queryStr).SetParameter("@siteEID", Eid)
                    .Execute()
                    .Select(r => r.GetValue<int>(0)).ToArray();


            foreach (var index in indices)
            {
                var facilityEid = Db.Query().CommandText("select facilityeid from intrusionproductionstack where id=@ID")
                    .SetParameter("@ID", index)
                    .ExecuteScalar<long>();

                var facilityEntityDefault = EntityDefault.GetByEid(facilityEid);
                var facilityLevel = GetFacilityLevelFromStack(facilityEid);

                InsertProductionLog(IntrusionEvents.productionFacilityDegradedByServer, facilityEntityDefault.Definition, facilityLevel, facilityLevel+1 , null, null, null, owner);

                Db.Query().CommandText("delete intrusionproductionstack where id=@ID")
                    .SetParameter("@ID", index)
                    .ExecuteNonQuery();
            }

            Logger.Info($"{indices.Length} amount of entries were removed from intrusion production stack for site: {Eid}");
        }

        public static int GetFacilityLevelFromStack(long facilityEid)
        {
            var level= Db.Query().CommandText("select count(*) from intrusionproductionstack where facilityeid=@facilityEID")
                .SetParameter("@facilityEID", facilityEid)
                .ExecuteScalar<int>();

            return level.Clamp(0, MAXIMUM_PRODUCTION_POINT_INDICES);
        }

        private int GetFacilityPointsSpent()
        {
            var spentPoints = Db.Query().CommandText("select count(*) from intrusionproductionstack where facilityeid=@siteEID")
                .SetParameter("@siteEID", Eid)
                .ExecuteScalar<int>();

            return spentPoints;
        }

        private const string INTRUSIONSITE_INFO_SELECT = "select owner, stability, dockingstandinglimit, dockingcontroltime, siteeid, message from intrusionsites where enabled=1 ";
        private const string INTRUSIONSITE_PRIVATE_INFO_SELECT = "select id,siteeid,owner,enabled,stability,dockingstandinglimit,dockingcontroltime,seteffectcontroltime,activeeffectid,message,productionpoints,defensestandinglimit from intrusionsites where enabled=1 and owner=@corporationEID";

        public static Dictionary<string, object> GetOwnershipInfo()
        {
            var counter = 0;
            var dict = new Dictionary<string, object>();

            foreach (var record in Db.Query().CommandText(INTRUSIONSITE_INFO_SELECT).Execute())
            {
                dict.Add("s"+counter++, record.RecordToDictionary());
            }
                    
            return dict;
        }

        public static Dictionary<string, object> GetOwnershipPrivateInfo(long corporationEid)
        {
            var counter = 0;
            var dict = new Dictionary<string, object>();

            foreach (var record in Db.Query().CommandText(INTRUSIONSITE_PRIVATE_INFO_SELECT).SetParameter("@corporationEid", corporationEid).Execute())
            {
                dict.Add("t" + counter++, record.RecordToDictionary());
            }

            return dict;
        }

        public void SendSiteInfoToOnlineCharacters()
        {
            Task.Run(() => Message.Builder.SetCommand(Commands.BaseGetOwnershipInfo)
                .SetData(k.data,GetInfoDictionary()).ToOnlineCharacters()
                .Send());
        }

        private Dictionary<string, object> GetInfoDictionary()
        {
            var counter = 0;
            var dict = new Dictionary<string, object>();

            var records = Db.Query().CommandText(INTRUSIONSITE_INFO_SELECT + "and siteeid=@siteEID")
                                 .SetParameter("@siteEID", Eid)
                                 .Execute();

            foreach (var record in records)
            {
                dict.Add("s" + counter++, record.RecordToDictionary());
            }

            return dict;
        }
        public const int INTRUSION_LOGS_LENGTH = 7;

        public IDictionary<string, object> GetIntrusionProductionLog(int offsetInDays, long corporationEid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT siteeid,eventtype,facilitydefinition,facilitylevel,oldfacilitylevel,characterid,points,oldpoints,eventtime
                                    FROM  intrusionproductionlog
                                    WHERE eventtime between @earlier AND @later and siteeid=@siteeid and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@later",later)
                .SetParameter("@earlier",earlier)
                .SetParameter("@corporationEid",corporationEid)
                .SetParameter("@siteEid",Eid).Execute().RecordsToDictionary("pr");
            return result;
        }

        public IDictionary<string, object> GetIntrusionStabilityLog(int daysBack)
        {
            var later = DateTime.Now.AddDays(-daysBack);
            
            const string sqlCmd = @"SELECT stability,eventtime
                                    FROM  intrusionsitelog
                                    WHERE eventtime>@later and siteeid=@siteeid";

            var result = Db.Query().CommandText(sqlCmd)
                                .SetParameter("@later",later)
                                .SetParameter("@siteeid",Eid).Execute().RecordsToDictionary("g");

            return result;
        }

        public static IDictionary<string, object> GetIntrusionStabilityPublicLog(int offsetInDays)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT siteeid,stability,eventtime,owner,winnercorporationeid,oldstability,sapdefinition,eventtype,oldowner
                                    FROM  intrusionsitelog
                                    WHERE eventtime between @earlier AND @later";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@later",later)
                .SetParameter("@earlier",earlier).Execute().RecordsToDictionary("f");
            return result;
        }

        public IDictionary<string, object> GetDockingRightsLog(int offsetInDays, long corporationeid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT characterid,siteeid,dockingstandinglimit,eventtime,eventtype
                                    FROM  intrusiondockingrightslog
                                    WHERE eventtime between @earlier AND @later and siteeid=@siteeid and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .SetParameter("@siteeid",Eid)
                .SetParameter("@corporationeid",corporationeid).Execute().RecordsToDictionary("a");
            return result;

        }

        public IDictionary<string, object> GetMessageChangeLog(int offsetInDays, long corporationeid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT characterid,siteeid,eventtime,eventtype
                                    FROM  intrusionsitemessagelog
                                    WHERE eventtime between @earlier AND @later and siteeid=@siteeid and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .SetParameter("@siteeid",Eid)
                .SetParameter("@corporationeid",corporationeid).Execute().RecordsToDictionary("b");
            return result;

        }

        public IDictionary<string, object> GetIntrusionCorporationLog(int offsetInDays, long corporationeid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT siteeid,owner,stability,eventtime,winnercorporationeid,sapdefinition,oldstability,oldowner,eventtype
                                    FROM  intrusionsitelog
                                    WHERE eventtime between @earlier AND @later and siteeid=@siteeid and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .SetParameter("@siteeid",Eid)
                .SetParameter("@corporationeid",corporationeid).Execute().RecordsToDictionary("c");
            return result;

        }

        public IDictionary<string, object> GetIntrusionEffectLog(int offsetInDays, long corporationeid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT siteeid,characterid,eventtime,effectid,eventtype
                                    FROM intrusioneffectlog
                                    WHERE eventtime between @earlier AND @later and siteeid=@siteeid and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                .SetParameter("@earlier",earlier)
                .SetParameter("@later",later)
                .SetParameter("@siteeid",Eid)
                .SetParameter("@corporationeid",corporationeid).Execute().RecordsToDictionary("d");
            return result;
        }

        [CanBeNull]
        public Corporation GetSiteOwner()
        {
            var info = GetIntrusionSiteInfo();
            return Corporation.Get(info.Owner ?? 0L);
        }
    }
}