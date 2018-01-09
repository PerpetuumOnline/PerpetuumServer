using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{

    public class DeliveryHelper
    {
        private readonly MissionDataCache _missionDataCache;
        private readonly MissionProcessor _missionProcessor;

        public bool wasChange;
        public int missionId;
        public int targetId;
        public int quantity;
        public int locationId;
        public int definition;
        public Character missionOwnerCharacter; //the owner of the mission target
        public Character assisting; //the character who actually did the delivery
        public Guid missionGuid;

        public delegate DeliveryHelper Factory();

        public DeliveryHelper(MissionDataCache missionDataCache,MissionProcessor missionProcessor)
        {
            _missionDataCache = missionDataCache;
            _missionProcessor = missionProcessor;
        }

        private int _progressCount;

        public int ProgressCount
        {
            get { return _progressCount; }
            set
            {
                wasChange = true;
                _progressCount = value;
            }
        }

        public bool IsQuantityMissing
        {
            get { return ProgressCount < quantity; }
        }

        public bool IsCompleted
        {
            get { return ProgressCount >= quantity; }
        }


        /// <summary>
        /// info for the client about quantity missing
        /// </summary>
        public Dictionary<string, object> MissingInfo
        {
            get
            {
                var result = new Dictionary<string, object>()
                {
                    {k.guid, missionGuid.ToString()},
                    {k.targetID, targetId},
                    {k.characterID, missionOwnerCharacter.Id},
                    {k.definition, definition},
                    {k.missing, quantity - ProgressCount},
                    {k.missionID, missionId},
                };

                return result;
            }
        }

        /// <summary>
        /// info for the client about a completed target
        /// </summary>
        public Dictionary<string, object> CompletedInfo
        {
            get
            {
                var result = new Dictionary<string, object>()
                {
                    {k.guid, missionGuid.ToString()},
                    {k.targetID, targetId},
                    {k.characterID, missionOwnerCharacter.Id},
                    {k.definition, definition},
                    {k.quality, quantity},
                    {k.location, locationId},
                    {k.missionID, missionId},
                    
                };

                return result;
            }
        }

        /// <summary>
        /// Informs the mission engine that a certain target is delivered
        /// </summary>
        public void EnqueueProgressInfo()
        {
            _missionProcessor.EnqueueMissionTargetAsync(missionOwnerCharacter,MissionTargetType.fetch_item, AddKeys);
        }

        public void AddKeys(IDictionary<string,object> dictionary)
        {
            dictionary.Add(k.guid,missionGuid);
            dictionary.Add(k.definition,definition);
            dictionary.Add(k.progressCount,ProgressCount);
            dictionary.Add(k.completed,IsCompleted);
            dictionary.Add(k.location,locationId);
            dictionary.Add(k.missionID,missionId);
            dictionary.Add(k.targetID,targetId);
            dictionary.Add(k.assistingCharacterID,assisting.Id);
            dictionary.Add(k.useGang,1);

            if (IsCompleted)
            {
                MissionLocation location;
                if (_missionDataCache.GetLocationById(locationId,out location))
                {
                    dictionary.Add(k.zoneID,location.ZoneConfig.Id);
                    dictionary.Add(k.position,location.MyPosition);
                }
            }
        }
    }

}
