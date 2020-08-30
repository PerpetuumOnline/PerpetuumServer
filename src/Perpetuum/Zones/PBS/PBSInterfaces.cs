using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Zones.PBS
{
    //ez kell az IPBSCoreConsumer helyett, lehet bele tolteni coret (core toltes celja lehet)
    public interface IPBSAcceptsCore { }

    //ez segit abban, hogy lehessen tolteni core-ral
    public interface ICoreAcceptHandler
    {
    }

    //ez lesz az ami fogyasztja a sajat corejat - periodikusan levon a sajat corejabol
    public interface IPBSUsesCore
    {
        ICoreUseHandler CoreUseHandler { get; }
        
        /// <summary>
        /// can be used to override/manipulate the config CoreConsuption
        /// 
        /// Return false to go on and use config value - Fall back to fix consumption
        /// return true to go use the out value as consumption
        /// 
        /// </summary>
        /// <param name="coreDemand"></param>
        /// <returns></returns>
        bool TryCollectCoreConsumption(out double coreDemand); 
    }

    //ez tud tolteni coret ojanba aki accepteli
    public interface IPBSCorePump
    {
        ICorePumpHandler CorePumpHandler { get; }
    }

    
    /// <summary>
    /// Feedable with items
    /// </summary>
    public interface IPBSFeedable
    {
        void FeedWithItems(Player player, IEnumerable<long> eids);
    }

    public interface IPBSObject 
    {
        IPBSReinforceHandler ReinforceHandler { get; }
        IPBSConnectionHandler ConnectionHandler { get; }

        ErrorCodes ModifyConstructionLevel(int amount,bool force = false); //modullal novesztik
        int ConstructionLevelMax { get; }
        int ConstructionLevelCurrent { get; } //ennyire van felnovesztve

        bool OnlineStatus { get; }
        void SetOnlineStatus(bool state,bool checkNofBase,bool forcedByServer = false);

        void TakeOver(long newOwner);
        bool IsOrphaned { get; set; }

        int ZoneIdCached { get; }

        event Action<Unit,bool /*orphanedState*/> OrphanedStateChanged;

        void SendNodeUpdate(PBSEventType eventType = PBSEventType.nodeUpdate);
    }

    public interface IPBSReinforceHandler
    {
        void SetReinforceOffset(Character issuer, int offset);
        [CanBeNull] DateTime? ReinforceEnd { get; }
        DateTime GetReinforceDetails();
        int ReinforceOffsetHours { get; }
        int ReinforceCounter { get; set; }
        void ForceDailyOffset(int forcedOffsetWithinDay);
        [CanBeNull] IReinforceState CurrentState { get; }
    }

    public static class PBSObjectExtensions
    {
        public static bool IsFullyConstructed(this IPBSObject pbsObject)
        {
            return pbsObject.ConstructionLevelCurrent >= pbsObject.ConstructionLevelMax;
        }

        public static bool IsReinforced(this IPBSObject  reinforcable)
        {
            return reinforcable.ReinforceHandler.CurrentState.IsReinforced;
        }

        public static void CheckAccessAndThrowIfFailed(this IPBSObject pbsObject, Character issuer)
        {
            var unit = pbsObject as Unit;
            if (unit == null)
                return;

            CheckAccessAndThrowIfFailed(unit.Owner, issuer);
        }

        public static void CheckAccessAndThrowIfFailed<T>(this T pbsObject, Character issuer) where T : Unit,IPBSObject
        {
            CheckAccessAndThrowIfFailed(pbsObject.Owner,issuer);
        }

        private static void CheckAccessAndThrowIfFailed(long owner,Character issuer)
        {
            var corporation = issuer.GetPrivateCorporationOrThrow();

            corporation.Eid.ThrowIfNotEqual(owner,ErrorCodes.OwnerMismatch);

            corporation.GetMemberRole(issuer).IsAnyRole(
                CorporationRole.DeputyCEO,
                CorporationRole.CEO,
                CorporationRole.editPBS
            ).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

        }

        public static int GetBandwidthUsage(this IPBSObject o)
        {
            var unit = o as Unit;
            if (unit == null)
                return 0;

            if (unit.ED.Config.bandwidthUsage != null)
                return (int) unit.ED.Config.bandwidthUsage;

            Logger.Error("consistency error. no bandwidth usage max was defined for definition: " + unit.Definition + " " + unit.ED.Name);
            return 100; 
        }

        public static void SendNodeDeployed(this IPBSObject node)
        {
            var eventType = node is PBSDockingBase ? PBSEventType.baseDeployed : PBSEventType.nodeDeployed;
            node.SendNodeUpdate(eventType);
        }

        public static void SendNodeDead(this IPBSObject node)
        {
            node.SendNodeUpdate(PBSEventType.nodeDead);
        }
    }
}
