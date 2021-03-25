using Perpetuum.Services.RiftSystem;

namespace Perpetuum.Services.EventServices.EventMessages
{
    public class SpawnPortalMessage : EventMessage
    {
        public Position SourcePosition { get; private set; }
        public int SourceZone { get; private set; }
        public CustomRiftConfig RiftConfig { get; private set; }
        public SpawnPortalMessage(int sourceZone, Position srcPosition, CustomRiftConfig riftConfig)
        {
            SourcePosition = srcPosition;
            SourceZone = sourceZone;
            RiftConfig = riftConfig;
        }
    }
}
