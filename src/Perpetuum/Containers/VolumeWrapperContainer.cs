using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine.TransportAssignments;

namespace Perpetuum.Containers
{
    /// <summary>
    /// A container which reflects the volume of the contained items
    /// </summary>
    public class VolumeWrapperContainer : Container 
    {
        private Character _principalCharacter = Character.None;

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override bool IsStackable
        {
            get { return base.IsStackable && IsRepackaged; }
        }

        public Character PrincipalCharacter
        {
            get
            {
                if (_principalCharacter == Character.None)
                {
                    _principalCharacter = Character.Get(DynamicProperties.GetOrAdd<int>(k.principal));
                }

                return _principalCharacter;
            }
            set
            {
                _principalCharacter = value;
                DynamicProperties.Update(k.principal,value.Id);
            }
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            if (DynamicProperties.Contains(k.principal))
            {
                info.Add(k.principal, PrincipalCharacter.Id);
            }

            if (IsInAssignment())
            {
                info.Add(k.assignmentID, AssignmentId);
            }
            
            return info;
        }

        public override void AddItem(Item item, long issuerEid, bool doStack)
        {
            item.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.DefinitionNotSupported);

            if (item is Robot)
            {
                item.IsRepackaged.ThrowIfFalse(ErrorCodes.ItemHasToBeRepackaged);
            }

            base.AddItem(item, issuerEid, doStack);
        }

        public void SetRandomName()
        {
            var nameString = FastRandom.NextString(3);
            var nameNumber = FastRandom.NextInt(0 , 999);
            Name = $"{nameString.ToUpper()}-{nameNumber}";

        }

        public int AssignmentId
        {
            get { return DynamicProperties.GetOrAdd<int>(k.assignmentID); }
            set
            {
                DynamicProperties.Update(k.assignmentID,value);
            }
        }

        public Dictionary<string,object> GetAssignmentInfo()
        {
            var result = ToDictionary();

            if (AssignmentId > 0)
            {
                var transportAssignmentInfo = TransportAssignment.Get(AssignmentId);
                result.Add(k.assignment, transportAssignmentInfo.ToPrivateDictionary());
            }

            return result;
        }

        public void ClearAssignmentId()
        {
            //isUpdated trigger
            DynamicProperties.Remove(k.assignmentID);
        }

        private bool _allowDelete;

        private bool IsDeleteAllowed()
        {
            return _allowDelete;
        }

        public void SetAllowDelete()
        {
            _allowDelete = true;
        }

        public bool IsInAssignment()
        {
            return DynamicProperties.Contains(k.assignmentID);
        }

        public override void SetItemName(long itemEid, string newName)
        {
            throw new PerpetuumException(ErrorCodes.AccessDenied);
        }

        public override void OnDeleteFromDb()
        {
            if (IsRepackaged)
                return;
            
            if (!IsDeleteAllowed())
            {
                base.OnDeleteFromDb();
                IsInAssignment().ThrowIfTrue(ErrorCodes.ContainerInAssignment);
            }

            TransportAssignment.ContainerDestroyed(this);
        }

        public IEnumerable<LootItem> GetLootItems()
        {
            var lista = new List<LootItem>();
            foreach (var item in GetItems())
            {
                var resultQuantity = (int) (Math.Round(item.Quantity*FastRandom.NextDouble()));
                var resutlDefinition = item.Definition;

                if (resultQuantity <= 0) 
                    continue;

                var lootItem = LootItemBuilder.Create(resutlDefinition).SetQuantity(resultQuantity).SetRepackaged(item.ED.AttributeFlags.Repackable).Build();
                lista.Add(lootItem);
            }

            return lista;
        }

        public void CheckSubmitConditionsAndThrowIfFailed()
        {
            string.IsNullOrEmpty(Name).ThrowIfTrue(ErrorCodes.ContainerHasNoName);
            Name.Length.ThrowIfLess(3,ErrorCodes.ContainerHasNoName);

            IsInAssignment().ThrowIfTrue(ErrorCodes.ContainerInAssignment);
            IsRepackaged.ThrowIfTrue(ErrorCodes.ContainerHasToBeUnPacked);

            Quantity.ThrowIfNotEqual(1,ErrorCodes.ItemHasToBeSingle);
            GetItems().Any().ThrowIfFalse(ErrorCodes.NoItemsToTransport);
        }

        public override double Volume
        {
            get
            {
                var volume = base.Volume;
                volume += GetItems().Sum(i => i.Volume);
                return volume;
            }
        }

        public void PrintDebug()
        {
            Logger.Info("container owner: " + this.Owner);

            foreach (var item in this.GetItems(true))
            {
                Logger.Info("item owner: " + item.Owner + " " + item.ED.Name);
            }
            Logger.Info("");



        }


    }
}
