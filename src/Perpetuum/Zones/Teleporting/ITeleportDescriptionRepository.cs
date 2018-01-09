using System.Collections.Generic;

namespace Perpetuum.Zones.Teleporting
{
    public interface ITeleportDescriptionRepository
    {
        void UpdateActive(TeleportDescription description);
        void Insert(TeleportDescription description);
        IEnumerable<TeleportDescription> GetAll();
    }
}