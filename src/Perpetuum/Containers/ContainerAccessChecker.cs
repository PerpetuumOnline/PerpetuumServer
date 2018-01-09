using System.Diagnostics;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Units.FieldTerminals;

namespace Perpetuum.Containers
{
    public interface IContainerAccessChecker
    {
        ErrorCodes CheckAccess(Container container);
    }

    public class ContainerAccessChecker : IContainerAccessChecker,
                                          IEntityVisitor<Container>,
                                          IEntityVisitor<PublicContainer>,
                                          IEntityVisitor<CorporateHangar>,
                                          IEntityVisitor<CorporateHangarFolder>,
                                          IEntityVisitor<PublicCorporationHangarStorage>,
                                          IEntityVisitor<VolumeWrapperContainer>,
                                          IEntityVisitor<InfiniteBoxContainer>,
                                          IEntityVisitor<LimitedBoxContainer>,
                                          IEntityVisitor<RobotInventory>,IEntityVisitor<SystemContainer>
    {
        private readonly Character _character;
        private readonly ContainerAccess _access;
        private ErrorCodes _error = ErrorCodes.NoError;

        private ContainerAccessChecker(Character character, ContainerAccess access)
        {
            _character = character;
            _access = access;
        }

        private ErrorCodes CheckDockedState(Container container)
        {
            if (container.GetOrLoadParentEntity() is FieldTerminal fieldTerminal)
            {
                //NOS, ez itt hack, csak hogy egyelore lehessen tovabblepni
                //no docked state check
                return ErrorCodes.NoError;
            }

            //only docked character is allowed to relocate to corporate hangar
            if (!_character.IsDocked)
                return ErrorCodes.CharacterHasToBeDocked;

            //check structure eid VS docked state
            if ( _character.CurrentDockingBaseEid != container.StructureRoot )
                return ErrorCodes.ItemOutOfRange;

            return ErrorCodes.NoError;
        }

        public void Visit(Container container)
        {
            //is the container is in a stack?
            if (container.Quantity > 1)
            {
                _error = ErrorCodes.ItemQuantityHasToBeOne;
                return;
            }

            //is it packed?
            if (container.IsRepackaged)
                _error = ErrorCodes.ItemHasToBeUnpacked;
        }

        public void Visit(CorporateHangar hangar)
        {
            Visit((Container)hangar);

            if ( _error != ErrorCodes.NoError )
                return;

            if (hangar.IsLeaseExpired && _access != ContainerAccess.LogList)
            {
                _error = ErrorCodes.CorporationHangarLeaseExpired;
                return;
            }

            if (!(_access == ContainerAccess.LogList ||_access == ContainerAccess.LogClear || _access == ContainerAccess.LogStart || 
                  _access == ContainerAccess.List || _access == ContainerAccess.LogStop)) 
            {
                _error = CheckDockedState(hangar);
                if ( _error != ErrorCodes.NoError )
                    return;
            }

            //is the owner of this container is the corporation the character is a member of?
            var corpEid = _character.CorporationEid;

            if (DefaultCorporationDataCache.IsCorporationDefault(corpEid))
            {
                _error = ErrorCodes.CharacterMustBeInPrivateCorporation;
                return;
            }

            if (!Corporation.Exists(corpEid))
            {
                Logger.Error("a character found with a non-existing corporationEID. characterID:" + _character + " corpEID:" + corpEid);
                _error = ErrorCodes.InsufficientPrivileges; //no such corporation
                return;
            }

            if (hangar.Owner != corpEid)
            {
                _error = ErrorCodes.InsufficientPrivileges;
                return;
            }

            //if his corporation owns this container
            //check role
            var memberRole = Corporation.GetRoleFromSql(_character);
            if ( !hangar.HasAccess(memberRole,_access) )
                _error = ErrorCodes.InsufficientPrivileges;
        }

        public void Visit(CorporateHangarFolder folder)
        {
            var parentEntity = folder.GetOrLoadParentEntity();
            Debug.Assert(parentEntity != null, "folder.ParentEntity != null");
            parentEntity.AcceptVisitor(this);
        }

        public void Visit(VolumeWrapperContainer container)
        {
            switch (_access)
            {
                case ContainerAccess.List:
                {
                    if (_character != container.PrincipalCharacter)
                    {
                        _error = ErrorCodes.OnlyPrincipalAllowed;
                        return;
                    }
                    break;
                }
                case ContainerAccess.Add:
                case ContainerAccess.Remove:
                case ContainerAccess.Delete:
                {
                    if (_character != container.PrincipalCharacter)
                    {
                        _error = ErrorCodes.OnlyPrincipalAllowed;
                        return;
                    }

                    var containerParent = container.GetOrLoadParentEntity();
                    if (containerParent is LimitedBoxContainer || containerParent is DefaultSystemContainer)
                    {
                        _error = ErrorCodes.ItemRelocateFails;
                        return;
                    }
                    break;
                }
            }

            Visit((Container)container);
        }

        public void Visit(InfiniteBoxContainer container)
        {
            Visit((Container)container);
            _error = CheckDockedState(container);
        }

        public void Visit(LimitedBoxContainer container)
        {
            Visit((Container)container);
            _error = CheckDockedState(container);
        }

        public void Visit(PublicContainer container)
        {
            Visit((Container)container);
            _error = CheckDockedState(container);
        }

        public void Visit(PublicCorporationHangarStorage storage)
        {
            Visit((Container)storage);
            _error = CheckDockedState(storage);
        }

        public void Visit(RobotInventory inventory)
        {
            _error = inventory.CheckParentRobot(_character.Eid);

            if ( _error != ErrorCodes.NoError )
                return;

            Visit((Container)inventory);
        }

        public void Visit(SystemContainer container)
        {
            _error = ErrorCodes.InsufficientPrivileges; //never ever
        }

        public ErrorCodes CheckAccess(Container container)
        {
            container.AcceptVisitor(this);
            return _error;
        }

        public static IContainerAccessChecker Create(Character character, ContainerAccess access)
        {
            return new ContainerAccessChecker(character, access);
        }
    }
}