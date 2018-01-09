using Perpetuum.Builders;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.Teleporting
{
    public class TeleportDescriptionBuilder : IBuilder<TeleportDescription>
    {
        private readonly IZoneManager _zoneManager;
        private readonly UnitHelper _unitHelper;
        private int _id;
        private string _description;
        private TeleportDescriptionType _type;
        private Teleport _sourceTeleport;
        private Teleport _targetTeleport;
        private IZone _sourceZone;
        private IZone _targetZone;
        private int? _sourceRange;
        private int _targetRange;
        private int _timeout;
        private bool _listable;
        private bool _active;
        private Position? _landingSpot;

        public delegate TeleportDescriptionBuilder Factory();

        public Teleport SourceTeleport => _sourceTeleport;
        public Teleport TargetTeleport => _targetTeleport;

        public TeleportDescriptionBuilder(IZoneManager zoneManager,UnitHelper unitHelper)
        {
            _zoneManager = zoneManager;
            _unitHelper = unitHelper;
        }

        public TeleportDescriptionBuilder SetId(int id)
        {
            _id = id;
            return this;
        }

        public TeleportDescriptionBuilder SetDescription(string description)
        {
            _description = description;
            return this;
        }

        public TeleportDescriptionBuilder SetType(TeleportDescriptionType type)
        {
            _type = type;
            return this;
        }

        public TeleportDescriptionBuilder SetSourceTeleport(long teleportEid)
        {
            var teleport = _unitHelper.GetUnit<Teleport>(teleportEid);
            return SetSourceTeleport(teleport);
        }

        public TeleportDescriptionBuilder SetSourceTeleport(Teleport teleport)
        {
            _sourceTeleport = teleport;
            return SetSourceZone(_sourceTeleport?.Zone);
        }

        public TeleportDescriptionBuilder SetSourceZone(int zoneID)
        {
            var zone = _zoneManager.GetZone(zoneID);
            return SetSourceZone(zone);
        }

        public TeleportDescriptionBuilder SetSourceZone(IZone zone)
        {
            _sourceZone = zone;
            return this;
        }

        public TeleportDescriptionBuilder SetSourceRange(int? range)
        {
            _sourceRange = range;
            return this;
        }

        public TeleportDescriptionBuilder SetTargetZone(int zoneID)
        {
            var zone = _zoneManager.GetZone(zoneID);
            return SetTargetZone(zone);
        }

        public TeleportDescriptionBuilder SetTargetZone(IZone zone)
        {
            _targetZone = zone;
            return this;
        }

        public TeleportDescriptionBuilder SetTargetTeleport(long teleportEid)
        {
            var teleport = _unitHelper.GetUnit<Teleport>(teleportEid);
            return SetTargetTeleport(teleport);
        }

        public TeleportDescriptionBuilder SetTargetTeleport(Teleport teleport)
        {
            _targetTeleport = teleport;
            var targetTeleportZone = _targetTeleport?.Zone;
            if (targetTeleportZone != null)
            {
                SetTargetZone(targetTeleportZone);
            }
            return this;
        }

        public TeleportDescriptionBuilder SetTargetRange(int range)
        {
            _targetRange = range;
            return this;
        }

        public TeleportDescriptionBuilder UseTimeout(int timeout)
        {
            _timeout = timeout;
            return this;
        }

        public TeleportDescriptionBuilder SetListable(bool listable)
        {
            _listable = listable;
            return this;
        }

        public TeleportDescriptionBuilder SetActive(bool active)
        {
            _active = active;
            return this;
        }

        public TeleportDescriptionBuilder SetLandingSpot(Position landingSpot)
        {
            _landingSpot = landingSpot;
            return this;
        }

        public TeleportDescriptionBuilder SelectTypeByZones()
        {
            var type = _sourceZone == _targetZone
                ? TeleportDescriptionType.WithinZone
                : TeleportDescriptionType.AnotherZone;
            return SetType(type);
        }

        public TeleportDescription Build()
        {
            var td = new TeleportDescription();
            td.id = _id;
            td.description = _description;
            td.descriptionType = _type;
            td.SourceZone = _sourceZone;
            td.SourceTeleport = _sourceTeleport;
            td.sourceRange = _sourceRange;
            td.TargetZone = _targetZone;
            td.TargetTeleport = _targetTeleport;
            td.targetRange = _targetRange;
            td._useTimeout = _timeout;
            td.active = _active;
            td.listable = _listable;
            td.landingSpot = _landingSpot;
            return td;
        }
    }
}