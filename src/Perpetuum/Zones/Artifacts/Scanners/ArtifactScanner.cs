using System;
using System.Collections.Generic;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones.Artifacts.Generators;
using Perpetuum.Zones.Artifacts.Generators.Loot;
using Perpetuum.Zones.Artifacts.Repositories;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.Artifacts.Scanners
{
    public class ArtifactScanner : IArtifactScanner
    {
        private readonly IZone _zone;
        private readonly IArtifactRepository _artifactRepository;
        private readonly IArtifactGenerator _artifactGenerator;
        private readonly IArtifactLootGenerator _artifactLootGenerator;

        public ArtifactScanner(IZone zone,IArtifactRepository artifactRepository,IArtifactGenerator artifactGenerator,IArtifactLootGenerator artifactLootGenerator)
        {
            _zone = zone;
            _artifactRepository = artifactRepository;
            _artifactGenerator = artifactGenerator;
            _artifactLootGenerator = artifactLootGenerator;
        }

        public IEnumerable<ArtifactScanResult> Scan(Player player, int scanRange, double scanAccuracy)
        {
            _artifactGenerator.GenerateArtifacts();

            var scanPosition = player.CurrentPosition;
            var artifactsInScanRange = _artifactRepository.GetArtifacts();

            var scanResults = new List<ArtifactScanResult>();

            foreach (var artifact in artifactsInScanRange)
            {
                var dist = scanPosition.TotalDistance2D(artifact.Position);
                if (dist > scanRange)
                    continue;

                var scanResult = new ArtifactScanResult { scannedArtifact = artifact };

                if (dist < 3.0)
                {
                    player.SendArtifactRadarBeam(artifact.Position);

                    scanResult.estimatedPosition = artifact.Position;
                    scanResult.radius = 0.0;

                    _artifactRepository.DeleteArtifact(artifact);

                    CreateLoots(player, artifact);
                    SpawnNpcs(player, artifact);

                    var ep = _zone.Configuration.IsBeta ? 10 : 5;
                    if (_zone.Configuration.Type == ZoneType.Training) ep = 0;
                    if (ep > 0) player.Character.AddExtensionPointsBoostAndLog(EpForActivityType.Artifact, ep);

                    player.MissionHandler.EnqueueMissionEventInfo(new FindArtifactEventInfo(player, artifact.Info.type, artifact.Position));
                }
                else
                {
                    var radius = Math.Pow(dist, 1.5) / (scanAccuracy * 60);
                    var p = artifact.Position.GetRandomPositionInRange2D(-radius, radius);
                    scanResult.radius = p.TotalDistance2D(artifact.Position);
                    scanResult.estimatedPosition = p;
                }

                scanResults.Add(scanResult);
            }

            return scanResults;
        }

        private void CreateLoots(Player player,Artifact artifact)
        {
            var lootItems = _artifactLootGenerator.GenerateLoot(artifact);
            if ( lootItems == null )
                return;

            LootContainer.Create().SetOwner(player).SetEnterBeamType(BeamType.artifact_found).AddLoot(lootItems.LootItems).BuildAndAddToZone(_zone, lootItems.Position);
        }

        /// <summary>
        /// Spawn npc on regular artifact's position. This part is completely independent from missions.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="artifact"></param>
        private void SpawnNpcs(Player player, Artifact artifact)
        {
            //do the standard stuff if the presence is set
            if (artifact.Info.npcPresenceId == null) 
                return;

            var presence = _zone.AddDynamicPresenceToPosition((int) artifact.Info.npcPresenceId, artifact.Position);

            foreach (var npc in presence.Flocks.GetMembers())
            {
                var duration = artifact.Info.isPersistent ? TimeSpan.FromMinutes(5) : //sima artifactban rovid idore
                                                            TimeSpan.FromHours(1); //mission presence-ben hosszu idore vannak taggelve

                npc.Tag(player, duration);
                npc.AddDirectThreat(player,40 + FastRandom.NextDouble(0.0, 3.0));
            }

            _zone.CreateBeam(BeamType.teleport_storm, b => b.WithPosition(artifact.Position).WithDuration(TimeSpan.FromSeconds(100)));
        }
    }
}