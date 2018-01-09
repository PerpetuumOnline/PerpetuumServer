using System.Collections.Generic;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public interface IMineralNodeRepository
    {
        void Insert(MineralNode node);
        void Update(MineralNode node);
        void Delete(MineralNode node);
        List<MineralNode> GetAll();
    }
}