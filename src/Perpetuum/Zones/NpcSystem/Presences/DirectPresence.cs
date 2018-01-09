using Perpetuum.IDGenerators;
using Perpetuum.Items.Templates;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public sealed class DirectPresence : DynamicPresence
    {
        private readonly IRobotTemplateRelations _robotTemplateRelations;
        private readonly FlockConfigurationBuilder.Factory _flockConfigurationBuilderFactory;

        public DirectPresence(IZone zone, PresenceConfiguration configuration,
                                          IRobotTemplateRelations robotTemplateRelations,
                                          FlockConfigurationBuilder.Factory flockConfigurationBuilderFactory) : base(zone, configuration)
        {
            _robotTemplateRelations = robotTemplateRelations;
            _flockConfigurationBuilderFactory = flockConfigurationBuilderFactory;
        }

        public IIDGenerator<int> FlockIDGenerator { get; set; }

        public IZoneMissionTarget MissionTarget { get; set; }

        /// <summary>
        /// Creates flocks for the related popNpc target
        /// </summary>
        public override void LoadFlocks()
        {
            if (MissionTarget.MyTarget.useQuantityOnly)
            {
                //do the new tech
                DoSelectNpcsFromPool();
            }
            else
            {
                //do the old school tech
                DoStrictDefinitionFlocks();
            }
        }

        private void DoStrictDefinitionFlocks()
        {
            //ekkor a mission target mar sqlbol jott, a definition meg a quantity ki van szamolva
            //ez van a kill targettel osszefuggesben
            var npcDefinition = MissionTarget.MyTarget.Definition;
            var amount = MissionTarget.MyTarget.Quantity;

            var builder = CreateFlockConfigurationBuilder()
                .WithDefinition(npcDefinition).With(c =>
                {
                    c.FlockMemberCount = amount;
                });
            var config = builder.Build();

            CreateAndAddFlock(config);
        }

        private FlockConfigurationBuilder CreateFlockConfigurationBuilder()
        {
            return _flockConfigurationBuilderFactory().WithIDGenerator(FlockIDGenerator);
        }

        private void DoSelectNpcsFromPool()
        {
            //new shit, build flocks etc....
            var level = MissionTarget.MyZoneMissionInProgress.MissionLevel; //mission level starts from 0
            var selectedRace = MissionTarget.MyZoneMissionInProgress.selectedRace;
           
            for (var i = 0; i < MissionTarget.MyTarget.Quantity; i++)
            {
                IRobotTemplateRelation npcTemplateRelation;

                var indyChance = FastRandom.NextDouble();
                if (indyChance > 0.15)
                {
                    //select from the mission's
                    npcTemplateRelation = _robotTemplateRelations.GetRandomByMissionLevelAndRaceID(level, selectedRace);
                    Logger.DebugInfo($" selected by race {npcTemplateRelation.EntityDefault.Name}");
                }
                else
                {
                    //somekind of saturation 15%
                    npcTemplateRelation = level == 0 ? _robotTemplateRelations.GetRandomDummyDecoyOthers() : 
                                                       _robotTemplateRelations.GetRandomIndustrialNpc(level);

                    Logger.DebugInfo($" selected as indy {npcTemplateRelation.EntityDefault.Name}");
                }

                var builder = CreateFlockConfigurationBuilder()
                    .With(c =>
                    {
                        c.EntityDefault = npcTemplateRelation.EntityDefault;
                        c.FlockMemberCount = 1;
                    });

                var config = builder.Build();
                CreateAndAddFlock(config);
            }
        }

    }
}
