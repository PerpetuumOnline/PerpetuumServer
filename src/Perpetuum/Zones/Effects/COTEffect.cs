
namespace Perpetuum.Zones.Effects
{
    /// <summary>
    /// Core over time effect 
    /// Reduces core on every tick
    /// </summary>
    public class CoTEffect : Effect
    {
        private double _corePerTick;

        public void SetCorePerTick(double value)
        {
            _corePerTick = value;
        }

        protected override void OnTick()
        {
            base.OnTick();
            
            if ( _corePerTick > 0 )
            {
                Owner.Core -= _corePerTick;
            }
        }
    }

}