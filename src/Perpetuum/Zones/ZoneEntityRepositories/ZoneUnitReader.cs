using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public abstract class ZoneUnitReader : IZoneUnitReader
    {
        private readonly UnitHelper _unitHelper;

        public ZoneUnitReader(UnitHelper unitHelper)
        {
            _unitHelper = unitHelper;
        }

        [CanBeNull]
        protected Unit CreateUnit(IDataRecord record)
        {
            var eid = record.GetValue<long>("eid");

            try
            {
                Unit unit;
                var runtime = record.GetValueOrDefault<bool>("runtime");
                if (runtime)
                {
                    var definition = record.GetValue<int?>("definition") ?? 0;
                    if (definition == 0)
                    {
                        Logger.Error("ZoneUnitReader: definition is null! eid = " + eid);
                        return null;
                    }
                    unit = _unitHelper.CreateUnit(definition, EntityIDGenerator.Fix(eid));
                    unit.Owner = record.GetValue<long?>("owner") ?? 0L;
                    unit.Name = record.GetValue<string>("ename");
                }
                else
                {
                    unit = _unitHelper.LoadUnit(eid);
                }

                var orientation = record.GetValue<byte>("orientation");
                unit.Orientation = (double)orientation / byte.MaxValue;
                return unit;
            }
            catch (Exception ex)
            {
                Logger.Error("ZoneUnitReader: CreateUnit error. eid = " + eid);
                Logger.Exception(ex);
            }

            return null;
        }

        public abstract Dictionary<Unit, Position> GetAll();
    }
}