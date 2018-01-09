using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.MissionBonusObjects
{
    public class MissionBonus
    {
        public Character character;
        private readonly MissionCategory _missionCategory;
        private readonly int _missionLevel;
        private readonly int _agentId;
        private int _bonus;
        public DateTime lastModified;

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public MissionBonus(Character character, MissionCategory missionCategory, int missionLevel,  MissionAgent agent, int bonus)
        {
            this.character = character;
            _missionCategory = missionCategory;
            _missionLevel = missionLevel;
            _agentId = agent.id;
            Bonus = bonus;

            lastModified = DateTime.Now;
        }

        public static MissionBonus FromRecrod(IDataRecord record)
        {
            var character = Character.Get(record.GetValue<int>("characterid"));
            var category = (MissionCategory)record.GetValue<int>("missioncategory");
            var level = record.GetValue<int>("missionlevel");
            var agentId = record.GetValue<int>("agentid");
            var bonus = record.GetValue<int>("bonus");

            var agent = _missionDataCache.GetAgent(agentId);

            var mb = new MissionBonus(character, category, level, agent, bonus);

            return mb;
        }





        public override string ToString()
        {
            return "missionBonus characterId:" + character.Id + " category:" + _missionCategory + " level:" + _missionLevel + " agentId:" + _agentId + " mult:" + BonusMultiplier + " timeoutSeconds:" + GetBonusTimeSeconds();
        }

        public long Key
        {
            get
            {
                return GetKey(_missionCategory, _missionLevel, _agentId);
            }
             
        }

        public int Bonus
        {
            get { return _bonus;  }
            private set
            {
                _bonus = value.Clamp(-5,5);
                lastModified = DateTime.Now;
            }
        }


        public static readonly Dictionary<int, double> BonusMultipliers = new Dictionary<int, double>()
            {
                {-5, 0},
                {-4, 0.2},
                {-3, 0.4},
                {-2, 0.6},
                {-1, 0.8},
                {0, 1.0},
                {1, 1.2},
                {2, 1.4},
                {3, 1.6},
                {4, 1.8},
                {5, 2.0},
            };
        

        public double BonusMultiplier
        {
            get
            {
                var mult =  BonusMultipliers[_bonus];
                var extBonus = character.GetExtensionBonusByName(ExtensionNames.MISSION_BONUS_LEVEL_MOD) +1;
                
                mult *=  extBonus ;
                
                return mult;
            }
            
        }

        public double RawMultiplier
        {
            get { return BonusMultipliers[_bonus]; }
        }


        public static long GetKey(MissionCategory missionCategory, int missionLevel, int agentId)
        {
            return (int)missionCategory | (missionLevel << 8) | (agentId << 16);
        }

        public void SaveToDb()
        {
            Db.Query().CommandText("missionBonusWrite")
                    .SetParameter("@characterId", character.Id)
                    .SetParameter("@missionCategory", (int) _missionCategory)
                    .SetParameter("@missionLevel", _missionLevel)
                    .SetParameter("@agentId", _agentId)
                    .SetParameter("@bonus", Bonus)
                    .ExecuteNonQuery();


        }

        public void Timeout()
        {
            var bonusPre = Bonus;

            if (Bonus > 0)
            {
                Bonus--;
            }
            else if (Bonus < 0)
            {
                Bonus++;
            }
            
            Logger.Info("bonus timeout " + bonusPre + " > " + Bonus + " " + this);
        }



        public void AdvanceBonus()
        {
            var bonusPre = Bonus;

            if (Bonus < 0)
            {
                Bonus = 0;
            }
            else
            {
                Bonus++;
            }

            Logger.Info("bonus advanced " + bonusPre + " > " + Bonus + " " + this);
        }

        public bool DecreaseBonus()
        {
            var bonusPre = Bonus;

            if (Bonus > 0)
            {
                Bonus = 0;
            }
            else
            {
                Bonus = Bonus - 1;
            }

            Logger.Info("bonus decreased " + bonusPre + " > " + Bonus + " " + this);

            var wasChange = bonusPre != Bonus;

            return wasChange;

        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>()
                {
                    {k.missionCategory, (int) _missionCategory},
                    {k.missionLevel, _missionLevel},
                    {k.agentID, _agentId},
                    {k.bonus, Bonus},
                    {k.expire, lastModified.AddSeconds(GetBonusTimeSeconds()) }
                };
        }


        public void SendUpdateToClient()
        {
            Message.Builder.SetCommand(Commands.MissionBonusUpdate)
                .WithData(ToDictionary())
                .ToCharacter(character)
                .Send();
        }

        private const int BONUS_EXPIRY_SECONDS = 3600; //one hour

        private double GetBonusTimeMultiplier()
        {
            var bonusTimeMultiplier = character.GetExtensionBonusByName(ExtensionNames.MISSION_BONUS_TIME_MOD);
            return bonusTimeMultiplier;

        }

        public double GetBonusTimeSeconds()
        {
            var extensionMultiplier = GetBonusTimeMultiplier();
            var finalMultiplier = 1.0;
            if (Bonus < 0)
            {
                finalMultiplier = 1.0 - (extensionMultiplier * 0.9);
            }
            else
            {
                finalMultiplier = 1.0 + extensionMultiplier;
            }

            return finalMultiplier * BONUS_EXPIRY_SECONDS;
        }


    }


}
