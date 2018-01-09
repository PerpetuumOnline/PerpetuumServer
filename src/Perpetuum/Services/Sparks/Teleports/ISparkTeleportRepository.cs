using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.Sparks.Teleports
{
    public interface ISparkTeleportRepository : IRepository<int,SparkTeleport>
    {
        IEnumerable<SparkTeleport> GetAllByCharacter(Character character);
        IEnumerable<SparkTeleport> GetAllByDockingBase(DockingBase dockingBase);
    }
}