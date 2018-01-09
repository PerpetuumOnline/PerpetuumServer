using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;

namespace Perpetuum.Items.Templates
{
    public static class RobotTemplateRelationsExtensions
    {
        private const double LEVEL_FRACTION = 0.4;
        private const int INDY_RACE = 4;

        [CanBeNull]
        public static IRobotTemplateRelation GetRandomDummyDecoyOthers(this IRobotTemplateRelations relations)
        {
            return relations.GetRandomIndustrialNpc(0);
        }

        [CanBeNull]
        public static IRobotTemplateRelation GetRandomIndustrialNpc(this IRobotTemplateRelations relations,int level)
        {
            return relations.GetAll().Where(r => r.HasMissionLevel && r.MissionLevel >= 0 && r.MissionLevel <= level).FilterByRaceID(INDY_RACE).RandomElement();
        }

        
        [CanBeNull]
        public static IRobotTemplateRelation GetRandomByMissionLevelAndRaceID(this IRobotTemplateRelations relations,int missionLevel,int raceID)
        {
            return relations.GetAll().Where(r => r.HasMissionLevel).FilterByMissionLevel(missionLevel).FilterByRaceID(raceID).RandomElement();
        }

        public static IEnumerable<IRobotTemplateRelation> FilterByMissionLevel(this IEnumerable<IRobotTemplateRelation> relations,int missionLevel)
        {
            var missionLevelFraction = (int)(Math.Floor(missionLevel * LEVEL_FRACTION));
            var lowerBound = missionLevel - missionLevelFraction;
            return relations.Where(r => r.HasMissionLevel && lowerBound <= r.MissionLevel && r.MissionLevel <= missionLevel);
        }

        public static IEnumerable<IRobotTemplateRelation> FilterByRaceID(this IEnumerable<IRobotTemplateRelation> relations,int raceID)
        {
            return relations.Where(r => r.RaceID == raceID);
        }

        public static RobotTemplate GetStarterMaster(this IRobotTemplateRelations relations,bool equipped = true)
        {
            return equipped ? relations.EquippedDefault : relations.UnequippedDefault;
        }

        public static RobotTemplate GetRelatedTemplate(this IRobotTemplateRelations relations,EntityDefault ed)
        {
            return relations.GetRelatedTemplate(ed.Definition);
        }

        [CanBeNull]
        public static RobotTemplate GetRelatedTemplate(this IRobotTemplateRelations relations,int definition)
        {
            var relation = relations.Get(definition);
            return relation?.Template;
        }

        public static RobotTemplate GetRelatedTemplateOrDefault(this IRobotTemplateRelations relations,EntityDefault ed)
        {
            return relations.GetRelatedTemplateOrDefault(ed.Definition);
        }

        public static RobotTemplate GetRelatedTemplateOrDefault(this IRobotTemplateRelations relations,int definition)
        {
            var template = relations.GetRelatedTemplate(definition);
            if (template != null)
                return template;

            Logger.Warning("robot template was not found for definition: " + definition + ", falling back to starter_master.");
            //fallback to arkhe
            return relations.EquippedDefault;
        }
    }
}