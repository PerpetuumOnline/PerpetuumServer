using System;
using System.Collections.Generic;
using System.Data;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.MissionEngine.TransportAssignments
{
    public partial class TransportAssignment
    {
        private const double COLLATERAL_PENALTY = 0.5;
        
        public int id;
        public DateTime creation;
        public long sourcebaseeid;
        public long targetbaseeid;
        public Character ownercharacter;
        public long reward;
        public long collateral;
        public bool taken;
        public Character volunteercharacter;
        public long containereid;
        public string containername;
        public double volume;
        public DateTime expiry;
        public DateTime? started;
        private bool _retrieved;
        private bool _deletedFromDb;

        public override string ToString()
        {
            return $"transport assignment id:{id} sourcebase:{sourcebaseeid} targetbase:{targetbaseeid} owner:{ownercharacter.Id} volunteer:{volunteercharacter.Id} name:{containername}";
        }

        private TransportAssignment(IDataRecord record)
        {
            id = record.GetValue<int>("id");
            creation = record.GetValue<DateTime>("creation");
            sourcebaseeid = record.GetValue<long>("sourcebaseeid");
            targetbaseeid = record.GetValue<long>("targetbaseeid");
            ownercharacter = Character.Get(record.GetValue<int>("ownercharacterid"));
            reward = record.GetValue<long>("reward");
            collateral = record.GetValue<long>("collateral");
            taken = record.GetValue<bool>("taken");
            volunteercharacter = Character.Get((record.GetValue<int?>("volunteercharacterid") ?? 0));
            containereid = record.GetValue<long>("containereid");
            volume = record.GetValue<double>("volume");
            expiry = record.GetValue<DateTime>("expiry");
            started = record.GetValue<DateTime?>("started");
            _retrieved = record.GetValue<bool>("retrieved");
            containername = record.GetValue<string>("containername");
        }

        private TransportAssignment() { }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID, id},
                {k.creation, creation},
                {k.sourceBase, sourcebaseeid},
                {k.targetBase, targetbaseeid},
                {k.ownerCharacter, ownercharacter.Id},
                {k.reward, reward},
                {k.collateral, collateral},
                {k.taken, taken},
                {k.volume, volume},
                {k.expire, expiry},
                {k.started, started},
                {k.retrieved, _retrieved},
                {k.name, containername},
                {k.deleted, _deletedFromDb}
            };
        }

        public Dictionary<string, object> ToPrivateDictionary()
        {
            var info = ToDictionary();
            info.Add(k.volunteer, volunteercharacter.Id);
            return info;
        }

        public void InsertToDb()
        {
            id = DynamicSqlQuery.InsertAndGetIdentity("transportassignments", new
            {
                sourcebaseeid,
                targetbaseeid,
                ownercharacterid = ownercharacter.Id,
                reward,
                collateral,
                containereid,
                volume,
                expiry,
                containername
            }).ThrowIfLessOrEqual(0, ErrorCodes.SQLInsertError);
        }
     
        public void UpdateToDb()
        {
            DynamicSqlQuery.Update("transportassignments", new
            {
                collateral,
                reward,
                targetbaseeid,
                taken,
                volunteercharacterid = volunteercharacter?.Id,
                expiry,
                started
            }, new { id }).ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        private void GetLocalPublicContainer(Character characterForVolumeWrapper, out VolumeWrapperContainer volumeWrapperContainer, Character characterForPublicContainer, out PublicContainer publicContainer)
        {
            volumeWrapperContainer = (VolumeWrapperContainer) Container.GetWithItems(containereid, characterForVolumeWrapper, ContainerAccess.List);
            var storage = Container.GetOrThrow(volumeWrapperContainer.Parent);
            publicContainer = Container.GetFromStructure(storage.Parent);
            publicContainer.ReloadItems(characterForPublicContainer);
        }

        public void SendDeliveryMessage(PublicContainer container)
        {
            var privateResult = new Dictionary<string, object>
            {
                {k.assignment, ToPrivateDictionary()}
            };

            if (volunteercharacter != Character.None)
            {
                Message.Builder.SetCommand(Commands.TransportAssignmentDelivered).WithData(privateResult).ToCharacter(volunteercharacter).Send();
            }

            var principalResult = new Dictionary<string, object>
            {
                {k.assignment, ToDictionary()},
                {k.container, container.ToDictionary()}
            };

            Message.Builder.SetCommand(Commands.TransportAssignmentDelivered).WithData(principalResult).ToCharacter(ownercharacter).Send();
        }

        public void WriteLog(TransportAssignmentEvent transportAssignmentEvent,long baseEid)
        {
            var o = new
            {
                assignmentevent = (int)transportAssignmentEvent,
                baseeid = baseEid,
                ownercharacterid = ownercharacter.Id,
                volunteercharacterid = volunteercharacter?.Id,
                assignmentid = id,
                containername,
            };

            DynamicSqlQuery.Insert("transportassignmentslog", o).ThrowIfEqual(0,ErrorCodes.SQLInsertError);
        }

        public void DeleteFromDb()
        {
            _deletedFromDb = true;

            Db.Query().CommandText("delete transportassignments where id=@ID")
                .SetParameter("@ID", id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

       

        private bool Retrieved
        {
            get { return _retrieved; }
            set
            {
                Db.Query().CommandText("update transportassignments set retrieved=@state where id=@id")
                    .SetParameter("@id", id)
                    .SetParameter("@state", value)
                    .ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLUpdateError);
                
                _retrieved = value;
            }
        }

        private void PayCollateralToPrincipal()
        {
            ownercharacter.AddToWallet(TransactionType.TransportAssignmentCollateral,collateral);
        }

        private void PaybackCollateral()
        {
            volunteercharacter.AddToWallet(TransactionType.TransportAssignmentCollateralPayback,collateral);
        }

        private void PaybackHalfCollateral()
        {
            volunteercharacter.AddToWallet(TransactionType.TransportAssignmentCollateralPaybackOnGiveUp,collateral * COLLATERAL_PENALTY);
        }

        private void PaybackReward()
        {
            ownercharacter.AddToWallet(TransactionType.TransportAssignmentRewardPayback,reward);
        }

        private void PayOutReward()
        {
            volunteercharacter.AddToWallet(TransactionType.TransportAssignmentDeliver,reward + collateral);
        }

        public void TakeCollateral(Character volunteer)
        {
            volunteer.SubtractFromWallet(TransactionType.TransportAssignmentCollateral,collateral);
        }

        //storage => base => public container -> owner character
        public void ReturnToLocalPublicContainer(out VolumeWrapperContainer volumeWrapperContainer, out PublicContainer publicContainer)
        {
            GetLocalPublicContainer(ownercharacter, out volumeWrapperContainer, ownercharacter, out publicContainer);

            publicContainer.AddItem(volumeWrapperContainer, false);
            volumeWrapperContainer.Owner = ownercharacter.Eid;
            volumeWrapperContainer.ClearAssignmentId();

            volumeWrapperContainer.Save();
        }

        public void RetrieveToBasePublicContainer(VolumeWrapperContainer volumeWrapperContainer, long targetBaseEid = 0)
        {
            var baseEid = sourcebaseeid;

            if (targetBaseEid != 0) 
                baseEid = targetBaseEid;

            var volumeInitCharacter = taken ? volunteercharacter : ownercharacter;
            var sourcePublicContainer = Container.GetFromStructure(volumeWrapperContainer.TraverseForStructureRootEid());
            sourcePublicContainer.ReloadItems(volumeInitCharacter);
            
            var wrapperContainer = sourcePublicContainer.GetItem(volumeWrapperContainer.Eid) as VolumeWrapperContainer;
            if (wrapperContainer != null && wrapperContainer.Parent == sourcePublicContainer.Eid)
            {
                sourcePublicContainer.RemoveItemOrThrow(wrapperContainer);
            }

            volumeWrapperContainer.ReloadItems(volumeInitCharacter);

            var targetPublicContainer = Container.GetFromStructure(baseEid);
            targetPublicContainer.ReloadItems(ownercharacter);

            volumeWrapperContainer.Owner = ownercharacter.Eid;
            volumeWrapperContainer.ClearAssignmentId();
            
            targetPublicContainer.AddItem(volumeWrapperContainer, false);

            PaybackCollateral();
            PaybackReward();

            targetPublicContainer.Save();
            sourcePublicContainer.Save();
            volumeWrapperContainer.Save();

            WriteLog(TransportAssignmentEvent.containerRetrieved, baseEid);

            DeleteFromDb();

            Transaction.Current.OnCommited(() =>
            {
                var info = new Dictionary<string, object>
                {
                    {k.assignment, ToPrivateDictionary()},
                    {k.container, sourcePublicContainer.ToDictionary()}
                };

                if (taken)
                {
                    //inform volunteer 
                    Message.Builder.SetCommand(Commands.TransportAssignmentContainerRetrieved).WithData(info).ToCharacter(volunteercharacter).Send();
                }

                //inform principal
                info[k.container] = targetPublicContainer.ToDictionary();
                info[k.assignment] = ToDictionary();
                Message.Builder.SetCommand(Commands.TransportAssignmentContainerRetrieved).WithData(info).ToCharacter(ownercharacter).Send();
            });
        }

        public void GiveToVolunteer(out VolumeWrapperContainer volumeWrapperContainer, out  PublicContainer publicContainer)
        {
            if (volunteercharacter == Character.None)
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            GetLocalPublicContainer(ownercharacter, out volumeWrapperContainer, volunteercharacter, out publicContainer);
           
            volumeWrapperContainer.Owner = volunteercharacter.Eid;
            publicContainer.AddItem(volumeWrapperContainer, false);
            volumeWrapperContainer.Save();
        }

        private void CashInOnSubmit()
        {
            ownercharacter.SubtractFromWallet(TransactionType.TransportAssignmentSubmit,reward);
        }

        public VolumeWrapperContainer GetContainer()
        {
            return (VolumeWrapperContainer) Container.GetOrThrow(containereid);
        }

        public void GiveUpAssignment(Character issuerCharacter)
        {
            taken.ThrowIfFalse(ErrorCodes.AccessDenied);

            volunteercharacter.ThrowIfEqual(null,ErrorCodes.AccessDenied);
            volunteercharacter.ThrowIfNotEqual(issuerCharacter,ErrorCodes.AccessDenied);

            var volumeWrapperContainer = GetContainer();
            volumeWrapperContainer.ReloadItems(volunteercharacter);
            volumeWrapperContainer.TraverseForStructureRootEid().ThrowIfLessOrEqual(0,ErrorCodes.ContainerHasToBeOnADockingBase);

            //container is on a base somewhere
            var baseEid = sourcebaseeid;

            if (!DockingBase.Exists(sourcebaseeid))
            {
                baseEid = ownercharacter.CurrentDockingBaseEid;
            }

            var sourceContainer = Container.GetOrThrow(volumeWrapperContainer.Parent);
            sourceContainer.ReloadItems(volunteercharacter);

            var wrapperContainer = sourceContainer.GetItem(volumeWrapperContainer.Eid) as VolumeWrapperContainer;
            if (wrapperContainer != null)
                sourceContainer.RemoveItemOrThrow(wrapperContainer);

            var publicContainer = Container.GetFromStructure(baseEid);
            publicContainer.ReloadItems(ownercharacter);

            volumeWrapperContainer.Owner = ownercharacter.Eid;
            volumeWrapperContainer.ClearAssignmentId();
            DeleteFromDb();

            publicContainer.AddItem(volumeWrapperContainer, false);
            publicContainer.Save();

            PaybackReward();
            PaybackHalfCollateral();

            WriteLog(TransportAssignmentEvent.gaveUp, baseEid);

            Transaction.Current.OnCommited(() =>
            {
                var result = new Dictionary<string, object>
                {
                    {k.assignment, ToDictionary()},
                    {k.container, publicContainer.ToDictionary()}
                };

                Message.Builder.SetCommand(Commands.TransportAssignmentGaveUp)
                    .WithData(result)
                    .ToCharacter(ownercharacter)
                    .Send();

                if (volunteercharacter == Character.None)
                    return;

                var privateResult = new Dictionary<string, object>
                {
                    {k.assignment, ToPrivateDictionary()},
                    {k.container, sourceContainer.ToDictionary()},
                };

                Message.Builder.SetCommand(Commands.TransportAssignmentGaveUp)
                    .WithData(privateResult)
                    .ToCharacter(volunteercharacter)
                    .Send();
            });
        }



        private void Expired()
        {
            VolumeWrapperContainer volumeWrapperContainer;
            PublicContainer publicContainer;
            ReturnToLocalPublicContainer(out volumeWrapperContainer, out publicContainer);

            PaybackReward();
            DeleteFromDb();
            WriteLog(TransportAssignmentEvent.expired, publicContainer.Parent);

            SendCommandWithTransportAssignmentsAndContainer(Commands.TransportAssignmentExpired, publicContainer, ownercharacter);
        }
    }
}