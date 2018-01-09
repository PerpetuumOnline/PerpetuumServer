using System.Collections.Generic;

namespace Perpetuum.Zones.Terrains
{
    /// <summary>
    /// Helper class to hand layer type flags
    /// </summary>
    public class LayerTypeFlags
    {
        private byte _flags;

        public LayerTypeFlags(params LayerType[] layerTypes)
        {
            SetMany(layerTypes,true);
        }

        public bool HasFlag(LayerType layerType)
        {
            var bit = (byte)(1 << (int)layerType);
            return (_flags & bit) > 0;
        }

        public bool Any()
        {
            return _flags > 0;
        }

        public void SetAll(bool value)
        {
            _flags = (byte) (value ? 255 : 0);
        }

        public void SetMany(IList<LayerType> layerTypes,bool value)
        {
            if ( layerTypes == null )
                return;

            for (var i = 0; i < layerTypes.Count; i++)
            {
                Set(layerTypes[i],value);
            }
        }

        public void Set(LayerType layerType, bool value)
        {
            var bit = (byte)(1 << (int)layerType);

            if (value)
            {
                _flags |= bit;
            }
            else
            {
                unchecked
                {
                    _flags &= (byte)(~bit);                    
                }
            }
        }

        public IList<LayerType> GetLayerTypes()
        {
            var result = new List<LayerType>(8);

            var index = 0;
            var tmpFlags = _flags;
            while (tmpFlags > 0)
            {
                if ( (tmpFlags & 1) > 0 )
                {
                    result.Add((LayerType) index);
                }

                tmpFlags >>= 1;
                index++;
            }

            return result;
        }

        public override string ToString()
        {
            return $"LayerTypes: {GetLayerTypes().ArrayToString("|")}";
        }
    }
}