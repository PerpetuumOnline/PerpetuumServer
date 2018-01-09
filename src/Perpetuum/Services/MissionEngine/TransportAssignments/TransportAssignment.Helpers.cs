using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.MissionEngine.TransportAssignments
{
    partial class TransportAssignment
    {
        public static IEntityServices EntityServices { get; set; }

        /// <summary>
        /// Creates a transport assignment
        /// </summary>
        public static void SubmitTransportAssignment(Character character, long wrapperContainerEid, long reward, long collateral, long sourceBaseEid, long targetBaseEid, int durationDays)
        {
            if (durationDays <= 0)
                durationDays = 1;

            reward.ThrowIfLessOrEqual(0, ErrorCodes.IllegalTransportAssignmentReward);
            collateral.ThrowIfLess(0, ErrorCodes.IllegalTransportAssignmentCollateral);

            targetBaseEid.ThrowIfEqual(sourceBaseEid, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            DockingBase.Exists(targetBaseEid).ThrowIfFalse(ErrorCodes.TargetDockingBaseWasNotFound);

            var publicContainer = Container.GetFromStructure(sourceBaseEid);
            publicContainer.ReloadItems(character);
            var wrapperContainer = (VolumeWrapperContainer)publicContainer.GetItemOrThrow(wrapperContainerEid);
            
            wrapperContainer.CheckSubmitConditionsAndThrowIfFailed();

            var transportAssignmentInfo = new TransportAssignment
            {
                reward = reward,
                ownercharacter = character,
                collateral = collateral,
                containereid = wrapperContainerEid,
                sourcebaseeid = sourceBaseEid,
                targetbaseeid = targetBaseEid,
                volume = wrapperContainer.Volume,
                expiry = DateTime.Now.AddDays(durationDays),
                containername = wrapperContainer.Name,
            };

            transportAssignmentInfo.CashInOnSubmit();
            transportAssignmentInfo.InsertToDb();

            wrapperContainer.Parent = GetTransportStorageEid(sourceBaseEid).ThrowIfLessOrEqual(0, ErrorCodes.ServerError);
            wrapperContainer.AssignmentId = transportAssignmentInfo.id;

            wrapperContainer.Save();

            transportAssignmentInfo.WriteLog(TransportAssignmentEvent.submit, sourceBaseEid);
            SendCommandWithTransportAssignmentsAndContainer(Commands.TransportAssignmentSubmit, publicContainer, character);
        }

        /// <summary>
        /// Cancels a waiting - not taken - transport assignment
        /// </summary>
        public static void CancelWaitingTransportAssignment(int id, Character character)
        {
            var transportAssignmentInfo = Get(id);

            transportAssignmentInfo.ownercharacter.ThrowIfNotEqual(character, ErrorCodes.AccessDenied);
            transportAssignmentInfo.taken.ThrowIfTrue(ErrorCodes.TransportAssignmentIsTaken);
            transportAssignmentInfo.volunteercharacter.ThrowIfNotEqual(null, ErrorCodes.TransportAssignmentIsTaken);

            DateTime.Now.Subtract(transportAssignmentInfo.creation).TotalSeconds.ThrowIfLess(60, ErrorCodes.TransportAssignmentCancelTooEarly);

            VolumeWrapperContainer volumeWrapperContainer;
            PublicContainer publicContainer;
            transportAssignmentInfo.ReturnToLocalPublicContainer(out volumeWrapperContainer, out publicContainer);
            transportAssignmentInfo.PaybackReward();
            transportAssignmentInfo.DeleteFromDb();
            transportAssignmentInfo.WriteLog(TransportAssignmentEvent.cancel, publicContainer.Parent);

            SendCommandWithTransportAssignmentsAndContainer(Commands.TransportAssignmentCancel, publicContainer, character);
        }


        public static void SendCommandWithTransportAssignmentsAndContainer(Command command, PublicContainer publicContainer, Character character, Dictionary<string, object> extraInfo = null)
        {
            Transaction.Current.OnCommited(() =>
            {
                var containerInfo = publicContainer.ToDictionary();
                var transportInfos = GetRunningTransportAssignments(character).ToDictionary(true, character);

                var result = new Dictionary<string, object>
                {
                    {k.container, containerInfo},
                    {k.transportAssignments, transportInfos}
                };

                if (extraInfo != null)
                {
                    result.AddRange(extraInfo);
                }

                result.Add(k.count, GetCountInfo(character));
                Message.Builder.SetCommand(command)
                    .WithData(result)
                    .ToCharacter(character)
                    .Send();
            });
        }

        private static readonly ConcurrentDictionary<long, long> _baseToTransportStorages = new ConcurrentDictionary<long, long>();
        private const int MAXBONUS = 1000000;

        private static long GetTransportStorageEid(long baseEid)
        {
            return _baseToTransportStorages.GetOrAdd(baseEid, GetOrCreateTransportStorage(baseEid));
        }

        private static long GetOrCreateTransportStorage(long baseEid)
        {
            var tsEd = EntityServices.Defaults.GetByName(DefinitionNames.TRANSPORT_STORAGE);
            var storage = EntityServices.Repository.GetFirstLevelChildren_(baseEid).OfType<DefaultSystemContainer>().FirstOrDefault(e => e.ED == tsEd);
            if (storage != null)
            {
                Logger.Info($"transport storage was found on base: {baseEid} storageEid:{storage.Eid}");
                return storage.Eid;
            }

            var dockingBase = EntityServices.Repository.Load(baseEid);
            storage = (DefaultSystemContainer)EntityServices.Factory.CreateWithRandomEID(tsEd);
            storage.Parent = dockingBase.Eid;
            storage.Name = $"transport storage {dockingBase.Name}";
            storage.Save();

            Logger.Info($"transport storage created on base: {baseEid} storageEid:{storage.Eid}");
            return storage.Eid;
        }

        private static IEnumerable<long> GetRunningTransportAssignmentWrapperEids(Character character)
        {
            return Db.Query().CommandText("select containereid from transportassignments where volunteercharacterid=@characterID")
                           .SetParameter("@characterID", character.Id)
                           .Execute()
                           .Select(r => r.GetValue<long>(0));
        }

        public static void ManualDeliverTransportAssignment(Character character, long wrapperEid)
        {
            ErrorCodes ec;

            VolumeWrapperContainer volumeWrapperContainer;
            if ((ec = PrepareDeliverOneAssignment(character, wrapperEid, out volumeWrapperContainer)) != ErrorCodes.NoError)
            {
                throw new PerpetuumException(ec);
            }

            var containerEid = volumeWrapperContainer.Parent;
            var container = Container.GetOrThrow(containerEid);
            container.ReloadItems(character);

            Message.Builder.SetCommand(Commands.ListContainer)
                .WithData(container.ToDictionary())
                .ToCharacter(character)
                .Send();

        }



        public static Task DeliverTransportAssignmentAsync(Character character)
        {

           return Task.Run(() => TryDeliverPossibleTransportAssignments(character));
        }

        /// <summary>
        /// Automatic at docking time
        /// </summary>
        private static void TryDeliverPossibleTransportAssignments(Character character)
        {
            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var wrapperEids = GetRunningTransportAssignmentWrapperEids(character);
                    foreach (var wrapperEid in wrapperEids)
                    {
                        VolumeWrapperContainer volumeWrapperContainer;
                        PrepareDeliverOneAssignment(character, wrapperEid, out volumeWrapperContainer);
                    }

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        private static ErrorCodes PrepareDeliverOneAssignment(Character character, long wrapperEid, out VolumeWrapperContainer volumeWrapperContainer)
        {
            volumeWrapperContainer = Container.GetOrThrow(wrapperEid) as VolumeWrapperContainer;
            if (volumeWrapperContainer == null)
                return ErrorCodes.DefinitionNotSupported;

            if (volumeWrapperContainer.Owner != character.Eid)
                return ErrorCodes.AccessDenied;

            var transportAssignmentInfo = GetByContainer(volumeWrapperContainer);
            
            if (transportAssignmentInfo == null)
            {
                //no assignment for this container
                return ErrorCodes.NoTransportAssignmentForContainer;
            }

            var ownerCharacter = transportAssignmentInfo.ownercharacter;

            if (transportAssignmentInfo.Retrieved)
            {
                var targetBaseEid = transportAssignmentInfo.sourcebaseeid;
                if (!DockingBase.Exists(transportAssignmentInfo.sourcebaseeid))
                {
                    targetBaseEid = DefaultCorporation.GetDockingBaseEid(ownerCharacter);
                }

                Logger.Info("retrieved assignment was found " + transportAssignmentInfo);
                transportAssignmentInfo.RetrieveToBasePublicContainer(volumeWrapperContainer, targetBaseEid);
                return ErrorCodes.NoError;
            }

            var baseOfWrapper = volumeWrapperContainer.TraverseForStructureRootEid();

            if (transportAssignmentInfo.targetbaseeid != baseOfWrapper)
                return ErrorCodes.TransportAssignmentCannotBeDeliveredHere;

            var publicContainer = Container.GetFromStructure(transportAssignmentInfo.targetbaseeid);
            publicContainer.ReloadItems(character);

            AdministerDelivery(transportAssignmentInfo, volumeWrapperContainer,publicContainer);

            return ErrorCodes.NoError;

        }




        private static void AdministerDelivery(TransportAssignment transportAssignmentInfo, VolumeWrapperContainer volumeWrapperContainer, PublicContainer container)
        {
            //normal procedure 
            //successful delivery

            transportAssignmentInfo.PayOutReward();

            volumeWrapperContainer.ReloadItems(transportAssignmentInfo.volunteercharacter); 
            volumeWrapperContainer.PrintDebug();

            container.AddItem(volumeWrapperContainer, transportAssignmentInfo.ownercharacter.Eid, false);
            
            volumeWrapperContainer.Owner = transportAssignmentInfo.ownercharacter.Eid;
            volumeWrapperContainer.ClearAssignmentId();
            volumeWrapperContainer.Save();

            volumeWrapperContainer.PrintDebug();

            //owner
            transportAssignmentInfo.WriteLog(TransportAssignmentEvent.deliver, container.Parent);
            transportAssignmentInfo.DeleteFromDb();
            transportAssignmentInfo.SendDeliveryMessage(container);
        }

        public static IDictionary<string, object> TransportAssignmentHistory(int offsetInDays, int characterId)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-7);

            const string sqlCmd = @"SELECT eventtime,assignmentevent,baseeid,assignmentid,containername as name, ( Case When (ownercharacterid=@characterId) then 1 else 0 end) as isowned
                                    FROM transportassignmentslog 
                                    WHERE (ownercharacterid=@characterId or volunteercharacterid=@characterId) AND (eventtime between @earlier AND @later)";

            var result = Db.Query().CommandText(sqlCmd)
                                .SetParameter("@characterId",characterId)
                                .SetParameter("@earlier",earlier)
                                .SetParameter("@later",later)
                                .Execute()
                                .RecordsToDictionary("t");
            return result;
        }

        private static int GetAssignmentCount(Character character)
        {
            return Db.Query().CommandText("select count(*) from transportassignments where volunteercharacterid=@characterID")
                            .SetParameter("@characterID", character.Id)
                            .ExecuteScalar<int>();
        }

        private static double GetAverageTimeInPast20Days(long sourceBaseEid, long targetBaseEid)
        {
            return Db.Query().CommandText("SELECT ISNULL(AVG(totalseconds),0)  FROM dbo.transportassignmenttimes WHERE ((sourcebase=@sourceBase AND targetbase=@targetBase) or (sourcebase=@targetBase AND targetbase=@sourceBase)) and eventtime>@past")
                           .SetParameter("@sourceBase", sourceBaseEid)
                           .SetParameter("@targetBase", targetBaseEid)
                           .SetParameter("@past", DateTime.Now.AddDays(-20))
                           .ExecuteScalar<double>();
        }

        public static void DockingBaseKilled(long baseEid)
        {
            var assignments = GetRunningTransportAssignmentsByBaseEid(baseEid).ToArray();

            foreach (var transportAssignmentInfo in assignments)
            {
                var ownerCharacter = transportAssignmentInfo.ownercharacter;

                var volumeWrapperContainer = transportAssignmentInfo.GetContainer();

                var containerRoot = volumeWrapperContainer.TraverseForStructureRootEid();

                if (containerRoot > 0)
                {
                    //yes, the container is retrievable.

                    if (containerRoot == baseEid)
                    {
                        // the container dies
                        Logger.Info("the container was on the killed base. " + transportAssignmentInfo);
                        transportAssignmentInfo.WriteLog(TransportAssignmentEvent.targetBaseDeleted, transportAssignmentInfo.targetbaseeid);
                    }
                    else
                    {
                        Logger.Info("retrieve location is valid. " + transportAssignmentInfo);

                        if (transportAssignmentInfo.sourcebaseeid == baseEid)
                        {
                            //innen kene vinni, de nem rakhatom ide vissza mert ez a bazis semmisul meg eppen
                            baseEid = DefaultCorporation.GetDockingBaseEid(ownerCharacter);
                        }
                        else
                        {
                            baseEid = transportAssignmentInfo.sourcebaseeid;
                        }

                        transportAssignmentInfo.RetrieveToBasePublicContainer(volumeWrapperContainer, baseEid);
                        Logger.Info("transport assignment's related base was deleted " + transportAssignmentInfo);
                    }

                    continue;
                }

                //the container is in a robot cargo on the zone, next docking will take care of it
                transportAssignmentInfo.Retrieved = true;
                transportAssignmentInfo.WriteLog(TransportAssignmentEvent.targetBaseDeleted, transportAssignmentInfo.targetbaseeid);
            }

            Transaction.Current.OnCommited(() =>
            {
                foreach (var transportAssignmentInfo in assignments)
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.assignment, transportAssignmentInfo.ToDictionary()}
                    };

                    Message.Builder.SetCommand(Commands.TransportAssignmentBaseDeleted).WithData(result).ToCharacter(transportAssignmentInfo.ownercharacter).Send();

                    if (transportAssignmentInfo.volunteercharacter != Character.None)
                    {
                        Message.Builder.SetCommand(Commands.TransportAssignmentBaseDeleted).WithData(result).ToCharacter(transportAssignmentInfo.volunteercharacter).Send();
                    }
                }

                Logger.Info(assignments.Length + " transport assignments were deleted with base:" + baseEid);
            });
        }

        public static void RetrieveTransportAssignment(int id, Character character)
        {
            var transportAssignmentInfo = Get(id);

            transportAssignmentInfo.ownercharacter.ThrowIfNotEqual(character, ErrorCodes.AccessDenied);
            transportAssignmentInfo.expiry.ThrowIfGreater(DateTime.Now, ErrorCodes.TransportAssignmentNotExpired);
            transportAssignmentInfo.taken.ThrowIfFalse(ErrorCodes.TransportAssignmentIsNotTaken);
            transportAssignmentInfo.Retrieved.ThrowIfTrue(ErrorCodes.TransportAssignmentAlreadyRetrieved);

            var volumeWrapperContainer = transportAssignmentInfo.GetContainer();
            var baseEid = volumeWrapperContainer.TraverseForStructureRootEid();
            var retriteveSuccess = false;

            if (baseEid > 0)
            {
                //its docked somewhere, so we can take it immediately
                transportAssignmentInfo.RetrieveToBasePublicContainer(volumeWrapperContainer);
                retriteveSuccess = true;
            }
            else
            {
                //mark it retrieved, so the next dock in can take care of it
                transportAssignmentInfo.Retrieved = true;
                transportAssignmentInfo.WriteLog(TransportAssignmentEvent.retrieved, transportAssignmentInfo.sourcebaseeid);
            }

            Transaction.Current.OnCommited(() =>
            {
                var result = new Dictionary<string, object>
                {
                    {k.assignment, transportAssignmentInfo.ToDictionary()},
                    {k.success, retriteveSuccess}
                };

                Message.Builder.SetCommand(Commands.TransportAssignmentRetrieved).WithData(result).ToCharacter(transportAssignmentInfo.ownercharacter).Send();

                if (transportAssignmentInfo.volunteercharacter == Character.None)
                    return;

                var privateResult = new Dictionary<string, object>
                {
                    {k.assignment, transportAssignmentInfo.ToPrivateDictionary()}
                };

                Message.Builder.SetCommand(Commands.TransportAssignmentRetrieved).WithData(privateResult).ToCharacter(transportAssignmentInfo.volunteercharacter).Send();
            });
        }

        private static int GetMaxOwnedAssignments(Character character)
        {
            return character.GetExtensionLevelSummaryByName(k.ext_transport_assignments_amount_basic);
        }

        public static Dictionary<string, object> GetCountInfo(Character character)
        {
            return new Dictionary<string, object>
                {
                    {k.max,GetMaxOwnedAssignments(character)},
                    {k.count,GetAssignmentCount(character)},
                };
        }

        public static void CharacterDeleted(Character character)
        {
            foreach (var assignment in GetRunningTransportAssignments(character))
            {
                if (assignment.ownercharacter == character && !assignment.taken)
                {
                    //his pending assignment, just simply delete
                    assignment.DeleteFromDb();
                    continue;
                }

                if (assignment.volunteercharacter != character)
                    continue;

                //this assigment was accepted by the deleted character

                var container = assignment.GetContainer();

                var publicContainer =  Container.GetFromStructure(assignment.sourcebaseeid);
                
                container.Parent = publicContainer.Eid;
                container.Save();

                //no collateral payback, since the character was deleted

                assignment.PaybackReward();
                assignment.DeleteFromDb();

                var assignment1 = assignment;
                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.assignment, assignment1.ToDictionary()},
                        {k.success, true}
                    };

                    Message.Builder.SetCommand(Commands.TransportAssignmentRetrieved).WithData(result).ToCharacter(assignment1.ownercharacter).Send();
                });
            }
        }


        public static void ContainerDestroyed(VolumeWrapperContainer container)
        {
            var transportAssignmentInfo = GetByContainer(container);

            if (transportAssignmentInfo == null)
            {
                //container is not connected to any transport assignment
                //nothing to do
                return;
            }


            Logger.Info("assignment cancelling on container destroy " + transportAssignmentInfo);

            transportAssignmentInfo.PaybackReward();
            transportAssignmentInfo.PayCollateralToPrincipal();
            transportAssignmentInfo.WriteLog(TransportAssignmentEvent.failed, transportAssignmentInfo.sourcebaseeid);
            transportAssignmentInfo.DeleteFromDb();

            Transaction.Current.OnCommited(() =>
            {
                var result = new Dictionary<string, object>
                {
                    {k.assignment, transportAssignmentInfo.ToDictionary()}
                };

                Message.Builder.SetCommand(Commands.TransportAssignmentFailed)
                    .WithData(result)
                    .ToCharacters(transportAssignmentInfo.ownercharacter, transportAssignmentInfo.volunteercharacter)
                    .Send();
            });
        }

        [CanBeNull]
        private static TransportAssignment GetByContainer(Container container)
        {
            var record = Db.Query().CommandText("select * from transportassignments where containereid=@containerEID")
                .SetParameter("@containerEid", container.Eid)
                .ExecuteSingleRow();

            return record == null ? null :  new TransportAssignment(record);
        }

        [NotNull]
        public static TransportAssignment Get(int id)
        {
            var record = Db.Query().CommandText("select * from transportassignments where id=@ID")
                .SetParameter("@ID", id)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.ItemNotFound);

            return new TransportAssignment(record);
        }

        public static IEnumerable<TransportAssignment> GetRunningTransportAssignments(Character character)
        {
            return Db.Query().CommandText("select * from transportassignments where volunteercharacterid=@characterID or ownercharacterid=@characterID")
                .SetParameter("@characterID", character.Id)
                .Execute()
                .Select(r => new TransportAssignment(r));
        }

        private static IEnumerable<TransportAssignment> GetRunningTransportAssignmentsByBaseEid(long baseEid)
        {
            return Db.Query().CommandText("select * from transportassignments where targetbaseeid=@baseEid or sourcebaseeid=@baseEid")
                .SetParameter("@baseEid", baseEid)
                .Execute()
                .Select(r => new TransportAssignment(r));
        }

        public static IEnumerable<TransportAssignment> GetAdvertisedTransportAssignments(Character character)
        {
            return Db.Query().CommandText("select * from transportassignments where (ownercharacterid=@characterID or volunteercharacterid=@characterID or taken=0) and expiry>@now")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@now", DateTime.Now)
                .Execute()
                .Select(r => new TransportAssignment(r));
        }

        public static Task CleanUpExpiredAssignmentsAsync()
        {
            return Task.Run(() => CleanUpExpiredAssignments());
        }

        private static void CleanUpExpiredAssignments()
        {
            var assignments = Db.Query().CommandText("select * from transportassignments where expiry<@now and taken=0")
                .SetParameter("@now", DateTime.Now)
                .Execute()
                .Select(r => new TransportAssignment(r)).ToArray();

            foreach (var assignmentInfo in assignments)
            {
                using (var scope = Db.CreateTransaction())
                {
                    try
                    {
                        assignmentInfo.Expired();
                        scope.Complete();
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }
            }
        }

    }
}
