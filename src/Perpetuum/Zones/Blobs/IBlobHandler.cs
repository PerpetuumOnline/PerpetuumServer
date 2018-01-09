
using Perpetuum.Units;

namespace Perpetuum.Zones.Blobs
{
    public interface IBlobHandler
    {
        void UpdateBlob(Unit target);
        void ApplyBlobPenalty(ref double v, double modifier);
    }
}