using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using Perpetuum.Data;
using Perpetuum.Log;

namespace Perpetuum.EntityFramework
{
    public class DefinitionConfig
    {
        public static readonly DefinitionConfig None = new DefinitionConfig();

        public readonly int definition;
        public int? targetDefinition;
        public int? npcPresenceId;
        public double? item_work_range;
        public double? explosion_radius;
        public int? cycle_time;
        public double? damage_chemical;
        public double? damage_explosive;
        public double? damage_kinetic;
        public double? damage_thermal;
        public int? lifeTime;
        public int? activationTime;
        public int? waves;
        public bool? missionRelated;
        public int? constructionRadius;
        private double? _actionDelay;
        public int? deploy_radius;
        public int? transmitradius;
        public int? constructionlevelmax;
        public int? blockingradius;
        public int? chargeAmount;
        public int? inConnections;
        public int? outConnections;
        public double? coreTransferred;
        public double? transferEfficiency;
        public int? productionUpgradeAmount;
        public int? productionLevel;
        private double? _coreConsumption;
        public int? effectId;
        public double? coreKickStartThreshold;
        public int? reinforceCounterMax;
        public int? bandwidthUsage;
        public int? bandwidthCapacity;
        public int? emitRadius;
        public int? typeExclusiveRange;
        public int? network_node_range;

        private readonly double _hitSize;

        private readonly Color _tint = Color.White;

        private readonly double? _coreCalories;

        public double CoreCalories
        {
            get { return _coreCalories ?? 0; }
        }

        private DefinitionConfig()
        {
        }

        public double CoreConsumption
        {
            get
            {
                double v;
                if (_coreConsumption.TryGetValue(out v))
                    return v;

                Logger.Warning($"no coreconsumption found for definition: {definition}");
                v = 0;
                return v;
            }
        }

        public EntityDefault TargetEntityDefault
        {
            get { return EntityDefault.GetOrThrow(targetDefinition ?? 0); }
        }

        public int ConstructionRadius
        {
            get { return (int) constructionRadius.ThrowIfNull(ErrorCodes.ServerError); }
        }

        public Color Tint
        {
            get { return _tint; }
        }

        public TimeSpan ActionDelay
        {
            get { return TimeSpan.FromMilliseconds((double) _actionDelay.ThrowIfNull(ErrorCodes.ServerError)); }
        }

        public double HitSize => _hitSize;

        public DefinitionConfig(IDataRecord record)
        {
            definition = record.GetValue<int>("definition");
            targetDefinition = record.GetValue<int?>("targetdefinition");
            npcPresenceId = record.GetValue<int?>("npcpresenceid");
            item_work_range = record.GetValue<double?>("item_work_range");
            explosion_radius = record.GetValue<double?>("explosion_radius");
            cycle_time = record.GetValue<int?>("cycle_time");
            damage_chemical = record.GetValue<double?>("damage_chemical");
            damage_explosive = record.GetValue<double?>("damage_explosive");
            damage_kinetic = record.GetValue<double?>("damage_kinetic");
            damage_thermal = record.GetValue<double?>("damage_thermal");
            lifeTime = record.GetValue<int?>("lifetime");
            activationTime = record.GetValue<int?>("activationtime");
            waves = record.GetValue<int?>("waves");
            missionRelated = record.GetValue<bool?>("missionrelated");
            constructionRadius = record.GetValue<int?>("constructionradius");
            _actionDelay = record.GetValue<int?>("action_delay");
            deploy_radius = record.GetValue<int?>("deploy_radius");
            transmitradius = record.GetValue<int?>("transmitradius");
            constructionlevelmax = record.GetValue<int?>("constructionlevelmax");
            blockingradius = record.GetValue<int?>("blockingradius");
            chargeAmount = record.GetValue<int?>("chargeAmount");
            inConnections = record.GetValue<int?>("inconnections");
            outConnections = record.GetValue<int?>("outconnections");
            coreTransferred = record.GetValue<double?>("coretransferred");
            transferEfficiency = record.GetValue<double?>("transferefficiency");
            productionUpgradeAmount = record.GetValue<int?>("productionupgradeamount");
            productionLevel = record.GetValue<int?>("productionlevel");
            _coreConsumption = record.GetValue<double?>("coreconsumption");
            effectId = record.GetValue<int?>("effectid");
            _coreCalories = record.GetValue<double?>("corecalories") ?? 0;
            coreKickStartThreshold = record.GetValue<double?>("corekickstartthreshold");
            reinforceCounterMax = record.GetValue<int?>("reinforcecountermax");
            bandwidthUsage = record.GetValue<int?>("bandwidthusage");
            bandwidthCapacity = record.GetValue<int?>("bandwidthcapacity");
            emitRadius = record.GetValue<int?>("emitradius");
            typeExclusiveRange = record.GetValue<int?>("typeexclusiverange");
            network_node_range = record.GetValue<int?>("network_node_range");
            _hitSize = record.GetValue<double?>("hitsize") ?? 1.41;


            var tint = record.GetValue<string>("tint");

            if (!string.IsNullOrEmpty(tint))
            {
                _tint = ColorTranslator.FromHtml(tint);
            }
        }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.targetDefinition, targetDefinition},
                {k.item_work_range, item_work_range},
                {k.explosion_radius, explosion_radius},
                {k.cycle_time, cycle_time},
                {k.damage_chemical, damage_chemical},
                {k.damage_explosive, damage_explosive},
                {k.damage_kinetic, damage_kinetic},
                {k.damage_thermal, damage_thermal},
                {k.lifeTime, lifeTime},
                {k.activationTime, activationTime},
                {k.waves, waves},
                {k.constructionRadius, constructionRadius},
                {k.action_delay, _actionDelay},
                {k.deploy_radius, deploy_radius},
                {k.transmitRadius, transmitradius},
                {k.constructionLevelMax, constructionlevelmax},
                {k.blockingRadius, blockingradius},
                {k.chargeAmount, chargeAmount},
                {k.inConnections, inConnections},
                {k.outConnections, outConnections},
                {k.coreTransferred, coreTransferred},
                {k.transferEfficiency, transferEfficiency},
                {k.productionLevel, productionLevel},
                {k.productionUpgradeAmount, productionUpgradeAmount},
                {k.coreConsumption, _coreConsumption},
                {k.effectType, effectId},
                {k.coreCalories,_coreCalories},
                {k.coreKickStartThreshold, coreKickStartThreshold},
                {k.reinforceCounterMax, reinforceCounterMax},
                {k.bandwidthUsage, bandwidthUsage},
                {k.bandwidthCapacity, bandwidthCapacity},
                {k.emitRadius, emitRadius},
                {k.typeExclusiveRange, typeExclusiveRange},
                {k.networkNodeRange, network_node_range},
            };
        }
    }
}