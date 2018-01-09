using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Gangs;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{
    public class RandomCalibrationProgram : CalibrationProgram
    {
        private readonly IGangManager _gangManager;

        public RandomCalibrationProgram(IProductionDataAccess productionDataAccess,IGangManager gangManager) : base(productionDataAccess)
        {
            _gangManager = gangManager;
        }

        public override void OnInsertToDb()
        {
            base.OnInsertToDb();

            // hogy irja felul a base-t
            MaterialEfficiencyPoints = 100;
            TimeEfficiencyPoints = 100;
        }

        public override int TargetDefinition
        {
            get
            {
                var target =
                    DynamicProperties.GetOrAdd(k.target, 0);

                if (target == 0)
                {
                    target = OriginalTargetDefinition;
                    DynamicProperties.Set(k.target, target);
                    Logger.Info("default behaviour, RandomCPRG is falling back to original target. CPRG:" + this.Eid);
                }

                return target;

            }
        }

        public override string ToString()
        {
            var info = base.ToString();

            if (DynamicProperties.Contains(k.target))
            {
                info += " RandomTarget: " + TargetDefinition;
            }
            
            return info;
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            info[k.components] = GetComponentDictionary;
            info[k.targetQuantity] = TargetQuantity;
            return info;
        }


        public int OriginalTargetDefinition
        {
            get { return LookUpTargetFromConfig(); }
        }

        public bool IsResultingOriginalTarget
        {
            get { return OriginalTargetDefinition == TargetDefinition; }
        }



        public override List<ProductionComponent> Components
        {
            get { return ParseDictionary; }
        }

        public Dictionary<string, object> GetComponentDictionary
        {
            get
            {
                var componentsDict =
                DynamicProperties.GetOrAdd(k.components, new Dictionary<string, object>());

                if (componentsDict.Count == 0)
                {
                    // The ct is used without set components

                    Logger.Warning("no production components found, falling back to fake in CPRG:" + this.Eid);

                    var fakeComponents = new List<ProductionComponent>();

                    var comp1 = new ProductionComponent(EntityDefault.GetByName(DefinitionNames.TITANIUM), 100);
                    var comp2 = new ProductionComponent(EntityDefault.GetByName(DefinitionNames.POLYNUCLEIT), 100);

                    fakeComponents.Add(comp1);
                    fakeComponents.Add(comp2);

                    SetComponents(fakeComponents); //fake stuff

                    this.Save();

                    return GetComponentDictionary;
                }

                return componentsDict;
            }
        }

        public List<ProductionComponent> ParseDictionary
        {
            get
            {
                
                var dict = GetComponentDictionary;
                var resultList = new List<ProductionComponent>(dict.Count);

                foreach (var kvp in dict)
                {
                    var entry = (Dictionary<string,object>)kvp.Value;

                    var definition = (int)entry[k.definition];
                    var quantity = (int)entry[k.quantity];

                    var ed = EntityDefault.Get(definition);

                    var pc = new ProductionComponent(ed, quantity);

                    resultList.Add(pc);

                }

                return resultList;
            }

        }

        public void SetComponents(List<ProductionComponent> components)
        {
            var count = 1;
            var result = new Dictionary<string, object>(components.Count);

            foreach (var productionComponent in components)
            {
                var entry = new Dictionary<string, object>
                {
                    {k.definition, productionComponent.EntityDefault.Definition},
                    {k.quantity, productionComponent.Amount}
                };

                result.Add("c"+count++, entry);

            }

            DynamicProperties.Set(k.components, result);

            
        }

        public void SetTargetDefinition(int definition)
        {
            DynamicProperties.Set(k.target,definition);
            Logger.Info("target definition got set in CPRG:" + this.Eid);
        }


        public override bool HasComponents
        {
            get { return true; }
        }

        public void SetComponentsFromRunningTargets(Character character)
        {
            var gang = _gangManager.GetGangByMember(character);
            if (gang == null)
            {
                var missionGuid = CollectComponentsFromIndustrialMissions(character);
                if (missionGuid == Guid.Empty)
                {
                    Logger.Info("not in gang, not in mission, falling back to fake components. CPRG:" + this.Eid);
                }
                return;
            }

            var gangMembers = gang.GetOnlineMembers().ToArray();
            if (gangMembers.Length == 1)
            {
                //alone in gang
                Logger.Info("alone in gang, and not in mission. falling back to fake components. CPRG:" + this.Eid);
            }

            foreach (var gangMember in gangMembers)
            {
                var missionGuid = CollectComponentsFromIndustrialMissions(gangMember);
                if (missionGuid != Guid.Empty)
                {
                    return;
                }
            }
        }

        private Guid CollectComponentsFromIndustrialMissions(Character character)
        {
            Guid missionGuid;
            if (MissionHelper.FindIndustrialMissionGuidWithConditions(character, Definition, out missionGuid))
            {
                //the current character has this kind of target in mission
                CollectComponentsFromMission(character, missionGuid);

                Logger.Info("components set for character in CPRG:" + this.Eid);
                return missionGuid;
            }
            return Guid.Empty;
        }

        private void CollectComponentsFromMission(Character character, Guid missionGuid)
        {
            var missionInProgress =
            MissionHelper.ReadMissionInProgressByGuid(missionGuid, character);

            if (missionInProgress == null)
            {
                Logger.Error("mission was not found: " + missionGuid);
                return;
            }

            missionInProgress.CollectComponentsForCPRG(this);

        }

        public override bool IsMissionRelated
        {
            get { return true; }
        }

        public override int TargetQuantity
        {
            get { return DynamicProperties.GetOrDefault<int>(k.amount); }
            set { DynamicProperties.Set(k.amount, value); }
        }
    }
}
