using System;

namespace Perpetuum.Zones.Terrains
{
    public class SlopeLayer : Layer<byte>
    {
        private readonly AltitudeLayer _altitudeLayer;

        public SlopeLayer(AltitudeLayer altitudeLayer) : base(LayerType.Slope,altitudeLayer.Width,altitudeLayer.Height)
        {
            _altitudeLayer = altitudeLayer;
//            _altitudeLayer.Updated += OnAltitudeUpdated;
//            _altitudeLayer.AreaUpdated += OnAltitudeAreaUpdated;

            UpdateSlopeByArea(Area.FromRectangle(0, 0, Width, Height));
        }

        private void OnAltitudeAreaUpdated(ILayer layer, Area area)
        {
            UpdateSlopeByArea(area);
        }

        private void OnAltitudeUpdated(ILayer layer, int x, int y)
        {
            UpdateSlope(x,y);
        }

        public void UpdateSlopeByArea(Area area)
        {
            for (var y = area.Y1 - 1; y < area.Y2 + 1; y++)
            {
                for (var x = area.X1 - 1; x < area.X2 + 1; x++)
                {
                    UpdateSlope(x, y);
                }
            }
        }

        public void UpdateSlope(int x, int y)
        {
            if ( x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            var value = CalculateSlope(x, y);
            SetValue(x, y, value);
        }

        private byte CalculateSlope(int x, int y)
        {
            var xo = (x + 1).Clamp(0, Width - 1);
            var yo = (y + 1).Clamp(0, Height - 1);

            var a = _altitudeLayer.GetAltitude(x, y);
            var b = _altitudeLayer.GetAltitude(xo, y);
            var c = _altitudeLayer.GetAltitude(xo, yo);
            var d = _altitudeLayer.GetAltitude(x, yo);

            var e = (a + b) >> 1;
            var f = (b + c) >> 1;
            var g = (c + d) >> 1;
            var h = (d + e) >> 1;
            var i = (a + b + c + d) >> 2;

            return (byte)((Math.Abs(i - a) +
                           Math.Abs(i - b) +
                           Math.Abs(i - c) +
                           Math.Abs(i - d) +
                           Math.Abs(i - e) +
                           Math.Abs(i - f) +
                           Math.Abs(i - g) +
                           Math.Abs(i - h)) * 2).Clamp(0, 255);
        }

        private const double MIN_SLOPE = 4.0;

        public bool CheckSlope(Position position, double slopeThreshold = MIN_SLOPE)
        {
            return CheckSlope((int) position.X, (int) position.Y, slopeThreshold);
        }


        /// <summary>
        /// Compares the layer slope value with the incoming threshold
        /// WARNING: very sensitive function, cannot be changed without tweaking the client as well
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="slopeThreshold">slope limit. usually comes from a unit's slope attribute</param>
        /// <returns></returns>
        public bool CheckSlope(int x, int y, double slopeThreshold = MIN_SLOPE)
        {
            var slope = GetValue(x, y) * 1024;
            var slopeThresholdInt = (int)(slopeThreshold * 1024 * 4);
            //tul meredek a terep ahova menni akar -> fail
            return slope < slopeThresholdInt;
        }
    }
}