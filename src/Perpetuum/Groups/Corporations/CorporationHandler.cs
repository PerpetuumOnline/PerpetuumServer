using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Perpetuum.Log;
using Perpetuum.Zones;

namespace Perpetuum.Groups.Corporations
{
    [Serializable]
    public enum CorporationCommand
    {
        TransferMember,
        ChangeRole,Close
    }

    public class CorporationHandler
    {
        private readonly IZone _zone;
        private readonly ConcurrentDictionary<long, Corporation> _corporations = new ConcurrentDictionary<long, Corporation>();

        public delegate CorporationHandler Factory(IZone zone);

        public CorporationHandler(IZone zone)
        {
            _zone = zone;
            Logger.Info("corporationHandler starting." + zone);
        }

        private Corporation GetOrAddCorporation(long corporationEid)
        {
            Debug.Assert(corporationEid > 0);
            return _corporations.GetOrAdd(corporationEid, _ => Corporation.GetOrThrow(corporationEid));
        }

        public void HandleCorporationCommand(CorporationCommand corporationCommand, Dictionary<string, object> data)
        {
            var role = CorporationRole.NotDefined;
            int characterId;
            long corporationEid;

            if (corporationCommand == CorporationCommand.Close)
            {
                corporationEid = (long)data[k.corporationEID];
                _corporations.Remove(corporationEid);
                Logger.DebugInfo($"corp removed from cache:{corporationEid} zone:{_zone.Id}");
                return;
            }

            switch (corporationCommand)
            {
                case CorporationCommand.ChangeRole:

                    characterId = (int)data[k.characterID];
                    role = (CorporationRole)(int)data[k.role];
                    corporationEid = (long)data[k.corporationEID];

                    var corporation = GetOrAddCorporation(corporationEid);
                    corporation.cache_addOrUpdateMember(characterId, role);

                    Logger.DebugInfo($"role changed on zone:{_zone.Id} character:{characterId} role:{role}");
                    break;

                case CorporationCommand.TransferMember:
                {
                    characterId = (int)data[k.characterID];
                    var fromEid = (long)data[k.from];
                    var toEid = (long)data[k.to];


                    if (!DefaultCorporationDataCache.IsCorporationDefault(fromEid))
                    {
                        var fromCorporation = GetOrAddCorporation(fromEid);
                        fromCorporation.cache_removeMember(characterId);
                        Logger.DebugInfo($"character removed from corp on zone:{_zone.Id} characterid:{characterId} corporationEID:{fromEid}");
                    }

                    if (!DefaultCorporationDataCache.IsCorporationDefault(toEid))
                    {
                        var toCorporation = GetOrAddCorporation(toEid);
                        toCorporation.cache_addOrUpdateMember(characterId, role);
                        Logger.DebugInfo($"character added to corp on zone:{_zone.Id} characterid:{characterId} corporationEID:{toEid}");
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(corporationCommand));
            }
        }
    }
}
