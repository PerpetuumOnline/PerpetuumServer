using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.Sparks.Teleports
{
    public class SparkTeleportHelper
    {
        private readonly ISparkTeleportRepository _sparkTeleportRepository;

        public SparkTeleportHelper(ISparkTeleportRepository sparkTeleportRepository)
        {
            _sparkTeleportRepository = sparkTeleportRepository;
        }

        public SparkTeleport Get(int sparkTeleportID)
        {
            return _sparkTeleportRepository.Get(sparkTeleportID);
        }

        public SparkTeleport CreateSparkTeleport(DockingBase dockingBase, Character character)
        {
            var teleport = new SparkTeleport
            {
                Character = character,
                DockingBase = dockingBase
            };
            _sparkTeleportRepository.Insert(teleport);

            return teleport;
        }

        public void DeleteAndInform(SparkTeleport sparkTeleport)
        {
            _sparkTeleportRepository.Delete(sparkTeleport);

            Transaction.Current.OnCommited(() =>
            {
                var data = new Dictionary<string, object> { { k.description,sparkTeleport.ToDictionary() } };

                Message.Builder.SetCommand(Commands.SparkTeleportBaseDeleted)
                    .WithData(data)
                    .ToCharacter(sparkTeleport.Character)
                    .Send();
            });
        }

        public int GetCostFromSparkTeleports(Character character)
        {
            var t = _sparkTeleportRepository.GetAllByCharacter(character);
            return GetCostFromDescriptions(t);
        }

        public int GetCostFromDescriptions(IEnumerable<SparkTeleport> sparkTeleports)
        {
            return sparkTeleports.Select(t => t.DockingBase.Zone.Configuration.SparkCost).Sum();
        }

        public IEnumerable<SparkTeleport> GetAllSparkTeleports(Character character)
        {
            return _sparkTeleportRepository.GetAllByCharacter(character);
        }

        public int GetMaxSparkTeleportCount(Character character)
        {
            return character.GetExtensionLevelSummaryByName(ExtensionNames.SPARK_TELEPORT_COUNT_BASIC);
        }

        public IDictionary<string, object> GetSparkTeleportDescriptionInfos(Character character)
        {
            var result = new Dictionary<string, object>
            {
                {k.characterID, character.Id},
                {k.descriptions,GetAllSparkTeleports(character).ToDictionary("s", d => d.ToDictionary())},
                {k.maxAmount, GetMaxSparkTeleportCount(character)},
                {k.alreadySpent, GetCostFromSparkTeleports(character)}
            };

            return result;
        }

        public void DeleteAllSparkTeleports(DockingBase dockingBase)
        {
            var sparkTeleports = _sparkTeleportRepository.GetAllByDockingBase(dockingBase).ToArray();

            foreach (var sparkTeleport in sparkTeleports)
            {
                DeleteAndInform(sparkTeleport);
            }

            Logger.Info($"{sparkTeleports.Length} spark teleport targets were deleted from dockingBase: {dockingBase.InfoString}");
        }
    }
}