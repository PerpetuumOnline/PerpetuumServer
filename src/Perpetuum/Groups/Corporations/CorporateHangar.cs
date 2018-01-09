using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.Insurance;
using Perpetuum.Services.Standing;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Groups.Corporations
{
    /// <summary>
    /// A container with corporate role based access with infinte capacity
    /// </summary>
    public class CorporateHangar : Container
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override void ReloadItems(long? ownerEid)
        {
            base.ReloadItems(Owner);
        }

        public override void OnInsertToDb()
        {
            DynamicProperties.Update(k.hangarAccess,(int)CorporationRole.HangarAccess_low);
        }

        public void CheckAccessAndThrowIfFailed(CorporationRole memberRole, ContainerAccess access)
        {
            HasAccess(memberRole, access).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
        }

        public bool HasAccess(CorporationRole memberRole, ContainerAccess access)
        {
            //not defined for this container
            if (HangarAccess == CorporationRole.NotDefined) 
                return true;

            //corp lord?
            if (memberRole.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO))
                return true;

            //return a single bit: the highest
            var hangarAccess = HangarAccess.GetHighestContainerAccess();

            // let's check the action
            var expectedAccess = (int) memberRole.CleanUpHangarAccess() & (int) hangarAccess;
            if (expectedAccess == 0)
                return false;

            if (access != ContainerAccess.Delete && access != ContainerAccess.Remove && access != ContainerAccess.List)
                return true;

            return memberRole.IsAnyRole(hangarAccess.GetRelatedRemoveAccess());
        }

        private CorporationRole HangarAccess
        {
            get { return (CorporationRole)DynamicProperties.GetOrAdd<int>(k.hangarAccess); }
        }

        public void SetHangarAccess(Character character, CorporationRole corporationRole)
        {
            DynamicProperties.Update(k.hangarAccess,(int)corporationRole);
            AddLogEntry(character, ContainerAccess.SetAccess);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.leaseStart,LeaseStart);
            result.Add(k.leaseEnd,LeaseEnd);
            result.Add(k.leaseExpired,IsLeaseExpired);
            result.Add(k.hangarAccess,(int)HangarAccess);
            return result;
        }

        public bool IsLeaseExpired
        {
            get
            {
                if (DynamicProperties.Contains(k.leaseExpired))
                {
                    return (DynamicProperties.GetOrAdd<int>(k.leaseExpired) > 0);
                }

                return false;
            }
            set
            {
                var intValue = (value) ? 1 : 0;
                DynamicProperties.Update(k.leaseExpired,intValue);
            }
        }

        public DateTime LeaseStart
        {
            get
            {
                if (DynamicProperties.Contains(k.leaseStart))
                {
                    return DynamicProperties.GetOrAdd<DateTime>(k.leaseStart);
                }

                //safe return
                return DateTime.Now.AddYears(7);
            }
            set
            {
                DynamicProperties.Update(k.leaseStart,value);
            }
        }

        public DateTime LeaseEnd
        {
            get
            {
                if (DynamicProperties.Contains(k.leaseEnd))
                {
                    return DynamicProperties.GetOrAdd<DateTime>(k.leaseEnd);
                }

                //safe return
                return DateTime.Now.AddYears(7);
            }
            set
            {
                DynamicProperties.Update(k.leaseEnd,value);
            }
        }

        public override void AddItem(Item item,long issuerEid, bool doStack)
        {
            CheckAllowedTypesForAddAndThrowIfFailed(item);
            base.AddItem(item,Owner, doStack);
        }

        public static void CheckAllowedTypesForAddAndThrowIfFailed(Item item)
        {
            item.ThrowIfType<Container>(ErrorCodes.ContainersAreNotSupportedTryCreatingACorporateFolder);

            if (item is Robot robot)
            {
                robot.IsSelected.ThrowIfTrue(ErrorCodes.RobotMustBeDeselected);
                InsuranceHelper.IsPrivateInsured(robot.Eid).ThrowIfTrue(ErrorCodes.OperationNotAllowedOnInsuredItem);
            }
        }

        public IEnumerable<CorporateHangarFolder> Folders
        {
            get { return GetItems().OfType<CorporateHangarFolder>(); }
        }

        public override void SetLogging(bool state, Character character, bool doLog = false)
        {
            foreach (var hangarFolder in Folders)
            {
                hangarFolder.SetLogging(state,character);
            }

            base.SetLogging(state,character,doLog);
        }

        protected override bool IsPersonalContainer
        {
            get { return false; }
        }

        public static bool Contains(Item item)
        {
            Container container;
            if (!TryFindContainerRoot(item, out container))
                return false;

            return container.ED.CategoryFlags.IsCategory(CategoryFlags.cf_corporate_hangar);
        }

        private static bool TryFindContainerRoot(Item item, out Container container)
        {
            var publicContainer = EntityDefault.GetByName(DefinitionNames.PUBLIC_CONTAINER);
            var corporateHangar = EntityDefault.GetByName(DefinitionNames.CORPORATE_HANGAR_STANDARD);

            var rootEid = Db.Query().CommandText("findContainerRoot")
                                  .SetParameter("@publicContainerDefinition", publicContainer.Definition)
                                  .SetParameter("@corporateHangarDefinition", corporateHangar.Definition)
                                  .SetParameter("@itemEid", item.Eid)
                                  .ExecuteScalar<long?>();

            if (rootEid == null)
            {
                container = null;
                return false;
            }

            container = GetOrThrow((long)rootEid);
            return true;
        }

        public Corporation GetCorporation()
        {
            return Corporation.GetOrThrow(Owner);
        }

        public PublicCorporationHangarStorage GetHangarStorage()
        {
            return (PublicCorporationHangarStorage)GetOrThrow(Parent);
        }

        public static Task CollectHangarRentAsync(IStandingHandler standingHandler)
        {
            return Task.Run(() => CollectHangarRent(standingHandler));
        }

        private static void CollectHangarRent(IStandingHandler standingHandler)
        {
            var storage = EntityDefault.GetByName(DefinitionNames.PUBLIC_CORPORATE_HANGARS_STORAGE);

            var hangarEids = Db.Query().CommandText("select eid from entities where parent in (SELECT eid FROM dbo.getLiveDockingbaseChildren() WHERE definition=@hangarDef) order by parent")
                                     .SetParameter("@hangarDef",storage.Definition)
                                     .Execute()
                                     .Select(h => (CorporateHangar)GetOrThrow(h.GetValue<long>(0)))
                                     .ToArray();

            Logger.Info("--- hangars collected for rent check: " + hangarEids.Count());

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    foreach (var hangar in hangarEids)
                    {
                        var hangarStorage = hangar.GetHangarStorage();
                        switch (hangarStorage.GetParentDockingBase())
                        {
                            case Outpost outpost:
                            {
                                var siteInfo = outpost.GetIntrusionSiteInfo();
                                if (siteInfo?.Owner != null)
                                {
                                    //it has an owner
                                    if (hangar.Owner != siteInfo.Owner)
                                    {
                                        //the owner is not the hangar's owner
                                        var dockingStandingLimit = siteInfo.DockingStandingLimit;
                                        if (dockingStandingLimit != null)
                                        {
                                            //the outpost has standing limit set
                                            var standingTowardsOwner = standingHandler.GetStanding((long)siteInfo.Owner,hangar.Owner);

                                            if (standingTowardsOwner < dockingStandingLimit)
                                            {
                                                //the hangar is inaccessible
                                                Logger.Info("hangar is inaccessible for corp. " + hangar.Owner + " hangaried:" + hangar.Eid + " standing:" + standingTowardsOwner + " dockingStandingLimit:" + dockingStandingLimit);
                                                continue;
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                            case PBSDockingBase pbsDockingBase:
                            {
                                if (pbsDockingBase.StandingEnabled)
                                {
                                    var standingTowardsOwner = standingHandler.GetStanding(pbsDockingBase.Owner,hangar.Owner);

                                    if (standingTowardsOwner < pbsDockingBase.StandingLimit)
                                    {
                                        Logger.Info("hangar is inaccessible for corp. " + hangar.Owner + " hangaried:" + hangar.Eid + " standing:" + standingTowardsOwner + " dockingStandingLimit:" + pbsDockingBase.StandingLimit);
                                        continue;
                                    }
                                }
                                break;
                            }
                        }

                        var rentInfo = hangarStorage.GetCorporationHangarRentInfo();

                        // rent expired?

                        if (hangar.IsLeaseExpired)
                            continue;

                        if (DateTime.Now > hangar.LeaseEnd)
                        {
                            var corporation = hangar.GetCorporation();

                            Logger.Info("--- hangar rent process started for hangarEID:" + hangar.Eid + " hangarName:" + hangar.Name + " corporaration:" + corporation.Eid + " corpname:" + corporation.Description.name);

                            var wallet = new CorporationWallet(corporation);

                            if (wallet.Balance < rentInfo.price)
                            {
                                Logger.Info("--- corporation is broken. corporationEID:" + corporation.Eid + " hangar closed. EID:" + hangar.Eid);

                                //corporation broken
                                hangar.IsLeaseExpired = true; //block the hangar's content

                                //alert accountants
                                var info = new Dictionary<string, object> { { k.containerEID, hangar.Eid } };

                                Message.Builder.SetCommand(Commands.CorporationHangarRentExpired)
                                               .WithData(info)
                                               .ToCorporation(corporation,CorporationRole.Accountant)
                                               .Send();
                            }
                            else
                            {
                                wallet.Balance -= rentInfo.price;

                                var b = TransactionLogEvent.Builder()
                                                           .SetCorporation(corporation)
                                                           .SetTransactionType(TransactionType.hangarRentAuto)
                                                           .SetCreditBalance(wallet.Balance)
                                                           .SetCreditChange(-rentInfo.price);

                                corporation.LogTransaction(b);

                                hangarStorage.GetParentDockingBase().AddCentralBank(TransactionType.hangarRentAuto, rentInfo.price);

                                hangar.LeaseStart = DateTime.Now;
                                hangar.LeaseEnd = DateTime.Now + rentInfo.period;
                                hangar.IsLeaseExpired = false;

                                Logger.Info("--- hangar price paid. hangarEID: " + hangar.Eid + " lease ended:" + hangar.LeaseEnd + " lease extened:" + hangar.LeaseEnd);
                            }

                            hangar.Save();
                        }
                        else
                        {
                            Logger.Info("--- hangar still paid. eid:" + hangar.Eid + " lease end:" + hangar.LeaseEnd);
                        }
                    }

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        public static CorporateHangar Create()
        {
            return (CorporateHangar)Factory.CreateWithRandomEID(EntityDefault.GetByName(DefinitionNames.CORPORATE_HANGAR_STANDARD));
        }
    }

}
