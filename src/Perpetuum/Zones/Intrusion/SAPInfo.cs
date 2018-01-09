using Perpetuum.EntityFramework;

namespace Perpetuum.Zones.Intrusion
{
    public class SAPInfo
    {
        private readonly EntityDefault _entityDefault;
        private readonly Position _position;

        public Position Position
        {
            get { return _position; }
        }

        public EntityDefault EntityDefault
        {
            get { return _entityDefault; }
        }

        public SAPInfo(EntityDefault entityDefault,Position position)
        {
            _entityDefault = entityDefault;
            _position = position;
        }

        public override string ToString()
        {
            return $"{_entityDefault} ({Position})";
        }
    }
}