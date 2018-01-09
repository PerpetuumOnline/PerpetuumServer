using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Units;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public delegate IZoneUnitService ZoneUnitServiceFactory(IZone zone);

    public interface IZoneUnitService
    {
        Dictionary<Unit,Position> GetAll();

        void AddDefaultUnit(Unit unit,Position position,string syncPrefix,bool runtime);
        void UpdateDefaultUnit(Unit unit);
        void RemoveDefaultUnit(Unit unit,bool runtime);

        void AddUserUnit(Unit unit,Position position);
        void RemoveUserUnit(Unit unit);
    }

    public class ZoneUnitService : IZoneUnitService
    {
        public IZoneUnitRepository DefaultRepository { get; set; }
        public IZoneUnitRepository UserRepository { get; set; }

        public void AddDefaultUnit(Unit unit, Position position, string syncPrefix,bool runtime)
        {
            AddUnit(DefaultRepository,unit,position,syncPrefix,runtime);
        }

        public void UpdateDefaultUnit(Unit unit)
        {
            DefaultRepository.Update(unit);
        }

        public void RemoveDefaultUnit(Unit unit,bool runtime)
        {
            RemoveUnit(DefaultRepository,unit,runtime);
        }

        public void AddUserUnit(Unit unit,Position position)
        {
            AddUnit(UserRepository,unit,position,null,false);
        }

        public void RemoveUserUnit(Unit unit)
        {
            RemoveUnit(UserRepository,unit,false);
        }

        private static void AddUnit(IZoneUnitRepository repository,Unit unit,Position position,string syncPrefix,bool runtime)
        {
            if (!runtime)
            {
                unit.Save();
            }

            repository.Insert(unit,position,syncPrefix,runtime);
        }

        private static void RemoveUnit(IZoneUnitRepository repository,Unit unit, bool runtime)
        {
            repository.Delete(unit);
         
            if (!runtime)
            {
                Entity.Repository.Delete(unit);
            }
        }

        public Dictionary<Unit, Position> GetAll()
        {
            var result = new Dictionary<Unit,Position>();

            result.AddRange(DefaultRepository.GetAll());
            result.AddRange(UserRepository.GetAll());
            
            return result;
        }
    }
}