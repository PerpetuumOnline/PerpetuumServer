using System;
using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Players;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{

    /// <summary>
    /// 
    /// Returns kiosk info
    /// 
    /// </summary>
    public class Kiosk : MissionStructure
    {
        public Kiosk() : base(MissionTargetType.submit_item)
        {
        }

        public Dictionary<string, object> GetKioskInfo(Player player, Guid guid)
        {
            var info = BaseInfoToDictionary();

            info.Add(k.mission, player.MissionHandler.GetKioskMissionInfo(this, guid));

            //.... itt meg johet mindenfele pl mennyiert veszi a plasmat, stb

            // ... 

            // info.add(k.valami, player.GetKioskValamiInfo(this)); 

            return info;
        }


        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }
    }

}
