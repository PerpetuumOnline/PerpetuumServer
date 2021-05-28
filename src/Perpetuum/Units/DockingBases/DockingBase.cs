using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.Channels;
using Perpetuum.Services.ItemShop;
using Perpetuum.Services.MarketEngine;
using Perpetuum.Services.ProductionEngine.Facilities;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.Training;

namespace Perpetuum.Units.DockingBases
{
    public class UndockSpawnPositionSelector : IEntityVisitor<DockingBase>,IEntityVisitor<TrainingDockingBase>
    {
        private Position _spawnPosition;

        public static Position SelectSpawnPosition(DockingBase dockingBase)
        {
            var selector = new UndockSpawnPositionSelector();
            dockingBase.AcceptVisitor(selector);
            return selector._spawnPosition.Center;
        }

        public void Visit(DockingBase dockingBase)
        {
            var minRange = dockingBase.Size;
            var maxRange = minRange + dockingBase.SpawnRange;

            var radius = FastRandom.NextInt(minRange, maxRange);
            var angle = FastRandom.NextDouble();

            _spawnPosition = dockingBase.CurrentPosition.OffsetInDirection(angle, radius);
        }

        public void Visit(TrainingDockingBase dockingBase)
        {
            _spawnPosition = dockingBase.SpawnPosition.GetRandomPositionInRange2D(0, dockingBase.SpawnRange);
        }
    }


    public class DockingBase : Unit
    {
        private readonly ICentralBank _centralBank;
        private readonly IRobotTemplateRelations _robotTemplateRelations;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public DockingBase(IChannelManager channelManager,ICentralBank centralBank,IRobotTemplateRelations robotTemplateRelations,DockingBaseHelper dockingBaseHelper)
        {
            ChannelManager = channelManager;
            _centralBank = centralBank;
            _robotTemplateRelations = robotTemplateRelations;
            _dockingBaseHelper = dockingBaseHelper;
        }

        protected IChannelManager ChannelManager { get; }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override ErrorCodes IsAttackable => ErrorCodes.TargetIsNonAttackable;

        public override bool IsLockable => false;

        private string WelcomeMessage => DynamicProperties.GetOrAdd<string>(k.welcome);

        public Position SpawnPosition => ED.Options.SpawnPosition;

        public int SpawnRange => ED.Options.SpawnRange;

        public int Size => ED.Options.Size;

        private int DockingRange => ED.Options.DockingRange;

        public bool IsInDockingRange(Player player)
        {
            return IsInRangeOf3D(player, DockingRange);
        }

        public virtual ErrorCodes IsDockingAllowed(Character issuerCharacter)
        {
            return ErrorCodes.NoError;
        }

        public override void OnDeleteFromDb()
        {
            Zone.UnitService.RemoveDefaultUnit(this,false);
            base.OnDeleteFromDb();
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var baseDict = base.ToDictionary();

            baseDict.Add(k.dockRange, DockingRange);
            baseDict.Add(k.welcome, WelcomeMessage);

            try
            {
                var publicContainerInfo = GetPublicContainer().ToDictionary();
                publicContainerInfo.Add(k.noItemsSent, 1);
                baseDict.Add(k.publicContainer, publicContainerInfo);
            }
            catch (Exception)
            {
                Logger.Warning("trouble with docking base: " + Eid + " but transaction saved");
            }

            return baseDict;
        }

        public Dictionary<string,object> GetDockingBaseDetails()
        {
            var info = ToDictionary();
            info[k.px] = CurrentPosition.intX;
            info[k.py] = CurrentPosition.intY;
            info[k.zone] = Zone.Id;
            return info;
        }

        public virtual double GetOwnerRefundMultiplier(TransactionType transactionType)
        {
            return 0;
        }

        public string ChannelName => $"base_{Eid}";

        public void DockIn(Character character,TimeSpan undockDelay, ZoneExitType zoneExitType)
        {
            DockIn(character,undockDelay);

            Transaction.Current.OnCommited(() =>
            {
                var data = new Dictionary<string, object>
                {
                    {k.result, new Dictionary<string, object>
                    {
                        {k.baseEID, Eid},
                        {k.reason,(byte)zoneExitType}
                    }}};

                Message.Builder.SetCommand(Commands.Dock).WithData(data).ToCharacter(character).Send();
            });
        }

        public void DockIn(Character character,TimeSpan undockDelay)
        {
            character.NextAvailableUndockTime = DateTime.Now + undockDelay;
            character.CurrentDockingBaseEid = Eid;
            character.IsDocked = true;
            character.ZoneId = null;
            character.ZonePosition = null;

            Transaction.Current.OnCommited(() => JoinChannel(character));
        }

        protected IEnumerable<Character> GetCharacters()
        {
            return Db.Query().CommandText("select characterid from characters where baseeid=@eid and active=1")
                           .SetParameter("@eid",Eid)
                           .Execute()
                           .Select(r => Character.Get(r.GetValue<int>(0)))
                           .ToArray();
        }

        public PublicContainer GetPublicContainerWithItems(Character character)
        {
            var publicContainer = GetPublicContainer();
            publicContainer.ReloadItems(character);
            return publicContainer;
        }

        public PublicContainer GetPublicContainer()
        {
            return _dockingBaseHelper.GetPublicContainer(this);
        }

        [NotNull]
        public Market GetMarketOrThrow()
        {
            return GetMarket().ThrowIfNull(ErrorCodes.MarketNotFound);
        }

        [CanBeNull]
        public Market GetMarket()
        {
            return _dockingBaseHelper.GetMarket(this);
        }

        [CanBeNull]
        public ItemShop GetItemShop()
        {
            return _dockingBaseHelper.GetItemShop(this);
        }

        
        public IEnumerable<ProductionFacility> GetProductionFacilities()
        {
            return _dockingBaseHelper.GetProductionFacilities(this);
        }

        public PublicCorporationHangarStorage GetPublicCorporationHangarStorage()
        {
            return _dockingBaseHelper.GetPublicCorporationHangarStorage(this);
        }

        [CanBeNull]
        public Robot CreateStarterRobotForCharacter(Character character,bool setActive = false)
        {
            var container = GetPublicContainerWithItems(character);

            // keresunk egy arkhet,ha van akkor csondben kilepunk
            var template = _robotTemplateRelations.GetStarterMaster(CanCreateEquippedStartRobot);

            if (container.GetItems(true).Any(i => i.Definition == template.EntityDefault.Definition))
                return null;

            // ha nincs akkor legyartunk egyet
            var robot = template.Build();
            robot.Owner = character.Eid;
            robot.Initialize(character);
            robot.Repair();

            container.AddItem(robot, true);
            container.Save();

            if (setActive)
            {
                character.SetActiveRobot(robot);
            }

            return robot;
        }

        protected virtual bool CanCreateEquippedStartRobot => Zone?.Configuration.Protected ?? false;

        public void JoinChannel(Character character)
        {
            ChannelManager.JoinChannel(ChannelName,character,ChannelMemberRole.Undefined,null);
        }

        public void LeaveChannel(Character character)
        {
            ChannelManager.LeaveChannel(ChannelName,character);
        }

        public static bool Exists(long baseEid)
        {
            return Db.Query().CommandText("select eid from zoneentities where eid=@baseEid").SetParameter("@baseEid", baseEid).ExecuteScalar<long>() > 0 ||
                   Db.Query().CommandText("select eid from zoneuserentities where eid=@baseEid").SetParameter("@baseEid", baseEid).ExecuteScalar<long>() > 0;
        }

        public virtual bool IsOnGammaZone()
        {
            return false;
        }

        public virtual bool IsVisible(Character character)
        {
            return true;
        }

        public void AddCentralBank(TransactionType transactionType, double amount)
        {
            amount = Math.Abs(amount);

            var centralBankShare = amount;

            var profitingOwner = ProfitingOwnerSelector.GetProfitingOwner(this);
            if (profitingOwner != null)
            {
                var multiplier = GetOwnerRefundMultiplier(transactionType);
                if (multiplier > 0.0)
                {
                    var shareFromOwnership = amount * multiplier;
                    centralBankShare = amount * (1 - multiplier);

                    Logger.Info("corpEID: " + profitingOwner.Eid + " adding to wallet: " + shareFromOwnership + " as docking base owner facility payback.");
                    IntrusionHelper.AddOwnerIncome(profitingOwner.Eid, shareFromOwnership);
                }
            }

            _centralBank.AddAmount(centralBankShare, transactionType);
        }
    }
}
