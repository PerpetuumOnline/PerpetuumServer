using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;

namespace Perpetuum.Containers
{
    /// <summary>
    /// Container Base
    /// </summary>
    public partial class Container : Item
    {
        private ContainerLogger _logger;

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public ContainerLogger ContainerLogger
        {
            get { return LazyInitializer.EnsureInitialized(ref _logger, () => new ContainerLogger(this)); }
        }

        public long StructureRoot
        {
            get
            {
                //look up the structure's root
                if (!DynamicProperties.Contains(k.structureRoot))
                {
                    DynamicProperties.Update(k.structureRoot,TraverseForStructureRootEid());
                    this.Save();
                }

                return DynamicProperties.GetOrAdd<long>(k.structureRoot);
            }
        }

        public long TraverseForStructureRootEid()
        {
            if (Parent == 0)
                return 0L;

            var record = Db.Query().CommandText("getStructureRoot")
                                 .SetParameter("@eid", Eid)
                                 .ExecuteSingleRow();

            var rootEid = record.GetValue<long>(0);
            var rootDefinition = record.GetValue<int>(1);

            if (rootEid == 0L)
                return 0L; //safety

            //root found
            return EntityDefault.Get(rootDefinition).CategoryFlags.IsCategory(CategoryFlags.cf_structures) || EntityDefault.Get(rootDefinition).CategoryFlags.IsCategory(CategoryFlags.cf_pbs_docking_base) ? rootEid : 0L;
        }


        protected void AddLogEntry(Character character, ContainerAccess access,int definition = 0,int quantity = 0)
        {
            if (!IsLogging())
                return;

            ContainerLogger.AddLogEntry(character,access,definition,quantity);
        }

        public bool IsLogging()
        {
            return DynamicProperties.GetOrAdd<int>(k.log) == 1;
        }

        public virtual void SetLogging(bool state, Character character, bool writeLog = false)
        {
            if (IsLogging() == state)
                return;

            DynamicProperties.Update(k.log,(state) ? 1 : 0);

            if (writeLog && character != Character.None )
            {
                ContainerLogger.AddLogEntry(character, state ? ContainerAccess.LogStart : ContainerAccess.LogStop);
            }
        }

        public override void OnSaveToDb()
        {
            if (_logger != null)
                ContainerLogger.SaveToDb();

            base.OnSaveToDb();
        }

        public void CheckAccessAndThrowIfFailed(Character character, ContainerAccess containerAccess)
        {
            CheckAccess(character, containerAccess).ThrowIfError();
        }

        public ErrorCodes CheckAccess(Character character, ContainerAccess access)
        {
            var checker = ContainerAccessChecker.Create(character, access);
            return checker.CheckAccess(this);
        }

        public void ReloadItems(Character character)
        {
            ReloadItems(character.Eid);
            this.Initialize(character);
        }

        public virtual void ReloadItems(long? ownerEid)
        {
            var children = Repository.LoadByOwner(Eid, ownerEid);
            RebuildTree(children);
        }

        public IEnumerable<Item> GetItems(IEnumerable<long> itemEids)
        {
            if (itemEids == null) 
                yield break;

            foreach (var itemEid in itemEids)
            {
                var item = GetItem(itemEid);
                if (item != null)
                    yield return item;
            }
        }

        public IEnumerable<Item> GetItems(bool allItemsInFullTree = false)
        {
            if ( allItemsInFullTree )
                return GetFullTree().OfType<Item>();

            return Children.OfType<Item>();
        }

        [NotNull]
        public Item GetItemOrThrow(long itemEid, bool searchInFullTree = false)
        {
            return GetItem(itemEid, searchInFullTree).ThrowIfNull(ErrorCodes.ItemNotFound);
        }

        [CanBeNull]
        public Item GetItem(long itemEid,bool searchInFullTree = false)
        {
            return GetItems(searchInFullTree).FirstOrDefault(i => i.Eid == itemEid);
        }

        public void AddItem(Item item,bool doStack)
        {
            AddItem(item, item.Owner, doStack);
        }

        public virtual void AddItem(Item item,long issuerEid,bool doStack)
        {
            Item targetItem = null;
            
            // ha nem lehet stackelni akkor nem eroltetjuk siman csak add lesz
            if (doStack && item.IsStackable)
            {
                targetItem = GetItems().FirstOrDefault(i => item.CanStackTo(i) == ErrorCodes.NoError);
            }

            // talalt?
            if (targetItem != null)
            {
                item.StackTo(targetItem);
                return;
            }

            item.Owner = issuerEid;
            AddChild(item);
        }

        public void RemoveItemOrThrow(Item item)
        {
            RemoveItem(item).ThrowIfNull(ErrorCodes.ItemNotFound);
        }

        [CanBeNull]
        public Item RemoveItem(Item item)
        {
            return RemoveItem(item, item.Quantity);
        }

        [CanBeNull]
        public Item RemoveItem(Item item, int quantity)
        {
            if (item.Parent != Eid)
                return null;

            var resultItem = item;
            if (item.Quantity > quantity)
                resultItem = item.Unstack(quantity);

            RemoveChild(resultItem);
            return resultItem;
        }

        //ammo loados remove - ez van a terepen
        public int RemoveItemByDefinition(int definition, int requestedQuantity)
        {
            var resultedQuantity = 0;
            var ed = EntityDefault.Get(definition).ThrowIfEqual(EntityDefault.None,ErrorCodes.DefinitionNotSupported);
            ed.AttributeFlags.AlwaysStackable.ThrowIfFalse(ErrorCodes.DefinitionNotSupported);

            var items = GetItems().Where(c => c.Definition == definition).OrderByDescending(c => c.Quantity);

            foreach (var item in items)
            {
                var quantity = item.Quantity;
                var remainQty = requestedQuantity - resultedQuantity;

                if (quantity <= remainQty)
                {
                    RemoveItemOrThrow(item);
                    // delete
                    resultedQuantity += quantity;
                    Repository.Delete(item);
                }
                else
                {
                    // update
                    resultedQuantity += remainQty;
                    item.Quantity -= remainQty;
                }

                if (resultedQuantity >= requestedQuantity)
                    break;
            }

            return resultedQuantity;
        }

        [CanBeNull]
        public Item GetAndRemoveItemByDefinition(int definition, int requestedQuantity)
        {
            var q = RemoveItemByDefinition(definition, requestedQuantity);
            if (q == 0)
                return null;

            var resultItem = Factory.CreateWithRandomEID(definition);
            resultItem.Quantity = q;
            return (Item) resultItem;
        }

        protected virtual void RelocateItem(Character character, long issuerEid, Item item, Container targetContainer)
        {
            //user can't move it)
            item.ED.AttributeFlags.NonRelocatable.ThrowIfTrue(ErrorCodes.ItemNotRelocatable);

            //add to container
            // ebbol a kontenerbol kikerult,ha sikerul a targetbe pakolni!
            targetContainer.AddItem(item,issuerEid, true);

            //item left his set or a new item added to the his set
            if (IsPersonalContainer != targetContainer.IsPersonalContainer)
            {
                var b = TransactionLogEvent.Builder();

                if (IsPersonalContainer)
                {
                    b.SetTransactionType(TransactionType.ItemDonate).SetContainer(targetContainer);
                }
                else
                {
                    b.SetTransactionType(TransactionType.ItemObtain).SetContainer(this);
                }

                character.LogTransaction(b);
            }

            //corp hangar and folder hierarchy check, other things if needed.
            if (IsLogSkipped(targetContainer)) 
                return;

            targetContainer.AddLogEntry(character,ContainerAccess.Add,item.Definition,item.Quantity);
            AddLogEntry(character,ContainerAccess.Remove,item.Definition,item.Quantity);
        }

        public void RelocateItems(Character character,Character issuer,IEnumerable<long> itemEids, Container targetContainer)
        {
            var items = GetItems().Where(i => itemEids.Contains(i.Eid));
            RelocateItems(character, issuer, items, targetContainer);
        }

        public void RelocateItems(Character character,Character issuer, IEnumerable<Item> items, Container targetContainer)
        {
            var itemArray = items.ToArray();

            if (itemArray.Length == 1)
            {
                RelocateItem(character, issuer.Eid, itemArray[0], targetContainer);
            }
            else
            {
                using (var n = new ItemErrorNotifier(true))
                {
                    foreach (var item in itemArray)
                    {
                        try
                        {
                            RelocateItem(character, issuer.Eid, item, targetContainer);
                        }
                        catch (PerpetuumException gex)
                        {
                            n.AddError(item, gex);
                        }
                    }
                }
            }
        }


        public void UnstackItem(long itemEid, Character issuer, int amount, int size, Container targetContainer)
        {
            size.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            amount.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var sourceItem = GetItemOrThrow(itemEid);

            if (amount * size == sourceItem.Quantity)
            {
                //unstack A containerbol Bbe 400 ammobol 1db 400as csomagot ... inkabb relocateljen
                amount.ThrowIfLessOrEqual(1, ErrorCodes.UnstackNotPossibleUseRelocate);
                //100at szeretne 2db 50esre -> 1db 50es + 50 maradek
                amount--;
            }

            var sumQty = 0;

            for (var i = 0; i < amount; i++)
            {
                if (sourceItem.Quantity < size)
                    continue;

                var unstackedItem = sourceItem.Unstack(size);

                targetContainer.AddItem(unstackedItem, issuer.Eid, false);

                sumQty += size;
            }

            if (sumQty <= 0 || targetContainer.Equals(this)) 
                return;

            if (IsLogSkipped(targetContainer)) 
                return;

            targetContainer.AddLogEntry(issuer, ContainerAccess.Add, sourceItem.Definition, sumQty);
            AddLogEntry(issuer, ContainerAccess.Remove, sourceItem.Definition, sumQty);
        }

        private bool IsLogSkipped(Container targetContainer)
        {
            if (this is CorporateHangar || this is CorporateHangarFolder || targetContainer is CorporateHangar || targetContainer is CorporateHangarFolder)
            {
                var parentEntity = GetOrLoadParentEntity();
                var targetParentEntity = targetContainer.GetOrLoadParentEntity();

                if ((parentEntity != null && parentEntity.Eid == targetContainer.Eid) || (targetParentEntity != null && targetParentEntity.Eid == Eid))
                {
                    //same hierarchy, skip logging
                    return true;
                }

                if (parentEntity != null && targetParentEntity != null && parentEntity.Eid == targetParentEntity.Eid)
                {
                    //same parent folder
                    return true;
                }
            }

            return false;
        }

        public void TrashItems(Character issuerCharacter, IEnumerable<long> target)
        {
            var trashableItems = GetItems(target).Where(i => i.IsTrashable).ToList();

            using (var n = new ItemErrorNotifier(true))
            {
                foreach (var item in trashableItems)
                {
                    try
                    {
                        //selected robot now allowed
                        issuerCharacter.IsRobotSelectedForCharacter(item as Robot).ThrowIfTrue(ErrorCodes.RobotMustBeDeselected);

                        RemoveItemOrThrow(item);
                        Repository.Delete(item);

                        AddLogEntry(issuerCharacter, ContainerAccess.Delete, item.Definition, item.Quantity);
                        var b = TransactionLogEvent.Builder()
                                                   .SetTransactionType(TransactionType.TrashItem)
                                                   .SetCharacter(issuerCharacter)
                                                   .SetContainer(this)
                                                   .SetItem(item.Definition, item.Quantity);
                        issuerCharacter.LogTransaction(b);
                    }
                    catch (PerpetuumException gex)
                    {
                        Logger.Error("Failed to delete item:" + item.Eid + " " + item.ED.Name + " owner:" + item.Owner + " issuerCharacter:" + issuerCharacter.Id);
                        n.AddError(item,gex);
                    }
                }
            }
        }

        /// <summary>
        /// This function filters the items for renaming
        /// </summary>
        public virtual void SetItemName(long itemEid, string newName)
        {
            var item = GetItemOrThrow(itemEid);

            item.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

            if (item.IsCategory(CategoryFlags.cf_robots) ||
                item.IsCategory(CategoryFlags.cf_documents) ||
                item.IsCategory(CategoryFlags.cf_limited_capacity_box) ||
                item.IsCategory(CategoryFlags.cf_infinite_capacity_box) ||
                item.IsCategory(CategoryFlags.cf_corporate_hangar_folder) ||
                item.IsCategory(CategoryFlags.cf_scan_result) ||
                item.IsCategory(CategoryFlags.cf_volume_wrapper_container))
            {
                item.Name = newName;
                return;
            }

            throw new PerpetuumException(ErrorCodes.AccessDenied);
        }

        public override void OnDeleteFromDb()
        {
            HasChildren.ThrowIfTrue(ErrorCodes.ContainerHasToBeEmpty);
            ContainerLogger.ClearLog(null);
            base.OnDeleteFromDb();
        }


        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.items, GetItems().ToDictionary("c", i => i.ToDictionary()));
            result.Add(k.log, IsLogging());
            return result;
        }

        public bool RemoveItemFromTree(Item item)
        {
            if (RemoveItem(item) != null)
                return true;

            foreach (var i in GetItems())
            {
                var container = i as Container;

                if ( container != null && !container.IsRepackaged)
                {
                    if (container.RemoveItemFromTree(item))
                        return true;
                }

                var robot = i as Robot;

                if (robot == null || robot.IsRepackaged) 
                    continue;

                var robotInventory = robot.GetContainer();

                if (robotInventory == null) 
                    continue;

                if (robotInventory.RemoveItemFromTree(item))
                    return true;
            }

            return false;
        }

        protected virtual bool IsPersonalContainer
        {
            get { return true; }
        }

        public IEnumerable<Item> SelectDamagedItems(IEnumerable<long> targetEids)
        {
            foreach (var targetEid in targetEids)
            {
                var item = GetItem(targetEid,true);
                if ( item == null )
                    continue;

                if (item.IsDamaged)
                    yield return item;
            }
        }

        public Item CreateAndAddItem(ItemInfo itemInfo, Action<Item> action = null)
        {
            return CreateAndAddItem(itemInfo, itemInfo.IsRepackaged, action);
        }

        public Item CreateAndAddItem(ItemInfo itemInfo,bool doStack, Action<Item> action = null)
        {
            var item = CreateWithRandomEid(itemInfo);

            action?.Invoke(item);

            AddItem(item, doStack);
            this.Save();
            return item;
        }

        public Item CreateAndAddItem(int definition,bool doStack,Action<Item> action = null)
        {
            var item = (Item)Factory.CreateWithRandomEID(definition);

            action?.Invoke(item);

            AddItem(item, doStack);
            this.Save();
            return item;
        }

        public T CreateAndAddItem<T>(int definition,bool doStack,Action<T> action) where T:Item
        {
            return (T)CreateAndAddItem(definition, doStack, item => action((T)item));
        }
    }
}
