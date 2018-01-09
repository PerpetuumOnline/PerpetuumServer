namespace Perpetuum.Zones.Blobs.BlobEmitters
{
    public interface IBlobEmitter
    {
        double BlobEmission { get; }
        double BlobEmissionRadius { get; }
    }
}