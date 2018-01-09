using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;

namespace Perpetuum.Units.FieldTerminals
{
 
    public class FieldTerminal : Unit , IUsableItem
    {
        private readonly MissionDataCache _missionDataCache;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public FieldTerminal(MissionDataCache missionDataCache,DockingBaseHelper dockingBaseHelper)
        {
            _missionDataCache = missionDataCache;
            _dockingBaseHelper = dockingBaseHelper;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var baseDict = base.ToDictionary();

            baseDict.Add(k.dockRange, 1);
            baseDict.Add(k.welcome, "kamu_welcome");

            try
            {
                var publicContainerInfo = GetPublicContainer().ToDictionary();
                publicContainerInfo.Add(k.noItemsSent, 1);
                baseDict.Add(k.publicContainer, publicContainerInfo);
            }
            catch (Exception)
            {
                Logger.Warning("trouble with field terminal: " + Eid + " but transaction saved");
            }

            return baseDict;
        }

        /// <summary>
        /// Nos itt ezt kell modernizalni, egyelore csak mukodjon valahogy. 
        /// </summary>
        /// <returns></returns>
        public PublicContainer GetPublicContainer()
        {
            return _dockingBaseHelper.GetPublicContainer(this);
        }

        public static string GenerateName(IZone zone)
        {
            var fieldTerminalsOnZone = zone.Units.Count(u => u.IsCategory(CategoryFlags.cf_field_terminal));
            var freshName = "fieldTerminal_z" + zone.Configuration.Id.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "_n" + (fieldTerminalsOnZone + 1).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            return freshName;
        }

        public void UseItem(Player player)
        {
            //itt lesz az amikor raduplaklikkelnek 
            SendInfoToCharacter(player.Character);
        }

        public Dictionary<string,object> GetFieldTerminalInfo()
        {
            var result = ToDictionary();
            // itt meg johet bele mindenfele
            return result;
        }

        public void SendInfoToCharacter(Character character)
        {
            var result = GetFieldTerminalInfo();
            Message.Builder.SetCommand(Commands.FieldTerminalInfo).ToCharacter(character).WithData(result).Send();
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            var location = _missionDataCache.GetLocationByEid(Eid);
            //delete mission related stuff
            location?.DeleteFromDb();

            //delete from zone entities
            zone.UnitService.RemoveDefaultUnit(this,false);

            //remove it from ram, etc...
            base.OnRemovedFromZone(zone);
        }
    }
}
