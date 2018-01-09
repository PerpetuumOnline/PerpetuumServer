using System.Collections.Generic;

namespace Perpetuum.Services.ExtensionService
{
 
    public class ExtensionPoints
    {
        public readonly Dictionary<int, int[]> points = new Dictionary<int, int[]>
                                                    {
                                                        {1, new[] {60, 120, 180, 240, 300, 720, 1260, 1920, 2700, 6000}},
                                                        {2, new[] {120, 240, 360, 480, 600, 1440, 2520, 3840, 5400, 12000}},
                                                        {3, new[] {180, 360, 540, 720, 900, 2160, 3780, 5760, 8100, 18000}},
                                                        {4, new[] {240, 480, 720, 960, 1200, 2880, 5040, 7680, 10800, 24000}},
                                                        {5, new[] {300, 600, 900, 1200, 1500, 3600, 6300, 9600, 13500, 30000}},
                                                        {6, new[] {360, 720, 1080, 1440, 1800, 4320, 7560, 11520, 16200, 36000}},
                                                        {7, new[] {420, 840, 1260, 1680, 2100, 5040, 8820, 13440, 18900, 42000}},
                                                        {8, new[] {480, 960, 1440, 1920, 2400, 5760, 10080, 15360, 21600, 48000}},
                                                        {9, new[] {540, 1080, 1620, 2160, 2700, 6480, 11340, 17280, 24300, 54000}},
                                                        {10, new[] {600, 1200, 1800, 2400, 3000, 7200, 12600, 19200, 27000, 60000}}
                                                    };

        public int GetNominalExtensionPoints(int extensionLevel, int extensionRank)
        {
            var nominalPoints = points[extensionRank][extensionLevel - 1];
            return nominalPoints;
        }
    }
}
