using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS
{
    /// <summary>
    /// Using this module player can build up a pbs node
    /// </summary>
    public class ConstructionModule : ActiveModule
    {
        public ConstructionModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
        }

        protected override void OnAction()
        {
            DoConstruct();
            ConsumeAmmo();
        }

        private void DoConstruct()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            var pbsObject = (unitLock.Target as IPBSObject).ThrowIfNull(ErrorCodes.DefinitionNotSupported);

            //itt epit vagy unepit
            var ammo = GetAmmo();

            var constructionAmount = ammo.GetPropertyModifier(AggregateField.construction_charge_amount);

            if (constructionAmount.Value < 0)
            {
                //deconstructhoz access kell
                pbsObject.CheckAccessAndThrowIfFailed(ParentRobot.GetCharacter());

                pbsObject.ReinforceHandler.CurrentState.IsReinforced.ThrowIfTrue(ErrorCodes.NotPossibleDuringReinforce);
                
                var dockingBase = pbsObject as PBSDockingBase;
                dockingBase?.IsDeconstructAllowed().ThrowIfError();

                pbsObject.OnlineStatus.ThrowIfTrue(ErrorCodes.NotPossibleOnOnlineNode);
            }

            pbsObject.ModifyConstructionLevel((int) constructionAmount.Value).ThrowIfError();
           

            CreateBeam(unitLock.Target, BeamState.AlignToTerrain);
        }
    }
}
