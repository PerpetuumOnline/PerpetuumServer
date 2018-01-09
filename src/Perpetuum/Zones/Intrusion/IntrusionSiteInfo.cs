using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Intrusion
{
    public class IntrusionSiteInfo
    {
        private readonly Outpost _outpost;
        private readonly long? _owner;
        private readonly int _stability;
        private readonly double? _dockingStandingLimit;
        private readonly DateTime? _dockingControlLimit;
        private readonly DateTime? _setEffectControlTime;
        private readonly EffectType? _activeEffect;
        private readonly int _productionPoints;
        private readonly DateTime? _intrusionStartTime;
        private readonly double? _defenseStandingLimit;

        private IntrusionSiteInfo(Outpost outpost,long? owner, int stability, double? dockingStandingLimit, DateTime? dockingControlLimit, DateTime? setEffectControlTime, EffectType? activeEffect, int productionPoints, DateTime? intrusionStartTime, double? defenseStandingLimit)
        {
            _outpost = outpost;
            _owner = owner;
            _stability = stability;
            _dockingStandingLimit = dockingStandingLimit;
            _dockingControlLimit = dockingControlLimit;
            _setEffectControlTime = setEffectControlTime;
            _activeEffect = activeEffect;
            _productionPoints = productionPoints;
            _intrusionStartTime = intrusionStartTime;
            _defenseStandingLimit = defenseStandingLimit;
        }

        public long? Owner
        {
            get { return _owner; }
        }

        public IntrusionSiteInfo SetOwner(long? owner)
        {
            if (_owner == owner)
                return this;
            else
                return new IntrusionSiteInfo(_outpost,
                                             owner, 
                                             _stability, 
                                             _dockingStandingLimit, 
                                             _dockingControlLimit, 
                                             _setEffectControlTime, 
                                             _activeEffect, 
                                             _productionPoints, 
                                             _intrusionStartTime, 
                                             _defenseStandingLimit);
        }

        public int Stability
        {
            get { return _stability; }
        }

        public double? DockingStandingLimit
        {
            get { return _dockingStandingLimit; }
        }

        public DateTime? DockingControlLimit
        {
            get { return _dockingControlLimit; }
        }

        public DateTime? SetEffectControlTime
        {
            get { return _setEffectControlTime; }
        }

        public EffectType ActiveEffect
        {
            get { return _activeEffect ?? EffectType.undefined; }
        }

        public int ProductionPoints
        {
            get { return _productionPoints; }
        }

        public DateTime? IntrusionStartTime
        {
            get { return _intrusionStartTime; }
        }

        public double? DefenseStandingLimit
        {
            get { return _defenseStandingLimit; }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>();

            d["owner"] = Owner;
            d["stability"] = Stability;
            d["dockingstandinglimit"] = DockingStandingLimit;
            d["dockingcontroltime"] = DockingControlLimit;
            d["seteffectcontroltime"] = SetEffectControlTime;
            d["activeeffectid"] = ActiveEffect == EffectType.undefined ? (object) null : ActiveEffect;
            d["productionpoints"] = ProductionPoints;
            d["intrusionstarttime"] = IntrusionStartTime;
            d["defensestandinglimit"] = DefenseStandingLimit;

            return d;
        }

        public void SaveToDb()
        {
            Db.Query().CommandText(@"update intrusionsiteinfo 
                                    set owner = @owner,
                                        stability = @stability,
                                        dockingstandinglimit = @dockingstandinglimit,
                                        dockingcontroltime = @dockingcontroltime,
                                        seteffectcontroltime = @seteffectcontroltime,
                                        activeeffectid = @activeeffectid,
                                        productionpoints = @productionpoints,
                                        intrusionstarttime = @intrusionstarttime,
                                        defensestandinglimit = @defensestandinglimit
                             where siteeid = @siteeid")
                .SetParameter("@owner",_owner)
                .SetParameter("@stability",_stability)
                .SetParameter("@dockingstandinglimit",_dockingStandingLimit)
                .SetParameter("@dockingcontroltime",_dockingControlLimit)
                .SetParameter("@seteffectcontroltime",_setEffectControlTime)
                .SetParameter("@activeeffectid",_activeEffect)
                .SetParameter("@productionpoints",_productionPoints)
                .SetParameter("@intrusionstarttime",_intrusionStartTime)
                .SetParameter("@defensestandinglimit",_defenseStandingLimit)
                .SetParameter("@siteeid", _outpost.Eid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

        }

        [NotNull]
        public static IntrusionSiteInfo Get(Outpost outpost)
        {
            var record = Db.Query().CommandText("select * from intrusionsites where siteeid = @siteEid")
                .SetParameter("@siteEid", outpost.Eid)
                .ExecuteSingleRow().ThrowIfNull(ErrorCodes.ItemNotFound);

            var owner = record.GetValue<long?>("owner");
            var stability = record.GetValue<int>("stability");
            var dockingStandingLimit = record.GetValue<double?>("dockingstandinglimit");
            var dockingControlLimit = record.GetValue<DateTime?>("dockingcontroltime");
            var seteffectControlTime = record.GetValue<DateTime?>("seteffectcontroltime");
            var activeEffect = (EffectType?)record.GetValue<int?>("activeeffectid");
            var productionPoints = record.GetValue<int>("productionpoints");
            var intrusionStartTime = record.GetValue<DateTime?>("intrusionstarttime");
            var defenseStandingLimit = record.GetValue<double?>("defensestandinglimit");

            return new IntrusionSiteInfo(outpost,owner,
                                         stability,dockingStandingLimit,dockingControlLimit,
                                         seteffectControlTime,activeEffect,productionPoints,
                                         intrusionStartTime,defenseStandingLimit);
        }
    }
}