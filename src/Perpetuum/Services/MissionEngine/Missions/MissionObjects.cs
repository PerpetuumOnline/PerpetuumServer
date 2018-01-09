using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Items;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public class MissionStandingChange
    {
        public readonly long allianceEid;
        public readonly double change;

        public static  MissionStandingChange FromRecord(IDataRecord record)
        {
            var allianceEid = record.GetValue<long>(k.allianceEID.ToLower());
            var change = record.GetValue<double>(k.change);

            return new MissionStandingChange(allianceEid,change);

        }

        public MissionStandingChange(long allianceEid, double change)
        {
            this.allianceEid = allianceEid;
            this.change = change;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.allianceEID, allianceEid},
                {k.amount, change}
            };
        }
    }


    public class MissionReward
    {
        public ItemInfo ItemInfo { get; private set; }
        public int Probability { get; private set; }

        public MissionReward(ItemInfo itemInfo)
        {
            Probability = 100;
            ItemInfo = itemInfo;
        }

        private MissionReward(int definition, int quantity, int probability)
        {
            ItemInfo = new ItemInfo(definition, quantity);
            Probability = probability;
        }


        public static MissionReward FromRecord(IDataRecord record)
        {
            var definition = record.GetValue<int>(k.definition);
            var quantity = record.GetValue<int>(k.quantity);
            var probability = record.GetValue<int>(k.probability);

            return new MissionReward(definition,quantity,probability);
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.definition, ItemInfo.Definition},
                {k.quantity, ItemInfo.Quantity},
                {k.probability, Probability}
            };
        }
    }


    public class MissionIssuer
    {
        public readonly long corporationEid;
        public readonly long allianceEid;

        public MissionIssuer(IDataRecord record)
        {
            corporationEid = record.GetValue<long>(k.corporationEID.ToLower());
            allianceEid = record.GetValue<long>(k.allianceEID.ToLower());
        }
    }

    // %%% ezt a dolgot teljesen at kene alakitani. Most nincs hasznalva
    public class MissionStandingRequirement
    {
        private readonly long _corporationEid;
        private readonly bool _standingAbove;
        private readonly double _standingThreshold;

        public MissionStandingRequirement(IDataRecord record)
        {
            _corporationEid = record.GetValue<long>(k.corporationEID.ToLower());
            _standingAbove = record.GetValue<bool>(k.standingAbove.ToLower());
            _standingThreshold = record.GetValue<double>(k.standingThreshold.ToLower());
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.corporationEID, _corporationEid},
                {k.standingAbove, _standingAbove ? 1 : 0},
                {k.standingThreshold, _standingThreshold}
            };
        }

        public bool CheckStanding(Character character)
        {
            return true;
        }
    }


    [Serializable]
    public struct MissionProgressUpdate
    {
        public Character character;
        public int missionId;
        public Guid missionGuid;
        public int targetOrder;
        public bool isFinished;
        public int missionLevel;
        public int locationId;
        public int selectedRace;
        public bool spreadInGang;


        public override string ToString()
        {
            return "missionProgressUpdate characterID:" + character.Id + " missionID:" + missionId + " TargetOrder:" + targetOrder + " isFinished:" + isFinished + " lvl:" + missionLevel + " loc:" + locationId;
        }
    }
}
