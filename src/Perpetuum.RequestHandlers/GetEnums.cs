using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Alliances;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Host;
using Perpetuum.Host.Requests;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Services.Channels;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.Relay;
using Perpetuum.Services.Social;
using Perpetuum.Services.TechTree;
using Perpetuum.Services.Trading;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.Connections;
using Perpetuum.Zones.Scanning;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Plants;
using Perpetuum.Zones.Terrains.Terraforming;

namespace Perpetuum.RequestHandlers
{
    public class GetEnums : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            var enumsDict = new Dictionary<string, object>
            {
                {k.errorCodes, EnumHelper.ToDictionary<ErrorCodes>()},
                {k.corporationRole, EnumHelper.ToDictionary<CorporationRole>()},
                {k.allianceRoles, EnumHelper.ToDictionary<AllianceRole>()},
                {k.accountState, EnumHelper.ToDictionary<AccountState>()},
                {k.accessRoles, EnumHelper.ToDictionary<AccessRoles>()},
                {k.accessLevel, EnumHelper.ToDictionary<AccessLevel>()},
                {k.containerAccess, EnumHelper.ToDictionary<ContainerAccess>()},
                {k.transactionType, EnumHelper.ToDictionary<TransactionType>()},
                {k.entityState, EnumHelper.ToDictionary<UnitStateFlags>()},
                {k.hostState, EnumHelper.ToDictionary<HostState>()},
                {k.slotFlags, EnumHelper.ToDictionary<SlotFlags>()},
                {k.aggregate, EnumHelper.ToDictionary<AggregateField>()},
                {k.beamState, EnumHelper.ToDictionary<BeamState>()},
                {k.attributeFlags, EnumHelper.ToDictionary<AttributeFlags>()},
                {k.serverMessageType, EnumHelper.ToDictionary<ServerMessageType>()},
                {k.serverMessageRecipientType, EnumHelper.ToDictionary<ServerMessageRecipient>()},
                {k.layerType, EnumHelper.ToDictionary<LayerType>()},
                {k.materialType, EnumHelper.ToDictionary<MaterialType>()},
                {k.NPCTemplateType, EnumHelper.ToDictionary<NpcTemplateType>()},
                {k.missionTargetType, EnumHelper.ToDictionary<MissionTargetType>()},
                {k.moduleState, EnumHelper.ToDictionary<ModuleStateType>()},
                {k.damageType, EnumHelper.ToDictionary<DamageType>()},
                {k.enterType, EnumHelper.ToDictionary<ZoneEnterType>()},
                {k.exitType, EnumHelper.ToDictionary<ZoneExitType>()},
                {k.lockType, EnumHelper.ToDictionary<LockType>()},
                {k.lockState, EnumHelper.ToDictionary<LockState>()},
                {k.zoneCommands, EnumHelper.ToDictionary<ZoneCommand>()},
                {k.deliveryState, EnumHelper.ToDictionary<DeliverResult>()},
                {k.materialProbeType, EnumHelper.ToDictionary<MaterialProbeType>()},
                {k.channelCommand, EnumHelper.ToDictionary<ChannelNotify>()},
                {k.channelType, EnumHelper.ToDictionary<ChannelType>()},
                {k.channelMemberRole, EnumHelper.ToDictionary<ChannelMemberRole>()},
                {k.productionType, EnumHelper.ToDictionary<ProductionInProgressType>()},
                {k.controlLayerBits, EnumHelper.ToDictionary<TerrainControlFlags>()},
                {k.tradeState, EnumHelper.ToDictionary<TradeState>()},
                {k.effectType, EnumHelper.ToDictionary<EffectType>()},
                {k.effectCategory, EnumHelper.ToDictionary<EffectCategory>()},
                {k.terrainCondition, EnumHelper.ToDictionary<TerrainCondition>()},
                {k.blockingFlags, EnumHelper.ToDictionary<BlockingFlags>()},
                {k.artifactType, EnumHelper.ToDictionary<ArtifactType>()},
                {k.socialState, EnumHelper.ToDictionary<SocialState>()},
                {k.NPCPresenceType, EnumHelper.ToDictionary<PresenceType>()},
                {k.stabilityBonusType, EnumHelper.ToDictionary<StabilityBonusType>()},
                {k.intrusionEvents, EnumHelper.ToDictionary<IntrusionEvents>()},
                {k.plantType, EnumHelper.ToDictionary<PlantType>()},
                {k.updatePacketControl, EnumHelper.ToDictionary<UpdatePacketControl>()},
                {k.PBSEvents, EnumHelper.ToDictionary<PBSEventType>()},
                {k.corporationPlanType, EnumHelper.ToDictionary<CorporationDocumentType>()},
                {k.productionEvents, EnumHelper.ToDictionary<ProductionEvent>()},
                {k.connectionTypes, EnumHelper.ToDictionary<PBSConnectionType>()},
                {k.PBSLogType, EnumHelper.ToDictionary<PBSLogType>()},
                {k.PBSEnergyState, EnumHelper.ToDictionary<PBSEnergyState>()},
                {k.unitDataType, EnumHelper.ToDictionary<UnitDataType>()},
                {k.gangRole, EnumHelper.ToDictionary<GangRole>()},
                {"transportAssignmentEvent", EnumHelper.ToDictionary<TransportAssignmentEvent>()},
                {"corporationBulletinEvent", EnumHelper.ToDictionary<CorporationBulletinEvent>()},
                {"combatLogTypes", EnumHelper.ToDictionary<CombatLogType>()},
                {"techTreeGroups", EnumHelper.ToDictionary<TechTreeGroup>()},
                {"techTreePointTypes", EnumHelper.ToDictionary<TechTreePointType>()},
                {"techTreeLogTypes", EnumHelper.ToDictionary<LogType>()},
                {"relayState", EnumHelper.ToDictionary<RelayState>()},
                {k.missionCategory, EnumHelper.ToDictionary<MissionCategory>()},
                {k.zoneTypes, EnumHelper.ToDictionary<ZoneType>()},
                {"teleportDescriptionType", EnumHelper.ToDictionary<TeleportDescriptionType>()},
                {"accountTransactionType", EnumHelper.ToDictionary<AccountTransactionType>()},
                {"PBSDockingBaseVisibility", EnumHelper.ToDictionary<PBSDockingBaseVisibility>()},
                {"TerraformType", EnumHelper.ToDictionary<TerraformType>()},
                {"TerraformDirection", EnumHelper.ToDictionary<TerraformDirection>()},
                {"TierType", EnumHelper.ToDictionary<TierType>()},
                {k.EpForActivityType, EnumHelper.ToDictionary<EpForActivityType>()},
            };

            Message.Builder.FromRequest(request).SetData("enums",enumsDict).Send();
        }
    }
}