using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Zones;
using Perpetuum.Zones.Terrains;
using System;
using System.Collections.Generic;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawRamp : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var width = request.Data.GetOrDefault(k.size, 5);
            var edge = request.Data.GetOrDefault(k.range, 0.4);
            var useMax = request.Data.GetOrDefault(k.max, 1) == 1;
            var x = request.Data.GetOrDefault<int>("positionx");
            var y = request.Data.GetOrDefault<int>("positiony");
            var fullBlend = request.Data.GetOrDefault("blend", 1.0);

            width = width.Clamp(0, 40);

            fullBlend = fullBlend.Clamp();
            var zone = request.Zone;
            zone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);

            Player player;
            if (zone.TryGetPlayer(request.Session.Character, out player))
            {
                var targetPosition = new Position(x, y);
                var sourcePosition = player.CurrentPosition;

                if (sourcePosition.TotalDistance2D(targetPosition) > 800)
                {
                    throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                }

                DrawRamp(zone, sourcePosition, targetPosition, width, edge, fullBlend, useMax);
            }


        }

        static double MixValues(double start, double end, double sampleBetween)
        {
            var diff = end - start;
            var diffAtSample = diff * sampleBetween;
            return start + diffAtSample;
        }

        private static void DrawRamp(IZone zone, Position sourcePosition, Position targetPosition, int rampWidth, double edge, double fullBlend, bool setIfMax = false)
        {
            // The Altitude layer is a ushort, but can be read as a decimal.
            // The decimal representation can be converted to ushort by dividing by 32
            // I suggest we work with just one unit and do a conversion
            var work = new List<RampSample>();

            sourcePosition = sourcePosition.Center;
            targetPosition = targetPosition.Center;

            var terrain = zone.Terrain;

            var subSamples = 5;

            var sourceAltitude = terrain.Altitude.GetAltitudeAsDouble(sourcePosition);
            var targetAltitude = terrain.Altitude.GetAltitudeAsDouble(targetPosition);

            var totalDistance = sourcePosition.TotalDistance2D(targetPosition);

            var directionVector = targetPosition - sourcePosition;
            var directionUnit = directionVector / directionVector.lengthDouble2D;
            var directionMicro = directionUnit / subSamples;

            var directionUnitLeft = directionUnit.RotateAroundOrigo(Math.PI / 2);
            var directionUnitRight = directionUnit.RotateAroundOrigo(-1 * Math.PI / 2);

            var leftMicro = directionUnitLeft / subSamples;
            var rightMicro = directionUnitRight / subSamples;

            for (var i = 0; i < totalDistance; i++)
            {
                var centerPosition = (directionUnit * i) + sourcePosition;

                for (var j = 0; j < rampWidth; j++)
                {

                    for (var k = 0; k < subSamples; k++)
                    {

                        var cMicroPos = directionMicro * k + centerPosition;

                        var leftCenter = (directionUnitLeft * j) + cMicroPos;
                        var rightCenter = (directionUnitRight * j) + cMicroPos;

                        var distanceFromStart = cMicroPos.TotalDistance2D(sourcePosition);
                        var dirBlend = distanceFromStart / totalDistance;

                        var dirAltitude = MixValues(sourceAltitude, targetAltitude, dirBlend);

                        for (var l = 0; l < subSamples; l++)
                        {
                            var cMicroLeft = leftMicro * l + leftCenter;
                            var cMicroRight = rightMicro * l + rightCenter;


                            var sideDist = cMicroLeft.TotalDistance2D(cMicroPos);

                            var blend = sideDist / rampWidth;

                            var mix = 0.0;
                            if (blend > 1 - edge)
                            {
                                mix = (blend - (1 - edge)) * 1 / edge;
                            }


                            var leftRamp = new RampSample
                            {
                                altitude = dirAltitude,
                                mix = mix,
                                position = cMicroLeft,
                            };

                            var rightRamp = new RampSample
                            {
                                altitude = dirAltitude,
                                mix = mix,
                                position = cMicroRight,
                            };

                            work.Add(rightRamp);
                            work.Add(leftRamp);
                        }
                    }


                }

            }

            var dict = new Dictionary<long, RampSample>();

            Console.WriteLine("drawing " + work.Count + " rampsamples.");

            foreach (var rampSample in work)
            {

                var key = rampSample.position.intX + rampSample.position.intY * zone.Size.Width;

                RampSample s;
                if (dict.TryGetValue(key, out s))
                {

                    s.altitude += rampSample.altitude;
                    s.mix += rampSample.mix;
                    s.samples += 1;

                }
                else
                {
                    var q = new RampSample
                    {
                        altitude = rampSample.altitude,
                        mix = rampSample.mix,
                        position = rampSample.position.Center,
                        samples = 1,
                    };

                    dict.Add(key, q);
                }
            }

            using (var terrainUpdateMonitor = new TerrainUpdateMonitor(zone))
            {
                int minX = 2048;
                int minY = 2048;
                int maxX = 0;
                int maxY = 0;
                foreach (var rampSample in dict.Values)
                {
                    var mix = rampSample.mix / rampSample.samples;
                    var avgAlt = rampSample.altitude / rampSample.samples;

                    var origAlt = zone.Terrain.Altitude.GetAltitudeAsDouble(rampSample.position.intX, rampSample.position.intY);

                    var altVal = MixValues(avgAlt, origAlt, mix);
                    var fullBlended = MixValues(origAlt, altVal, fullBlend);

                    var finalAlt = fullBlended;
                    if (setIfMax)
                    {
                        finalAlt = Math.Max(origAlt, fullBlended);
                    }
                    if (finalAlt == 0)
                    {
                        finalAlt = origAlt;
                    }
                    var rx = rampSample.position.intX;
                    var ry = rampSample.position.intY;

                    minX = Math.Min(minX, rx);
                    maxX = Math.Max(maxX, rx);
                    minY = Math.Min(minY, ry);
                    maxY = Math.Max(maxY, ry);

                    var shortAlt = System.Convert.ToUInt16(finalAlt * 32);

                    zone.Terrain.Altitude.SetValue(rx, ry, shortAlt);
                }
                zone.Terrain.Slope.UpdateSlopeByArea(new Area(minX, minY, maxX, maxY));
            }
        }

        private class RampSample
        {
            public double altitude;
            public double mix;
            public Position position;

            public int samples;

        }

    }
}
