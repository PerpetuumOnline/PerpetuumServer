using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;

namespace Perpetuum.Services.Standing
{
    public static class StandingExtensions
    {
        public static double GetStanding(this IStandingHandler standingHandler,long sourceEID, long targetEID)
        {
            double standing;
            standingHandler.TryGetStanding(sourceEID, targetEID, out standing);
            return standing;
        }

        public static IDictionary<string, object> GetStandingForDefaultCorporations(this IStandingHandler standingHandler,Character character)
        {
            return DefaultCorporationDataCache.GetAllDefaultCorporationEid().ToDictionary("s", corporationEID =>
            {
                var standing = standingHandler.GetStanding(corporationEID, character.Eid);
                var entry = new Dictionary<string, object>
                {
                    {k.eid, corporationEID},
                    {k.standing, standing}
                };
                return entry;
            });
        }

        public static IDictionary<string, object> GetStandingForDefaultAlliances(this IStandingHandler standingHandler,Character character)
        {
            return DefaultCorporationDataCache.GetMegaCorporationEids().ToDictionary("s", allianceEID =>
            {
                var standing = standingHandler.GetStanding(allianceEID, character.Eid);
                var entry = new Dictionary<string, object>()
                {
                    {k.eid, allianceEID},
                    {k.standing, standing},
                };
                return entry;
            });
        }

        public static void SendStandingToDefaultCorps(this IStandingHandler standingHandler,Character character)
        {
            //refresh standings
            Message.Builder.SetCommand(Commands.GetStandingForDefaultCorporations)
                .WithData(standingHandler.GetStandingForDefaultCorporations(character))
                .ToCharacter(character)
                .Send();
        }

        public static void SendStandingToDefaultAlliances(this IStandingHandler standingHandler,Character character)
        {
            //refresh standings
            Message.Builder.SetCommand(Commands.GetStandingForDefaultAlliances)
                .WithData(standingHandler.GetStandingForDefaultAlliances(character))
                .ToCharacter(character)
                .Send();
        }

        public static double GetStandingServerEntityToPlayerHierarchy(this IStandingHandler standingHandler,long serverEntityEid, Character character)
        {
            var playersCorporationEid = character.CorporationEid;
            var playersAllianceEid = character.AllianceEid;

            if (serverEntityEid == playersAllianceEid || serverEntityEid == playersCorporationEid)
            {
                //own corp or alliance
                return 10.0;
            }

            double standing;
            //was standing set for this alliance?
            if (standingHandler.TryGetStanding(serverEntityEid, playersAllianceEid, out standing))
            {
                //if yes, then it overrides everything
                return standing;
            }

            //was the standing set for the player's corp?
            if (standingHandler.TryGetStanding(serverEntityEid, playersCorporationEid, out standing))
                return standing;

            //was the standing set this player?
            if (standingHandler.TryGetStanding(serverEntityEid, character.Eid, out standing))
                return standing;

            return 0;
        }
    }
}