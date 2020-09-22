using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Perpetuum
{
    public static class Commands
    {
        private static Dictionary<string, Command> _commands;

        static Commands()
        {
            _commands = typeof(Commands).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(info => (Command) info.GetValue(null))
                .ToDictionary(cmd => cmd.Text);
        }




        public static Command GetCommandByText(string commandText)
        {
            return _commands.GetOrDefault(commandText);
        }

        public static readonly Command Welcome = new Command
        {
            Text = "welcome"
        };

        public static readonly Command MarketCleanUp = new Command
        {
            Text = "marketCleanUp",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneDrawAllDecors = new Command
        {
            Text = "zoneDrawAllDecors",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ReloadStandingForCharacter = new Command
        {
            Text = "reloadStandingForCharacter",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command ZoneDrawDecorEnvByDef = new Command
        {
            Text = "zoneDrawDecorEnvByDef",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command ZoneMakeGotoXY = new Command
        {
            Text = "zoneMakeGotoXY",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command ZoneUpdateStructure = new Command
        {
            Text = "zoneUpdateStructure",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        //[22:30:34] ERR [UREQ] NoSuchCommand Data = {command=zoneDrawRamp} ip: 127.0.0.1 account: 9 character: 4 Req: zoneDrawRamp:zone_39:#max=n0#size=n60#range=f0.494141#positionx=n1411#positiony=n916#blend=f0.500000
        public static readonly Command ZoneDrawRamp = new Command
        {
            Text = "zoneDrawRamp",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.max),
                new Argument<int>(k.size),
                new Argument<double>(k.range),
                new Argument<int>("positionx"),
                new Argument<int>("positiony"),
                new Argument<double>("blend")
            }
        };

        public static readonly Command ZoneDisplayMissionSpots = new Command
        {
            Text = "zoneDisplayMissionSpots",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZonePBSFixOrphaned = new Command
        {
            Text = "zonePBSFixOrphaned",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneDisplayMissionRandomPoints = new Command
        {
            Text = "zoneDisplayMissionRandomPoints",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionStartedFromFieldTerminal = new Command
        {
            Text = "missionStartedFromFieldTerminal",
        };

        public static readonly Command MissionResolveTest = new Command
        {
            Text = "missionResolveTest",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionSpotPlace = new Command
        {
            Text = "missionSpotPlace",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionSpotUpdate = new Command
        {
            Text = "missionSpotUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneKillNPlants = new Command
        {
            Text = "zoneKillNPlants",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command BaseSelect = new Command
        {
            Text = "baseSelect",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneSwitchDegrade = new Command
        {
            Text = "zoneSwitchDegrade",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneRestoreOriginalGamma = new Command
        {
            Text = "zoneRestoreOriginalGamma",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ExtensionTest = new Command
        {
            Text = "extensionTest",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneSetReinforceCounter = new Command
        {
            Text = "zoneSetReinforceCounter",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ExtensionPointsIncreased = new Command
        {
            Text = "extensionPointsIncreased",
        };

        public static readonly Command ZoneForceDeconstruct = new Command
        {
            Text = "zoneForceDeconstruct",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneFixPBS = new Command
        {
            Text = "zoneFixPBS",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneTerraformTest = new Command
        {
            Text = "zoneTerraformTest",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionBonusUpdate = new Command
        {
            Text = "missionBonusUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionAdminTake = new Command
        {
            Text = "missionAdminTake",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.missionID)
            }
        };

        public static readonly Command MissionAdminListAll = new Command
        {
            Text = "missionAdminListAll",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketRemoveItems = new Command
        {
            Text = "marketRemoveItems",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneResetMissions = new Command
        {
            Text = "zoneResetMissions",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketCreateGammaPlasmaOrders = new Command
        {
            Text = "marketCreateGammaPlasmaOrders",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CorporationBulletinUpdate = new Command
        {
            Text = "corporationBulletinUpdate",
        };

        public static readonly Command SparkTeleportBaseDeleted = new Command
        {
            Text = "sparkTeleportBaseDeleted",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentRetrieved = new Command
        {
            Text = "transportAssignmentRetrieved",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentExpired = new Command
        {
            Text = "transportAssignmentExpired",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentAccepted = new Command
        {
            Text = "transportAssignmentAccepted",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentBaseDeleted = new Command
        {
            Text = "transportAssignmentBaseDeleted",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentFailed = new Command
        {
            Text = "transportAssignmentFailed",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentDelivered = new Command
        {
            Text = "transportAssignmentDelivered",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentGaveUp = new Command
        {
            Text = "transportAssignmentGaveUp",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TransportAssignmentContainerRetrieved = new Command
        {
            Text = "transportAssignmentContainerRetrieved",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionLineDead = new Command
        {
            Text = "productionLineDead",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command NpcCheckCondition = new Command
        {
            Text = "NPCCheckCondition",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionFacilityState = new Command
        {
            Text = "productionFacilityState",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CorporationInfoFlushCache = new Command
        {
            Text = "corporationInfoFlushCache",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TeleportEnabled = new Command
        {
            Text = "teleportEnabled",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionUpdate = new Command
        {
            Text = "productionUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command PbsEvent = new Command
        {
            Text = "PBSEvent",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZonePBSTest = new Command
        {
            Text = "zonePBSTest",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneHealAllWalls = new Command
        {
            Text = "zoneHealAllWalls",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZonePlaceWall = new Command
        {
            Text = "zonePlaceWall",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneClearWalls = new Command
        {
            Text = "zoneClearWalls",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TeleportTargetSet = new Command
        {
            Text = "teleportTargetSet",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneSapActivityEnd = new Command
        {
            Text = "zoneSapActivityEnd",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneRemoveByDefinition = new Command
        {
            Text = "zoneRemoveByDefinition",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command ZoneServerMessage = new Command
        {
            Text = "zoneServerMessage",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CharacterForcedToBase = new Command
        {
            Text = "characterForcedToBase",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command SparkSetDefault = new Command
        {
            Text = "sparkSetDefault",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneCheckRoaming = new Command
        {
            Text = "zoneCheckRoaming",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProximityProbeUpdate = new Command
        {
            Text = "proximityProbeUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProximityProbeCreated = new Command
        {
            Text = "proximityProbeCreated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProximityProbeDead = new Command
        {
            Text = "proximityProbeDead",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProximityProbeInfo = new Command
        {
            Text = "proximityProbeInfo",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionRemoteStart = new Command
        {
            Text = "productionRemoteStart",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionRemoteEnd = new Command
        {
            Text = "productionRemoteEnd",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionRemoteCancel = new Command
        {
            Text = "productionRemoteCancel",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneSetRuntimeZoneEntityName = new Command
        {
            Text = "zoneSetRuntimeZoneEntityName",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command ZoneDrawBeam = new Command
        {
            Text = "zoneDrawBeam",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<double>(k.x),
                new Argument<double>(k.y),
            }
        };

        public static readonly Command MissionError = new Command
        {
            Text = "missionError",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ContainerMover = new Command
        {
            Text = "containerMover",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<int>(k.characterID),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command ServerShutDown = new Command
        {
            Text = "serverShutDown",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<DateTime>(k.date),
                new Argument<string>(k.message),
            }
        };

        public static readonly Command ServerShutDownCancel = new Command
        {
            Text = "serverShutDownCancel",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ServerShutDownState = new Command
        {
            Text = "serverShutDownState",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command TriggerMissionStructure = new Command
        {
            Text = "triggerMissionStructure",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command JumpAnywhere = new Command
        {
            Text = "jumpAnywhere",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.zoneID),
                new Argument<int>(k.x),
                new Argument<int>(k.y)
            }
        };

        public static readonly Command MovePlayer = new Command
        {
            Text = "movePlayer",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<int>(k.zoneID),
                new Argument<int>(k.x),
                new Argument<int>(k.y)
            }
        };

        public static readonly Command MissionTargetUpdate = new Command
        {
            Text = "missionTargetUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionTargetCompleted = new Command
        {
            Text = "missionTargetCompleted",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionTargetActivated = new Command
        {
            Text = "missionTargetActivated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command StandingSetOnMyCorporation = new Command
        {
            Text = "standingSetOnMyCorporation",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command AlarmOver = new Command
        {
            Text = "alarmOver",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RemoveMissionStructure = new Command
        {
            Text = "removeMissionStructure",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command BaseReown = new Command
        {
            Text = "baseReown",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ExtensionRevert = new Command
        {
            Text = "extensionRevert",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.fee),
            }
        };

        public static readonly Command ChannelCreateForTerminals = new Command
        {
            Text = "channelCreateForTerminals",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TeleportConnectColumns = new Command
        {
            Text = "teleportConnectColumns",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
            }
        };

        public static readonly Command NpcAddSafeSpawnPoint = new Command
        {
            Text = "npcAddSafeSpawnPoint",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command NpcListSafeSpawnPoint = new Command
        {
            Text = "npcListSafeSpawnPoint",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command NpcDeleteSafeSpawnPoint = new Command
        {
            Text = "npcDeleteSafeSpawnPoint",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command NpcPlaceSafeSpawnPoint = new Command
        {
            Text = "npcPlaceSafeSpawnPoint",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.x),
                new Argument<int>(k.y),
            }
        };

        public static readonly Command NpcSetSafeSpawnPoint = new Command
        {
            Text = "npcSetSafeSpawnPoint",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.x),
                new Argument<int>(k.y)
            }
        };

        public static readonly Command CharacterUpdate = new Command
        {
            Text = "characterUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionReset = new Command
        {
            Text = "missionReset",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command SetMaxUserCount = new Command
        {
            Text = "setMaxUserCount",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command DecorCategoryList = new Command
        {
            Text = "decorCategoryList",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command NpcCheckFlocks = new Command
        {
            Text = "npcCheckFlocks",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionCorporationInsuranceList = new Command
        {
            Text = "productionCorporationInsuranceList",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketListFacilities = new Command
        {
            Text = "marketListFacilities",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketInsertStats = new Command
        {
            Text = "marketInsertStats",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.day),
                new Argument<long>(k.marketEID),
                new Argument<double>(k.price),
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command MarketInsertAverageForCF = new Command
        {
            Text = "marketInsertAverageForCF",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.amount),
                new Argument<int>(k.day),
                new Argument<string>(k.category)
            }
        };

        public static readonly Command ProductionGetInsurance = new Command
        {
            Text = "productionGetInsurance",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionSetInsurance = new Command
        {
            Text = "productionSetInsurance",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ReturnCorporateOwnedItems = new Command
        {
            Text = "returnCorporateOwnedItems",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ForceFactionStandings = new Command
        {
            Text = "forceFactionStandings",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<double>(k.standing)
            }
        };

        public static readonly Command ZoneTest = new Command
        {
            Text = "zoneTest",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command DockAll = new Command
        {
            Text = "dockAll",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CharacterSetCredit = new Command
        {
            Text = "characterSetCredit",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.credit)
            }
        };

        public static readonly Command ZoneCreateTeleportColumn = new Command
        {
            Text = "zoneCreateTeleportColumn",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command State = new Command
        {
            Text = "state",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RobotTemplateAdd = new Command
        {
            Text = "robotTemplateAdd",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<Dictionary<string, object>>(k.description),
            }
        };

        public static readonly Command RobotTemplateUpdate = new Command
        {
            Text = "robotTemplateUpdate",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<Dictionary<string, object>>(k.description),
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command RobotTemplateDelete = new Command
        {
            Text = "robotTemplateDelete",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command RobotTemplateList = new Command
        {
            Text = "robotTemplateList",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RobotTemplateBuild = new Command
        {
            Text = "robotTemplateBuild",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command FittingPresetList = new Command
        {
            Text = "fittingPresetList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command FittingPresetSave = new Command
        {
            Text = "fittingPresetSave",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<string>(k.name),
            }
        };

        public static readonly Command FittingPresetDelete = new Command
        {
            Text = "fittingPresetDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command FittingPresetApply = new Command
        {
            Text = "fittingPresetApply",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.robotEID),
                new Argument<long>(k.containerEID)
            }
        };

        public static readonly Command ServerMessage = new Command
        {
            Text = "serverMessage",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.message),
                new Argument<int>(k.type),
                new Argument<int>(k.recipients),
                new Argument<int>(k.translate)
            }
        };

        public static readonly Command UpdateMoodMessage = new Command
        {
            Text = "update_moodMessage",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<string>(k.moodMessage),
            }
        };

        public static readonly Command DecorUpdate = new Command
        {
            Text = "decorUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command DecorDelete = new Command
        {
            Text = "decorDelete",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CharacterSetAvatar = new Command
        {
            Text = "characterSetAvatar",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<Dictionary<string, object>>(k.avatar),
                new Argument<string>(k.rendered),
            }
        };

        public static readonly Command CharacterGetZoneInfo = new Command
        {
            Text = "characterGetZoneInfo",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command ExtensionGive = new Command
        {
            Text = "extensionGive",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ExtensionReset = new Command
        {
            Text = "extensionReset",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command AddNews = new Command
        {
            Text = "addNews",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.title),
                new Argument<string>(k.body),
                new Argument<int>(k.type),
                new Argument<int>(k.language)
            }
        };

        public static readonly Command UpdateNews = new Command
        {
            Text = "updateNews",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.title),
                new Argument<string>(k.body),
                new Argument<int>(k.type),
                new Argument<int>(k.language),
                new Argument<int>(k.ID),
                new Argument<DateTime>(k.time)
            }
        };

        public static readonly Command CorporationHangarRentExpired = new Command
        {
            Text = "corporationHangarRentExpired",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CorporationForceInfo = new Command
        {
            Text = "corporationForceInfo",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<Dictionary<string, object>>(k.publicProfile),
            }
        };

        public static readonly Command GangAddMember = new Command
        {
            Text = "gangAddMember",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command GangRemoveMember = new Command
        {
            Text = "gangRemoveMember",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command GangKickMember = new Command
        {
            Text = "gangKickMember",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionExpired = new Command
        {
            Text = "missionExpired",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionDone = new Command
        {
            Text = "missionDone",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionStartItems = new Command
        {
            Text = "missionStartItems",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionReloadCache = new Command
        {
            Text = "missionReloadCache",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MissionFlush = new Command
        {
            Text = "missionFlush",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RelayOpen = new Command
        {
            Text = "relayOpen",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RelayClose = new Command
        {
            Text = "relayClose",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ContainerUpdate = new Command
        {
            Text = "containerUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CreateItem = new Command
        {
            Text = "createItem",
            AccessLevel = AccessLevel.normal
        }; //%%% na ez egy sechole, fix it!!!

        public static readonly Command CreateCorporationHangarStorage = new Command
        {
            Text = "createCorporationHangarStorage",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.baseEID)
            }
        };

        public static readonly Command ForceStanding = new Command
        {
            Text = "forceStanding",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
                new Argument<double>(k.standing)
            }
        };

        public static readonly Command ZoneGetQueueInfo = new Command
        {
            Text = "zoneGetQueueInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ZoneSetQueueLength = new Command
        {
            Text = "zoneSetQueueLength",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ZoneCancelEnterQueue = new Command
        {
            Text = "zoneCancelEnterQueue",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ZoneEntityChangeState = new Command
        {
            Text = "zoneEntityChangeState",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.targetEID),
                new Argument<long>(k.cloneEID),
                new Argument<int>(k.bit),
                new Argument<int>(k.state)
            }
        };

        public static readonly Command ZoneDecorAdd = new Command
        {
            Text = "zoneDecorAdd",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.x),
                new Argument<int>(k.y),
                new Argument<int>(k.z),
                new Argument<double>(k.quaternionX),
                new Argument<double>(k.quaternionY),
                new Argument<double>(k.quaternionZ),
                new Argument<double>(k.quaternionW),
                new Argument<double>(k.scale),
                new Argument<int>(k.category)
            }
        };

        public static readonly Command ZoneDecorSet = new Command
        {
            Text = "zoneDecorSet",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.x),
                new Argument<int>(k.y),
                new Argument<int>(k.z),
                new Argument<double>(k.quaternionX),
                new Argument<double>(k.quaternionY),
                new Argument<double>(k.quaternionZ),
                new Argument<double>(k.quaternionW),
                new Argument<double>(k.scale),
                new Argument<int>(k.ID),
                new Argument<double>(k.fadeDistance),
                new Argument<int>(k.category)
            }
        };

        public static readonly Command ZoneDecorDelete = new Command
        {
            Text = "zoneDecorDelete",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command ZoneDecorLock = new Command
        {
            Text = "zoneDecorLock",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.locked),
            }
        };

        public static readonly Command ZoneEnvironmentDescriptionList = new Command
        {
            Text = "zoneEnvironmentDescriptionList",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneDrawDecorEnvironment = new Command
        {
            Text = "zoneDrawDecorEnvironment",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneSampleDecorEnvironment = new Command
        {
            Text = "zoneSampleDecorEnvironment",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.range),
            }
        };

        public static readonly Command ZoneCreateIsland = new Command
        {
            Text = "zoneCreateIsland",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.low)
            }
        };

        public static readonly Command ZoneSampleEnvironment = new Command
        {
            Text = "zoneSampleEnvironment",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.range),
            }
        };

        public static readonly Command ZoneSetPlantsSpeed = new Command
        {
            Text = "zoneSetPlantsSpeed",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.speed)
            }
        };

        public static readonly Command ZoneSetPlantsMode = new Command
        {
            Text = "zoneSetPlantsMode",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.mode)
            }
        };

        public static readonly Command ZoneGetPlantsMode = new Command
        {
            Text = "zoneGetPlantsMode",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneCreateGarden = new Command
        {
            Text = "zoneCreateGarden",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.x),
                new Argument<int>(k.y),
            }
        };

        public static readonly Command ZoneClearLayer = new Command
        {
            Text = "zoneClearLayer",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<string>(k.layerName)
            }
        };

        public static readonly Command ZoneSetBaseDetails = new Command
        {
            Text = "zoneSetBaseDetails",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ZonePutPlant = new Command
        {
            Text = "zonePutPlant",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.x),
                new Argument<int>(k.y),
                new Argument<int>(k.index),
                new Argument<int>(k.state)
            }
        };

        public static readonly Command ZoneDrawBlockingByEid = new Command
        {
            Text = "zoneDrawBlockingByEid",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ZoneDrawBlockingByDefinition = new Command
        {
            Text = "zoneDrawBlockingByDefinition",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int[]>(k.definition)
            }
        };

        public static readonly Command ZoneCleanBlockingByDefinition = new Command
        {
            Text = "zoneCleanBlockingByDefinition",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int[]>(k.definition)
            }
        };

        public static readonly Command ZoneDrawStatMap = new Command
        {
            Text = "zoneDrawStatMap",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneCleanObstacleBlocking = new Command
        {
            Text = "zoneCleanObstacleBlocking",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneMoveUnit = new Command
        {
            Text = "zoneMoveUnit",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.x),
                new Argument<int>(k.y),
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command ZoneListPresences = new Command
        {
            Text = "zoneListPresences",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneNpcFlockSet = new Command
        {
            Text = "zoneNPCFlockSet",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneNpcFlockSetParameter = new Command
        {
            Text = "zoneNPCFlockSetParameter",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneNpcFlockKill = new Command
        {
            Text = "zoneNPCFlockKill",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.presenceID),
                new Argument<int>(k.flockID),
            }
        };

        public static readonly Command ZoneNpcFlockNew = new Command
        {
            Text = "zoneNPCFlockNew",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneNpcFlockDelete = new Command
        {
            Text = "zoneNPCFlockDelete",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.presenceID),
                new Argument<int>(k.flockID),
            }
        };

        public static readonly Command ZoneMissionNew = new Command
        {
            Text = "zoneMissionNew",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneDebugLOS = new Command
        {
            Text = "zoneDebugLOS",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.state)
            }
        };

        public static readonly Command ZoneGetMyArtifacts = new Command
        {
            Text = "zoneGetMyArtifacts",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneGetZoneObjectDebugInfo = new Command
        {
            Text = "zoneGetZoneObjectDebugInfo",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.targetEID)
            }
        };

        public static readonly Command ZoneUploadScanResult = new Command
        {
            Text = "zoneUploadScanResult",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ZoneRemoveObject = new Command
        {
            Text = "zoneRemoveObject",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.target)
            }
        };

        public static readonly Command MarketItemSold = new Command
        {
            Text = "marketItemSold",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketItemBought = new Command
        {
            Text = "marketItemBought",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketItemExpired = new Command
        {
            Text = "marketItemExpired",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketSellOrderCreated = new Command
        {
            Text = "marketSellOrderCreated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketBuyOrderUpdate = new Command
        {
            Text = "marketBuyOrderUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketSetState = new Command
        {
            Text = "marketSetState",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketBuyOrderCreated = new Command
        {
            Text = "marketBuyOrderCreated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketSellOrderUpdate = new Command
        {
            Text = "marketSellOrderUpdate",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MarketFlush = new Command
        {
            Text = "marketFlush",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.market)
            }
        };

        public static readonly Command MarketAddCategory = new Command
        {
            Text = "marketAddCategory",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.isSell),
                new Argument<int>(k.quantity),
                new Argument<int>(k.duration),
                new Argument<int>(k.price)
            }
        };

        public static readonly Command MarketGetState = new Command
        {
            Text = "marketGetState",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command RobotActivated = new Command
        {
            Text = "robotActivated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionRemoveFacility = new Command
        {
            Text = "productionRemoveFacility",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ProductionSpawnComponents = new Command
        {
            Text = "productionSpawnComponents",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command ProductionScaleComponentsAmount = new Command
        {
            Text = "productionScaleComponentsAmount",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.targets),
                new Argument<long>(k.materials),
                new Argument<double>(k.ratio)
            }
        };

        public static readonly Command ProductionUnrepairItem = new Command
        {
            Text = "productionUnrepairItem",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.target)
            }
        };

        public static readonly Command ProductionFinished = new Command
        {
            Text = "productionFinished",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionFacilityOnOff = new Command
        {
            Text = "productionFacilityOnOff",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<int>(k.state),
            }
        };

        public static readonly Command ProductionForceEnd = new Command
        {
            Text = "productionForceEnd",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ProductionSpawnCPRG = new Command
        {
            Text = "productionSpawnCPRG",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };


        public static readonly Command MissionListAgents = new Command
        {
            Text = "missionListAgents",
            AccessLevel = AccessLevel.normal
        };
        
        public static readonly Command EpForActivityDailyLog = new Command
        {
            Text = "epForActivityDailyLog",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionPlayerAddsParticipant = new Command
        {
            Text = "missionPlayerAddsParticipant",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.ID),
                new Argument<string>(k.guid),
            }
        };

        public static readonly Command ItemCountOnZone = new Command
        {
            Text = "itemCountOnZone",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionStartFromZone = new Command
        {
            Text = "missionStartFromZone",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.missionCategory),
                new Argument<int>(k.missionLevel),
            }
        };

        public static readonly Command FieldTerminalInfo = new Command
        {
            Text = "fieldTerminalInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command SteamGetProducts = new Command
        {
            Text = "steamGetProducts",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SteamStartTransaction = new Command
        {
            Text = "steamStartTransaction",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SteamFinishTransaction = new Command
        {
            Text = "steamFinishTransaction",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command UseItem = new Command
        {
            Text = "useItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command GateSetName = new Command
        {
            Text = "gateSetName",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command ExtensionBuyEpBoost = new Command
        {
            Text = "extensionBuyEpBoost",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MtProductPriceList = new Command
        {
            Text = "mtProductPriceList",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command RedeemableItemActivate = new Command
        {
            Text = "redeemableItemActivate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command RedeemableItemList = new Command
        {
            Text = "redeemableItemList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command RedeemableItemRedeem = new Command
        {
            Text = "redeemableItemRedeem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentGiveUp = new Command
        {
            Text = "transportAssignmentGiveUp",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentListContent = new Command
        {
            Text = "transportAssignmentListContent",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentRetrieve = new Command
        {
            Text = "transportAssignmentRetrieve",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentRunning = new Command
        {
            Text = "transportAssignmentRunning",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TransportAssignmentContainerInfo = new Command
        {
            Text = "transportAssignmentContainerInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command TransportAssignmentLog = new Command
        {
            Text = "transportAssignmentLog",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command TransportAssignmentTake = new Command
        {
            Text = "transportAssignmentTake",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentCancel = new Command
        {
            Text = "transportAssignmentCancel",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command TransportAssignmentSubmit = new Command
        {
            Text = "transportAssignmentSubmit",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.sourceBase),
                new Argument<long>(k.targetBase),
                new Argument<long>(k.eid),
                new Argument<int>(k.duration),
                new Argument<long>(k.reward),
                new Argument<long>(k.collateral)
            }
        };

        public static readonly Command TransportAssignmentList = new Command
        {
            Text = "transportAssignmentList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TransportAssignmentDeliver = new Command
        {
            Text = "transportAssignmentDeliver",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command SparkTeleportList = new Command
        {
            Text = "sparkTeleportList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SparkTeleportDelete = new Command
        {
            Text = "sparkTeleportDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command SparkTeleportSet = new Command
        {
            Text = "sparkTeleportSet",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SparkTeleportUse = new Command
        {
            Text = "sparkTeleportUse",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command GoodiePackList = new Command
        {
            Text = "goodiePackList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GoodiePackRedeem = new Command
        {
            Text = "goodiePackRedeem",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionQueryLineNextRound = new Command
        {
            Text = "productionQueryLineNextRound",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionMergeResearchKitsMulti = new Command
        {
            Text = "productionMergeResearchKitsMulti",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<long>(k.facility),
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command ProductionMergeResearchKitsMultiQuery = new Command
        {
            Text = "productionMergeResearchKitsMultiQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<long>(k.facility),
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command CorporationSetColor = new Command
        {
            Text = "corporationSetColor",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.color)
            }
        };

        public static readonly Command ProductionCPRGForgeQuery = new Command
        {
            Text = "productionCPRGForgeQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long>(k.source),
                new Argument<long>(k.target)
            }
        };

        public static readonly Command ProductionCPRGForge = new Command
        {
            Text = "productionCPRGForge",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long>(k.source),
                new Argument<long>(k.target)
            }
        };

        public static readonly Command CorporationDocumentRent = new Command
        {
            Text = "corporationDocumentRent",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentTransfer = new Command
        {
            Text = "corporationDocumentTransfer",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.target),
            }
        };

        public static readonly Command CorporationDocumentConfig = new Command
        {
            Text = "corporationDocumentConfig",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationDocumentList = new Command
        {
            Text = "corporationDocumentList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationDocumentUpdateBody = new Command
        {
            Text = "corporationDocumentUpdateBody",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.body),
                new Argument<int>(k.ID),
                new Argument<int>(k.version)
            }
        };

        public static readonly Command CorporationDocumentCreate = new Command
        {
            Text = "corporationDocumentCreate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.type)
            }
        };

        public static readonly Command CorporationDocumentOpen = new Command
        {
            Text = "corporationDocumentOpen",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentDelete = new Command
        {
            Text = "corporationDocumentDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentUnmonitor = new Command
        {
            Text = "corporationDocumentUnmonitor",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentMonitor = new Command
        {
            Text = "corporationDocumentMonitor",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentRegisterList = new Command
        {
            Text = "corporationDocumentRegisterList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command CorporationDocumentRegisterSet = new Command
        {
            Text = "corporationDocumentRegisterSet",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int[]>(k.members),
                new Argument<int[]>(k.writeAccess)
            }
        };

        public static readonly Command PBSSetBaseDeconstruct = new Command
        {
            Text = "PBSSetBaseDeconstruct",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.state),
            }
        };

        public static readonly Command PBSGetTerritories = new Command
        {
            Text = "PBSGetTerritories",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command PBSSetTerritoryVisibility = new Command
        {
            Text = "PBSSetTerritoryVisibility",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command PBSNodeInfo = new Command
        {
            Text = "PBSNodeInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command PBSSetStandingLimit = new Command
        {
            Text = "PBSSetStandingLimit",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command PBSCheckDeployment = new Command
        {
            Text = "PBSCheckDeployment",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.x),
                new Argument<int>(k.y)
            }
        };

        public static readonly Command PBSGetNetwork = new Command
        {
            Text = "PBSGetNetwork",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command PBSSetOnline = new Command
        {
            Text = "PBSSetOnline",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.state),
            }
        };

        public static readonly Command PBSRenameNode = new Command
        {
            Text = "PBSRenameNode",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<string>(k.name),
            }
        };

        public static readonly Command PBSSetConnectionWeight = new Command
        {
            Text = "PBSSetConnectionWeight",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
                new Argument<double>(k.weight)
            }
        };

        public static readonly Command PBSBreakConnection = new Command
        {
            Text = "PBSBreakConnection",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
            }
        };

        public static readonly Command PBSMakeConnection = new Command
        {
            Text = "PBSMakeConnection",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
            }
        };

        public static readonly Command PBSFeedableInfo = new Command
        {
            Text = "PBSFeedableInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command PBSFeedItems = new Command
        {
            Text = "PBSFeedItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command PBSGetLog = new Command
        {
            Text = "PBSGetLog",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command PBSSetReinforceOffset = new Command
        {
            Text = "PBSSetReinforceOffset",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.offset),
            }
        };

        public static readonly Command PBSSetEffect = new Command
        {
            Text = "PBSSetEffect",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.effect),
            }
        };

        public static readonly Command PBSGetReimburseInfo = new Command
        {
            Text = "PBSGetReimburseInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command PBSSetReimburseInfo = new Command
        {
            Text = "PBSSetReimburseInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationCeoTakeOverStatus = new Command
        {
            Text = "corporationCEOTakeOverStatus",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationVolunteerForCeo = new Command
        {
            Text = "corporationVolunteerForCEO",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TeleportQueryWorldChannels = new Command
        {
            Text = "teleportQueryWorldChannels",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command IntrusionSetDefenseThreshold = new Command
        {
            Text = "intrusionSetDefenseThreshold",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.siteEID)
            }
        };

        public static readonly Command GiftOpen = new Command
        {
            Text = "giftOpen",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command IntrusionSAPSubmitItem = new Command
        {
            Text = "intrusionSAPSubmitItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command IntrusionSAPGetItemInfo = new Command
        {
            Text = "intrusionSAPGetItemInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target)
            }
        };

        public static readonly Command GetIntrusionMySitesLog = new Command
        {
            Text = "getIntrusionMySitesLog",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetIntrusionPublicLog = new Command
        {
            Text = "getIntrusionPublicLog",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command IntrusionUpgradeFacility = new Command
        {
            Text = "intrusionUpgradeFacility",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility)
            }
        };

        public static readonly Command SetIntrusionSiteMessage = new Command
        {
            Text = "setIntrusionSiteMessage",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.message),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command GetIntrusionStabilityLog = new Command
        {
            Text = "getIntrusionStabilityLog",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.day),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command GetIntrusionLog = new Command
        {
            Text = "getIntrusionLog",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.offset),
            }
        };

        public static readonly Command BaseSetDockingRights = new Command
        {
            Text = "baseSetDockingRights",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.baseEID)
            }
        };

        public static readonly Command BaseGetOwnershipInfo = new Command
        {
            Text = "baseGetOwnershipInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SparkRemove = new Command
        {
            Text = "sparkRemove",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SparkList = new Command
        {
            Text = "sparkList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SparkChange = new Command
        {
            Text = "sparkChange",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.sparkID)
            }
        };

        public static readonly Command SparkUnlock = new Command
        {
            Text = "sparkUnlock",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.sparkID)
            }
        };

        public static readonly Command ProximityProbeRemove = new Command
        {
            Text = "proximityProbeRemove",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ProximityProbeRegisterSet = new Command
        {
            Text = "proximityProbeRegisterSet",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int[]>(k.members),
            }
        };

        public static readonly Command ProximityProbeList = new Command
        {
            Text = "proximityProbeList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProximityProbeSetName = new Command
        {
            Text = "proximityProbeSetName",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<string>(k.name),
            }
        };

        public static readonly Command ProximityProbeGetRegistrationInfo = new Command
        {
            Text = "proximityProbeGetRegistrationInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ExtensionRemoveLevel = new Command
        {
            Text = "extensionRemoveLevel",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.extensionID)
            }
        };

        public static readonly Command GetDefinitionConfigUnits = new Command
        {
            Text = "getDefinitionConfigUnits",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ItemShopList = new Command
        {
            Text = "itemShopList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ItemShopBuy = new Command
        {
            Text = "itemShopBuy",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.ID),
            }
        };

        public static readonly Command ProductionInProgressCorporation = new Command
        {
            Text = "productionInProgressCorporation",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionGetSupply = new Command
        {
            Text = "missionGetSupply",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command GetDistances = new Command
        {
            Text = "getDistances",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command YellowPagesSearch = new Command
        {
            Text = "yellowPagesSearch",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command YellowPagesSubmit = new Command
        {
            Text = "yellowPagesSubmit",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command YellowPagesGet = new Command
        {
            Text = "yellowPagesGet",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command YellowPagesDelete = new Command
        {
            Text = "yellowPagesDelete",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command AlarmStart = new Command
        {
            Text = "alarmStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command KioskInfo = new Command
        {
            Text = "kioskInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command KioskSubmitItem = new Command
        {
            Text = "kioskSubmitItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<long>(k.target),
            }
        };

        public static readonly Command ItemCount = new Command
        {
            Text = "itemCount",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SystemInfo = new Command
        {
            Text = "systemInfo",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command GetItemSummary = new Command
        {
            Text = "getItemSummary",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command ProductionFacilityDescription = new Command
        {
            Text = "productionFacilityDescription",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionInsuranceList = new Command
        {
            Text = "productionInsuranceList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionInsuranceQuery = new Command
        {
            Text = "productionInsuranceQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long[]>(k.target),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command ProductionInsuranceDelete = new Command
        {
            Text = "productionInsuranceDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target)
            }
        };

        public static readonly Command ProductionInsuranceBuy = new Command
        {
            Text = "productionInsuranceBuy",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long[]>(k.target),
            }
        };

        public static readonly Command StackTo = new Command
        {
            Text = "stackTo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.inventory),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command ForceDock = new Command
        {
            Text = "forceDock",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ForceDockAdmin = new Command
        {
            Text = "forceDockAdmin",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command ZoneSaveLayer = new Command
        {
            Text = "zoneSaveLayer",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TeleportList = new Command
        {
            Text = "teleportList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TeleportUse = new Command
        {
            Text = "teleportUse",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.ID),
            }
        };

        public static readonly Command TeleportToZoneObject = new Command
        {
            Text = "teleportToZoneObject",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target)
            }
        };

        public static readonly Command TeleportGetChannelList = new Command
        {
            Text = "teleportGetChannelList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command SignIn = new Command
        {
            Text = "signIn",
            AccessLevel = AccessLevel.notDefined,
            Arguments =
            {
                new Argument<string>(k.email),
                new Argument<string>(k.password),
                new Argument<int>(k.client)
            }
        };

        public static readonly Command SignInSteam = new Command
        {
            Text = "signInSteam",
            AccessLevel = AccessLevel.notDefined,
            Arguments =
            {
                new Argument<byte[]>("encData"),
                new Argument<int>(k.client),
            }
        };

        public static readonly Command SignOut = new Command
        {
            Text = "signOut",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SteamListAccounts = new Command
        {
            Text = "steamListAccounts",
            AccessLevel = AccessLevel.notDefined,
            Arguments =
            {
                new Argument<byte[]>("encData")
            }
        };

        public static readonly Command CharacterSettingsGet = new Command
        {
            Text = "characterSettingsGet",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterSettingsSet = new Command
        {
            Text = "characterSettingsSet",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<Dictionary<string, object>>(k.data)
            }
        };

        public static readonly Command CharacterSearch = new Command
        {
            Text = "characterSearch",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name)
            }
        };

        public static readonly Command PollGet = new Command
        {
            Text = "pollGet",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command PollAnswer = new Command
        {
            Text = "pollAnswer",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.answer),
            }
        };

        public static readonly Command Ping = new Command
        {
            Text = "ping",
            AccessLevel = AccessLevel.notDefined,
            Arguments =
            {
                new Argument<string>(k.state)
            }
        };

        public static readonly Command Quit = new Command
        {
            Text = "quit",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command GetEnums = new Command
        {
            Text = "getEnums",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command GetCommands = new Command
        {
            Text = "getCommands",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command GetZoneInfo = new Command
        {
            Text = "getZoneInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetEntityDefaults = new Command
        {
            Text = "getEntityDefaults",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command ExtensionHistory = new Command
        {
            Text = "extensionHistory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationTransactionHistory = new Command
        {
            Text = "corporationTransactionHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command CharacterTransactionHistory = new Command
        {
            Text = "characterTransactionHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command ProductionHistory = new Command
        {
            Text = "productionHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command StandingHistory = new Command
        {
            Text = "standingHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command CharacterListNpcDeath = new Command
        {
            Text = "characterListNpcDeath",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetEffects = new Command
        {
            Text = "getEffects",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command RobotEmpty = new Command
        {
            Text = "robotEmpty",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.container),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command ZoneSectorList = new Command
        {
            Text = "zoneSectorList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetHighScores = new Command
        {
            Text = "getHighScores",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetMyHighScores = new Command
        {
            Text = "getMyHighScores",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command IsOnline = new Command
        {
            Text = "isOnline",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.characterID)
            }
        };

        public static readonly Command Chat = new Command
        {
            Text = "chat",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.message),
                new Argument<int>(k.target),
            }
        };

        public static readonly Command CharacterGetProfiles = new Command
        {
            Text = "characterGetProfiles",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterCreate = new Command
        {
            Text = "characterCreate",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterSelect = new Command
        {
            Text = "characterSelect",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterWizardData = new Command
        {
            Text = "characterWizardData",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterCheckNick = new Command
        {
            Text = "characterCheckNick",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.nick)
            }
        };

        public static readonly Command CharacterUpdateBalance = new Command
        {
            Text = "characterUpdateBalance",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterCorporationHistory = new Command
        {
            Text = "characterCorporationHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterSetMoodMessage = new Command
        {
            Text = "characterSetMoodmessage",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.moodMessage)
            }
        };

        public static readonly Command CharacterRemoveFromCache = new Command
        {
            Text = "characterRemoveFromCache",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterSetBlockTrades = new Command
        {
            Text = "characterSetBlockTrades",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.state)
            }
        };

        public static readonly Command CharacterForceDeselect = new Command
        {
            Text = "characterForceDeselect",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterForceDisconnect = new Command
        {
            Text = "characterForceDisconnect",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterNickHistory = new Command
        {
            Text = "characterNickHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterRename = new Command
        {
            Text = "characterRename",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.nick),
                new Argument<int>(k.characterID),
            }
        };

        public static readonly Command SocialGetMyList = new Command
        {
            Text = "socialGetMyList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SocialFriendRequest = new Command
        {
            Text = "socialFriendRequest",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.friend)
            }
        };

        public static readonly Command SocialConfirmPendingFriendRequest = new Command
        {
            Text = "socialConfirmPendingFriendRequest",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.friend),
                new Argument<int>(k.accept),
            }
        };

        public static readonly Command SocialFriendRequestReply = new Command
        {
            Text = "socialFriendRequestReply",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command SocialDeleteFriend = new Command
        {
            Text = "socialDeleteFriend",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.friend)
            }
        };

        public static readonly Command SocialBlockFriend = new Command
        {
            Text = "socialBlockFriend",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.friend)
            }
        };

        public static readonly Command MailOpen = new Command
        {
            Text = "mailOpen",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.ID)
            }
        };

        public static readonly Command MailDelete = new Command
        {
            Text = "mailDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.ID)
            }
        };

        public static readonly Command MailList = new Command
        {
            Text = "mailList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.folder)
            }
        };

        public static readonly Command MailDeleteFolder = new Command
        {
            Text = "mailDeleteFolder",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.folder)
            }
        };

        public static readonly Command MailMoveToFolder = new Command
        {
            Text = "mailMoveToFolder",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.folder),
                new Argument<string>(k.ID),
            }
        };

        public static readonly Command MailNewCount = new Command
        {
            Text = "mailNewCount",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MailUsedFolders = new Command
        {
            Text = "mailUsedFolders",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MailSend = new Command
        {
            Text = "mailSend",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MassMailOpen = new Command
        {
            Text = "massMailOpen",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.ID)
            }
        };

        public static readonly Command MassMailDelete = new Command
        {
            Text = "massMailDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.ID)
            }
        };

        public static readonly Command MassMailSend = new Command
        {
            Text = "massMailSend",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.subject),
                new Argument<string>(k.body),
                new Argument<int[]>(k.target)
            }
        };

        public static readonly Command MassMailList = new Command
        {
            Text = "massMailList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.folder)
            }
        };

        public static readonly Command MassMailNewCount = new Command
        {
            Text = "massMailNewCount",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ChannelTalk = new Command
        {
            Text = "channelTalk",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<string>(k.message),
            }
        };

        public static readonly Command ChannelList = new Command
        {
            Text = "channelList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ChannelListAll = new Command
        {
            Text = "channelListAll",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ChannelMyList = new Command
        {
            Text = "channelMyList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ChannelNotification = new Command
        {
            Text = "channelNotification",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ChannelCreate = new Command
        {
            Text = "channelCreate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel)
            }
        };

        public static readonly Command ChannelJoin = new Command
        {
            Text = "channelJoin",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel)
            }
        };

        public static readonly Command ChannelLeave = new Command
        {
            Text = "channelLeave",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel)
            }
        };

        public static readonly Command ChannelKick = new Command
        {
            Text = "channelKick",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<int>(k.memberID),
            }
        };

        public static readonly Command ChannelSetTopic = new Command
        {
            Text = "channelSetTopic",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<string>(k.topic),
            }
        };

        public static readonly Command ChannelSetMemberRole = new Command
        {
            Text = "channelModifyMemberRole",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<int>(k.memberID),
                new Argument<int>(k.role)
            }
        };

        public static readonly Command ChannelSetPassword = new Command
        {
            Text = "channelSetPassword",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel)
            }
        };

        public static readonly Command ChannelBan = new Command
        {
            Text = "channelBan",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<int>(k.memberID),
            }
        };

        public static readonly Command ChannelRemoveBan = new Command
        {
            Text = "channelRemoveBan",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel),
                new Argument<int>(k.memberID),
            }
        };

        public static readonly Command ChannelGetBannedMembers = new Command
        {
            Text = "channelGetBannedMembers",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.channel)
            }
        };

        public static readonly Command ChannelGlobalMute = new Command
        {
            Text = "channelGlobalMute",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<int>(k.state),
            }
        };

        public static readonly Command ChannelGetMutedCharacters = new Command
        {
            Text = "channelGetMutedCharacters",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ExtensionGetAll = new Command
        {
            Text = "extensionGetAll",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionLearntList = new Command
        {
            Text = "extensionLearntList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionPrerequireList = new Command
        {
            Text = "extensionPrerequireList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionCategoryList = new Command
        {
            Text = "extensionCategoryList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionGetAvailablePoints = new Command
        {
            Text = "extensionGetAvailablePoints",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionBuyForPoints = new Command
        {
            Text = "extensionBuyForPoints",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.extensionID)
            }
        };

        public static readonly Command ExtensionGetPointParameters = new Command
        {
            Text = "extensionGetPointParameters",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ExtensionResetCharacter = new Command
        {
            Text = "extensionResetCharacter",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command ExtensionFreeLockedEp = new Command
        {
            Text = "extensionFreeLockedEp",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.amount)
            }
        }; //

        //GameAdmin Command
        public static readonly Command ExtensionFreeAllLockedEpCommand = new Command
        {
            Text = "extensionFreeAllLockedEpByCommand",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.accountID)
            }
        };

        //GameAdmin Command
        public static readonly Command EPBonusSet = new Command
        {
            Text = "EPBonusSet",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.bonus),
                new Argument<int>(k.duration),
            }
        };

        public static readonly Command FreshNewsCount = new Command
        {
            Text = "freshNewsCount",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.language)
            }
        };

        public static readonly Command GetNews = new Command
        {
            Text = "getNews",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.language),
                new Argument<int>(k.amount),
            }
        };

        public static readonly Command NewsCategory = new Command
        {
            Text = "newsCategory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command RequestInfiniteBox = new Command
        {
            Text = "requestInfiniteBox",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationCreate = new Command
        {
            Text = "corporationCreate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<string>(k.nick),
                new Argument<int>(k.taxRate),
                new Argument<Dictionary<string, object>>(k.publicProfile)
            }
        };

        public static readonly Command CorporationGetMyInfo = new Command
        {
            Text = "corporationGetMyInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationRemoveMember = new Command
        {
            Text = "corporationRemoveMember",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID)
            }
        };

        public static readonly Command CorporationSetMemberRole = new Command
        {
            Text = "corporationSetMemberRole",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID),
                new Argument<int>(k.role),
            }
        };

        public static readonly Command CorporationCharacterInvite = new Command
        {
            Text = "corporationCharacterInvite",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID),
                new Argument<string>(k.message),
            }
        };

        public static readonly Command CorporationInviteReply = new Command
        {
            Text = "corporationInviteReply",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.answer)
            }
        };

        public static readonly Command CorporationMemberTransferred = new Command
        {
            Text = "corporationMemberTransferred",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command CorporationInfo = new Command
        {
            Text = "corporationInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.eid)
            }
        };

        public static readonly Command CorporationLeave = new Command
        {
            Text = "corporationLeave",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationSearch = new Command
        {
            Text = "corporationSearch",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name)
            }
        };

        public static readonly Command CorporationSetInfo = new Command
        {
            Text = "corporationSetInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationDonate = new Command
        {
            Text = "corporationDonate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.amount)
            }
        };

        public static readonly Command CorporationDropRoles = new Command
        {
            Text = "corporationDropRoles",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationCancelLeave = new Command
        {
            Text = "corporationCancelLeave",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationHangarListOnBase = new Command
        {
            Text = "corporationHangarListOnBase",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationHangarListAll = new Command
        {
            Text = "corporationHangarListAll",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationRentHangar = new Command
        {
            Text = "corporationRentHangar",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.baseEID)
            }
        };

        public static readonly Command CorporationHangarLogSet = new Command
        {
            Text = "corporationHangarLogSet",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.log),
            }
        };

        public static readonly Command CorporationHangarLogClear = new Command
        {
            Text = "corporationHangarLogClear",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationHangarSetAccess = new Command
        {
            Text = "corporationHangarSetAccess",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.hangarAccess),
            }
        };

        public static readonly Command CorporationHangarClose = new Command
        {
            Text = "corporationHangarClose",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationHangarLogList = new Command
        {
            Text = "corporationHangarLogList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.offset),
            }
        };

        public static readonly Command CorporationPayOut = new Command
        {
            Text = "corporationPayOut",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID),
                new Argument<long>(k.amount),
            }
        };

        public static readonly Command CorporationHangarPayRent = new Command
        {
            Text = "corporationHangarPayRent",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationVoteStart = new Command
        {
            Text = "corporationVoteStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<string>(k.topic),
                new Argument<int>(k.participation),
                new Argument<int>(k.consensusRate)
            }
        };

        public static readonly Command CorporationVoteList = new Command
        {
            Text = "corporationVoteList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationVoteDelete = new Command
        {
            Text = "corporationVoteDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.voteID)
            }
        };

        public static readonly Command CorporationVoteCast = new Command
        {
            Text = "corporationVoteCast",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.voteID),
                new Argument<int>(k.answer),
            }
        };

        public static readonly Command CorporationVoteSetTopic = new Command
        {
            Text = "corporationVoteSetTopic",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.voteID),
                new Argument<string>(k.topic),
            }
        };

        public static readonly Command CorporationBulletinStart = new Command
        {
            Text = "corporationBulletinStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.title),
                new Argument<string>(k.text),
            }
        };

        public static readonly Command CorporationBulletinEntry = new Command
        {
            Text = "corporationBulletinEntry",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.text),
                new Argument<int>(k.bulletinID),
            }
        };

        public static readonly Command CorporationBulletinDelete = new Command
        {
            Text = "corporationBulletinDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.bulletinID)
            }
        };

        public static readonly Command CorporationBulletinList = new Command
        {
            Text = "corporationBulletinList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationBulletinDetails = new Command
        {
            Text = "corporationBulletinDetails",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.bulletinID)
            }
        };

        public static readonly Command CorporationHangarSetName = new Command
        {
            Text = "corporationHangarSetName",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<string>(k.name),
            }
        };

        public static readonly Command CorporationHangarRentPrice = new Command
        {
            Text = "corporationHangarRentPrice",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationApply = new Command
        {
            Text = "corporationApply",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.corporationEID),
                new Argument<string>(k.note),
            }
        };

        public static readonly Command CorporationListMyApplications = new Command
        {
            Text = "corporationListMyApplications",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationListApplications = new Command
        {
            Text = "corporationListApplications",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationDeleteMyApplication = new Command
        {
            Text = "corporationDeleteMyApplication",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.corporationEID),
                new Argument<int>(k.all),
            }
        };

        public static readonly Command CorporationDeleteApplication = new Command
        {
            Text = "corporationDeleteApplication",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<int>(k.all),
            }
        };

        public static readonly Command CorporationAcceptApplication = new Command
        {
            Text = "corporationAcceptApplication",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<long>(k.corporationEID),
            }
        };

        public static readonly Command CorporationHangarFolderSectionCreate = new Command
        {
            Text = "corporationHangarFolderCreate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationHangarFolderSectionDelete = new Command
        {
            Text = "corporationHangarFolderDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<long>(k.container),
            }
        };

        public static readonly Command CorporationGetDelegates = new Command
        {
            Text = "corporationGetDelegates",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command CorporationBulletinEntryDelete = new Command
        {
            Text = "corporationBulletinEntryDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.bulletinID),
                new Argument<int>(k.ID),
            }
        };

        public static readonly Command CorporationTransfer = new Command
        {
            Text = "corporationTransfer",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.amount),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command CorporationBulletinNewEntries = new Command
        {
            Text = "corporationBulletinNewEntries",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<DateTime>(k.time)
            }
        };

        public static readonly Command CorporationBulletinModerate = new Command
        {
            Text = "corporationBulletinModerate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<int>(k.bulletinID),
                new Argument<string>(k.text)
            }
        };

        public static readonly Command CorporationGetReputation = new Command
        {
            Text = "corporationGetReputation",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationMyStandings = new Command
        {
            Text = "corporationGetMyStandings",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationSetMembersNeutral = new Command
        {
            Text = "corporationSetMembersNeutral",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationLogHistory = new Command
        {
            Text = "corporationLogHistory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CorporationRename = new Command
        {
            Text = "corporationRename",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<string>(k.nick),
            }
        };

        public static readonly Command CorporationNameHistory = new Command
        {
            Text = "corporationNameHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.corporationEID)
            }
        };

        public static readonly Command AllianceGetMyInfo = new Command
        {
            Text = "allianceGetMyInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterGetNote = new Command
        {
            Text = "characterGetNote",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterSetNote = new Command
        {
            Text = "characterSetNote",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.target)
            }
        };

        public static readonly Command SetStanding = new Command
        {
            Text = "setStanding",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
                new Argument<double>(k.standing)
            }
        };

        public static readonly Command StandingList = new Command
        {
            Text = "standingList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command GangInviteReply = new Command
        {
            Text = "gangInviteReply",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.answer)
            }
        };

        public static readonly Command GangInvite = new Command
        {
            Text = "gangInvite",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID)
            }
        };

        public static readonly Command GangCreate = new Command
        {
            Text = "gangCreate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.name)
            }
        };

        public static readonly Command GangInfo = new Command
        {
            Text = "gangInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GangDelete = new Command
        {
            Text = "gangDelete",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GangLeave = new Command
        {
            Text = "gangLeave",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GangKick = new Command
        {
            Text = "gangKick",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID)
            }
        };

        public static readonly Command GangSetLeader = new Command
        {
            Text = "gangSetLeader",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID)
            }
        };

        public static readonly Command GangSetRole = new Command
        {
            Text = "gangSetRole",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID),
                new Argument<int>(k.role),
            }
        };

        public static readonly Command MissionStart = new Command
        {
            Text = "missionStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.missionCategory),
                new Argument<int>(k.missionLevel),
            }
        };

        public static readonly Command MissionLogList = new Command
        {
            Text = "missionLogList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command MissionData = new Command
        {
            Text = "missionData",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionGetOptions = new Command
        {
            Text = "missionGetOptions",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command MissionListRunning = new Command
        {
            Text = "missionListRunning",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionDeliver = new Command
        {
            Text = "missionDeliver",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MissionAbort = new Command
        {
            Text = "missionAbort",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command AccountUpdateBalance = new Command
        {
            Text = "accountUpdateBalance",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command AccountList = new Command
        {
            Text = "accountList",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command AccountGetTransactionHistory = new Command
        {
            Text = "accountGetTransactionHistory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command AccountEpForActivityHistory = new Command
        {
            Text = "accountEpForActivityHistory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterDeselect = new Command
        {
            Text = "characterDeselect",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterDelete = new Command
        {
            Text = "characterDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.characterID)
            }
        };

        public static readonly Command CharacterList = new Command
        {
            Text = "characterList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterGetMyProfile = new Command
        {
            Text = "characterGetMyProfile",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command BaseGetMyItems = new Command
        {
            Text = "baseGetMyItems",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command BaseListFacilities = new Command
        {
            Text = "baseListFacilities",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command BaseGetInfo = new Command
        {
            Text = "baseGetInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.baseEID)
            }
        };

        public static readonly Command TransferData = new Command
        {
            Text = "transferData",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.target),
                new Argument<Dictionary<string, object>>(k.data),
            }
        };

        public static readonly Command ConnectionStart = new Command
        {
            Text = "connectionStart",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ConnectionEnd = new Command
        {
            Text = "connectionEnd",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MailReceived = new Command
        {
            Text = "mailReceived",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MailGotRead = new Command
        {
            Text = "mailGotRead",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MailGotDeleted = new Command
        {
            Text = "mailGotDeleted",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command StackItems = new Command
        {
            Text = "stackItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.source),
                new Argument<long>(k.target),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command PackItems = new Command
        {
            Text = "packItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.container),
            }
        };

        public static readonly Command UnpackItems = new Command
        {
            Text = "unpackItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.container),
            }
        };

        public static readonly Command RelocateItems = new Command
        {
            Text = "relocateItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.eid),
                new Argument<long>(k.targetContainer),
                new Argument<long>(k.sourceContainer)
            }
        };

        public static readonly Command TrashItems = new Command
        {
            Text = "trashItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.container),
            }
        };

        public static readonly Command SetItemName = new Command
        {
            Text = "setItemName",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target),
                new Argument<string>(k.name),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command StackSelection = new Command
        {
            Text = "stackSelection",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.eid),
                new Argument<long>(k.container),
            }
        };

        public static readonly Command UnstackAmount = new Command
        {
            Text = "unStackAmount",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<int>(k.amount),
                new Argument<int>(k.size),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command CharacterTransferCredit = new Command
        {
            Text = "characterTransferCredit",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.amount),
                new Argument<int>(k.target),
            }
        };

        public static readonly Command RequestStarterRobot = new Command
        {
            Text = "requestStarterRobot",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command StarterRobotCreated = new Command
        {
            Text = "starterRobotCreated",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command CorporationRoleHistory = new Command
        {
            Text = "corporationRoleHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset)
            }
        };

        public static readonly Command CorporationMemberRoleHistory = new Command
        {
            Text = "corporationMemberRoleHistory",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.memberID),
                new Argument<int>(k.offset),
            }
        };

        public static readonly Command AllianceRoleHistory = new Command
        {
            Text = "allianceRoleHistory",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command Undock = new Command
        {
            Text = "undock",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command Dock = new Command
        {
            Text = "dock",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetAggregateFields = new Command
        {
            Text = "getAggregateFields",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetStandingForDefaultCorporations = new Command
        {
            Text = "getStandingForDefaultCorporations",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetStandingForDefaultAlliances = new Command
        {
            Text = "getStandingForDefaultAlliances",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterSetHomeBase = new Command
        {
            Text = "characterSetHomeBase",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterClearHomeBase = new Command
        {
            Text = "characterClearHomeBase",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command AllianceGetDefaults = new Command
        {
            Text = "allianceGetDefaults",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetMyKillReports = new Command
        {
            Text = "getMyKillReports",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command CharacterCorrectNick = new Command
        {
            Text = "characterCorrectNick",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.nick),
                new Argument<int>(k.characterID),
            }
        };

        public static readonly Command ListContainer = new Command
        {
            Text = "listContainer",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.container)
            }
        };

        public static readonly Command ZoneSOS = new Command
        {
            Text = "zoneSOS",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ZoneSelfDestruct = new Command
        {
            Text = "zoneSelfDestruct",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command ZoneGetBuildings = new Command
        {
            Text = "zoneGetBuildings",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MarketModifyOrder = new Command
        {
            Text = "marketModifyOrder",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MarketItemList = new Command
        {
            Text = "marketItemList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command MarketGetMyItems = new Command
        {
            Text = "marketGetMyItems",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MarketCancelItem = new Command
        {
            Text = "marketCancelItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.marketItemID)
            }
        };

        public static readonly Command MarketCreateSellOrder = new Command
        {
            Text = "marketCreateSellOrder",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.itemEID),
                new Argument<int>(k.duration),
                new Argument<double>(k.price),
                new Argument<int>(k.useCorporationWallet),
                new Argument<long>(k.container)
            }
        };

        public static readonly Command MarketBuyItem = new Command
        {
            Text = "marketBuyItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.marketItemID),
                new Argument<int>(k.useCorporationWallet),
                new Argument<int>(k.quantity)
            }
        };

        public static readonly Command MarketCreateBuyOrder = new Command
        {
            Text = "marketCreateBuyOrder",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.duration),
                new Argument<double>(k.price),
                new Argument<int>(k.useCorporationWallet)
            }
        };

        public static readonly Command MarketGetAveragePrices = new Command
        {
            Text = "marketGetAveragePrices",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.marketEID),
                new Argument<int>(k.definition),
                new Argument<int>(k.day)
            }
        };

        public static readonly Command MarketGlobalAveragePrices = new Command
        {
            Text = "marketGlobalAveragePrices",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.day),
            }
        };

        public static readonly Command MarketGetDefinitionAveragePrice = new Command
        {
            Text = "marketGetDefinitionAveragePrice",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command MarketAvailableItems = new Command
        {
            Text = "marketAvailableItems",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid)
            }
        };

        public static readonly Command MarketItemsInRange = new Command
        {
            Text = "marketItemsInRange",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command MarketTaxLogList = new Command
        {
            Text = "marketTaxLogList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.marketEID),
                new Argument<int>(k.offset),
            }
        };

        public static readonly Command MarketTaxChange = new Command
        {
            Text = "marketTaxChange",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.marketEID),
                new Argument<double>(k.tax),
            }
        };

        public static readonly Command MarketGetInfo = new Command
        {
            Text = "marketGetInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.eid)
            }
        };

        public static readonly Command SelectActiveRobot = new Command
        {
            Text = "selectActiveRobot",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<long>(k.containerEID),
            }
        };

        public static readonly Command GetRobotInfo = new Command
        {
            Text = "getRobotInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID)
            }
        };

        public static readonly Command GetRobotFittingInfo = new Command
        {
            Text = "getRobotFittingInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID)
            }
        };

        public static readonly Command SetRobotTint = new Command
        {
            Text = "setRobotTint",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<Color>(k.tint),
            }
        };

        public static readonly Command EquipModule = new Command
        {
            Text = "equipModule",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.moduleEID),
                new Argument<int>(k.slot),
                new Argument<string>(k.robotComponent)
            }
        };

        public static readonly Command RemoveModule = new Command
        {
            Text = "removeModule",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<long>(k.moduleEID),
            }
        };

        public static readonly Command ChangeModule = new Command
        {
            Text = "changeModule",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.source),
                new Argument<int>(k.target),
                new Argument<string>(k.sourceComponent)
            }
        };

        public static readonly Command EquipAmmo = new Command
        {
            Text = "equipAmmo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<long>(k.ammoEID),
                new Argument<long>(k.moduleEID)
            }
        };

        public static readonly Command UnequipAmmo = new Command
        {
            Text = "unEquipAmmo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<long>(k.moduleEID),
            }
        };

        public static readonly Command ChangeAmmo = new Command
        {
            Text = "changeAmmo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.robotEID),
                new Argument<long>(k.sourceModuleEID),
                new Argument<long>(k.targetModuleEID)
            }
        };

        public static readonly Command GetResearchLevels = new Command
        {
            Text = "getResearchLevels",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionRefine = new Command
        {
            Text = "productionRefine",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<long>(k.facility),
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command ProductionRefineQuery = new Command
        {
            Text = "productionRefineQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<long>(k.facility),
                new Argument<int>(k.amount)
            }
        };

        public static readonly Command ProductionReprocess = new Command
        {
            Text = "productionReprocess",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionReprocessQuery = new Command
        {
            Text = "productionReprocessQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long[]>(k.target),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionComponentsList = new Command
        {
            Text = "productionComponentsList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionFacilityInfo = new Command
        {
            Text = "productionFacilityInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionRepair = new Command
        {
            Text = "productionRepair",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long[]>(k.target),
            }
        };

        public static readonly Command ProductionInProgress = new Command
        {
            Text = "productionInProgress",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionCancel = new Command
        {
            Text = "productionCancel",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command ProductionRepairQuery = new Command
        {
            Text = "productionRepairQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long[]>(k.target),
            }
        };

        public static readonly Command ProductionServerInfo = new Command
        {
            Text = "productionServerInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command ProductionResearch = new Command
        {
            Text = "productionResearch",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.item),
                new Argument<long>(k.researchKitEID),
                new Argument<long>(k.facility),
                new Argument<int>(k.useCorporationWallet)
            }
        };

        public static readonly Command ProductionResearchQuery = new Command
        {
            Text = "productionResearchQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition),
                new Argument<int>(k.target),
                new Argument<long>(k.facility)
            }
        };

        public static readonly Command ProductionLineList = new Command
        {
            Text = "productionLineList",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility)
            }
        };

        public static readonly Command ProductionLineCalibrate = new Command
        {
            Text = "productionLineCalibrate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.eid),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionLineDelete = new Command
        {
            Text = "productionLineDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionLineStart = new Command
        {
            Text = "productionLineStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionCPRGInfo = new Command
        {
            Text = "productionCPRGInfo",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<long>(k.eid),
            }
        };

        public static readonly Command ProductionPrototypeStart = new Command
        {
            Text = "productionPrototypeStart",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<int>(k.definition),
                new Argument<int>(k.useCorporationWallet)
            }
        };

        public static readonly Command ProductionPrototypeQuery = new Command
        {
            Text = "productionPrototypeQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<int>(k.definition),
            }
        };

        public static readonly Command ProductionGetCprgFromLine = new Command
        {
            Text = "productionGetCPRGFromLine",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionGetCprgFromLineQuery = new Command
        {
            Text = "productionGetCPRGFromLineQuery",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID),
                new Argument<long>(k.facility),
            }
        };

        public static readonly Command ProductionLineSetRounds = new Command
        {
            Text = "productionLineSetRounds",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.facility),
                new Argument<int>(k.ID),
                new Argument<int>(k.rounds)
            }
        };

        public static readonly Command IntrusionSiteSetEffectBonus = new Command
        {
            Text = "intrusionSiteSetEffectBonus",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.target),
                new Argument<int>(k.effectType),
            }
        };

        public static readonly Command IntrusionSapItemInfo = new Command
        {
            Text = "intrusionSAPItemInfo",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command GetStabilityBonusThresholds = new Command
        {
            Text = "getStabilityBonusThresholds",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command GetIntrusionSiteInfo = new Command
        {
            Text = "getIntrusionSiteInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command IntrusionEnabler = new Command
        {
            Text = "intrusionEnabler",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.state)
            }
        };

        public static readonly Command IntrusionState = new Command
        {
            Text = "intrusionState",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command IntrusionGetPauseTime = new Command
        {
            Text = "intrusionGetPauseTime",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command IntrusionSetPauseTime = new Command
        {
            Text = "intrusionSetPauseTime",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TradeBegin = new Command
        {
            Text = "tradeBegin",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.traderID)
            }
        };

        public static readonly Command TradeCancel = new Command
        {
            Text = "tradeCancel",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TradeSetOffer = new Command
        {
            Text = "tradeSetOffer",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.credit)
            }
        };

        public static readonly Command TradeAccept = new Command
        {
            Text = "tradeAccept",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TradeRetractOffer = new Command
        {
            Text = "tradeRetractOffer",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TradeState = new Command
        {
            Text = "tradeState",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TradeOffer = new Command
        {
            Text = "tradeOffer",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TradeFinished = new Command
        {
            Text = "tradeFinished",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command MineralScanResultList = new Command
        {
            Text = "mineralScanResultList",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command MineralScanResultDelete = new Command
        {
            Text = "mineralScanResultDelete",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.items)
            }
        };

        public static readonly Command MineralScanResultMove = new Command
        {
            Text = "mineralScanResultMove",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int[]>(k.items),
                new Argument<string>(k.folder),
            }
        };

        public static readonly Command MineralScanResultCreateItem = new Command
        {
            Text = "mineralScanResultCreateItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.ID)
            }
        };

        public static readonly Command MineralScanResultUploadFromItem = new Command
        {
            Text = "mineralScanResultUploadFromItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.containerEID),
                new Argument<long>(k.itemEID),
            }
        };

        public static readonly Command TechTreeInfo = new Command
        {
            Text = "techTreeInfo",
            AccessLevel = AccessLevel.normal
        };

        public static readonly Command TechTreeUnlock = new Command
        {
            Text = "techTreeUnlock",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.definition)
            }
        };

        public static readonly Command TechTreeResearch = new Command
        {
            Text = "techTreeResearch",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.container),
                new Argument<long[]>(k.items),
            }
        };

        public static readonly Command TechTreeDonate = new Command
        {
            Text = "techTreeDonate",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<Dictionary<string, object>>(k.points)
            }
        };

        public static readonly Command TechTreeCorporationInfo = new Command
        {
            Text = "techTreeCorporationInfo",
            AccessLevel = AccessLevel.admin
        };

        public static readonly Command TechTreeGetLogs = new Command
        {
            Text = "techTreeGetLogs",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<int>(k.offset),
                new Argument<int>(k.duration),
            }
        };

        public static readonly Command EnableSelfTeleport = new Command
        {
            Text = "enableSelfTeleport",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.characterID),
                new Argument<int>(k.durationMinutes),
            }
        };

        public static readonly Command UseLotteryItem = new Command
        {
            Text = "useLotteryItem",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<long>(k.itemEID),
                new Argument<long>(k.containerEID),
            }
        };

        public static readonly Command GetRifts = new Command
        {
            Text = "getRifts",
            AccessLevel = AccessLevel.normal
        };


        //--------- admin tool commands -------- 

        // account list with extra character info for the admintool 
        public static readonly Command GetAccountsWithCharacters = new Command
        {
            Text = "getAccountsWithCharacters",
            AccessLevel = AccessLevel.admin,
        };

        public static readonly Command GetCharactersOnline = new Command
        {
            Text = "getCharactersOnline",
            AccessLevel = AccessLevel.admin,
        };

        public static readonly Command AccountGet = new Command
        {
            Text = "accountGet",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.accountID)
            }
        };

        // admin tool stuff
        // updates email,pass,acclevel
        //
        // open version: use the ChangeSessionEmail or ChangeSessionPassword commands
        public static readonly Command AccountUpdate = new Command
        {
            Text = "accountUpdate",
            AccessLevel = AccessLevel.toolAdmin,
            Arguments =
            {
                new Argument<int>(k.accountID)
            }
        };

        public static readonly Command ReimburseItem = new Command
        {
            Text = "ReimburseItem",
            AccessLevel = AccessLevel.admin
        };

        // creates an account from the tool. 
        // 
        // open version: use AccountOpenCreate
        public static readonly Command AccountCreate = new Command
        {
            Text = "accountCreate",
            AccessLevel = AccessLevel.toolAdmin,
            Arguments =
            {
                new Argument<string>(k.email),
                new Argument<int>(k.accessLevel),
                new Argument<string>(k.password)
            }
        };

        // create an account for yourself if the server is open
        public static readonly Command AccountOpenCreate = new Command
        {
            Text = "accountOpenCreate",
            AccessLevel = AccessLevel.notDefined,
            Arguments =
            {
                new Argument<string>(k.email),
                new Argument<string>(k.password)
            }
        };


        // changes the password of the sender's account - safe to be available always
        //
        // requires login -> no old pass or other validation is needed
        public static readonly Command ChangeSessionPassword = new Command
        {
            Text = "changeSessionPassword",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.password)
            }
        };

        // changes the email of the sender's account - safe, like the pass change
        // not yet implemented
        public static readonly Command ChangeSessionEmail = new Command
        {
            Text = "changeSessionEmail",
            AccessLevel = AccessLevel.normal,
            Arguments =
            {
                new Argument<string>(k.email)
            }
        };

        // confirm email for account. From GM interface.
        public static readonly Command AccountConfirmEmail = new Command
        {
            Text = "accountConfirmEmail",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.accountID)
            }
        };

        // ban account and disconnect if online
        //
        public static readonly Command AccountBan = new Command
        {
            Text = "accountBan",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.accountID),
                new Argument<int>(k.banLength),
            }
        };

        public static readonly Command AccountUnban = new Command
        {
            Text = "accountUnban",
            AccessLevel = AccessLevel.admin,
            Arguments =
            {
                new Argument<int>(k.accountID),
            }
        };

        public static readonly Command ServerInfoSet = new Command
        {
            Text = "serverInfoSet",
            AccessLevel = AccessLevel.toolAdmin,
            Arguments =
            {
                new Argument<string>(k.name),
                new Argument<string>(k.description),
                new Argument<string>(k.contact),
                new Argument<int>(k.isOpen),
                new Argument<int>(k.isBroadcast),
            }
        };

        // safe for open
        public static readonly Command ServerInfoGet = new Command
        {
            Text = "serverInfoGet",
            AccessLevel = AccessLevel.notDefined
        };

        public static readonly Command AccountDelete = new Command
        {
            Text = "accountDelete",
            AccessLevel = AccessLevel.toolAdmin,
            Arguments =
            {
                new Argument<int>(k.accountID),
            }
        };

    }
}
