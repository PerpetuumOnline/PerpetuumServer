using System;
using System.Linq;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.Terrains
{
    /// <summary>
    /// Static layer operations
    /// </summary>
    public static class LayerHelper
    {
        /// <summary>
        /// Creates an instance from each PlantType and growing state --
        /// </summary>
        public static void CreateGarden(IZone zone, int x, int y)
        {
            using (new TerrainUpdateMonitor(zone))
            {
                foreach (var rule in zone.Configuration.PlantRules.OrderBy(p=>(int)p.Type))
                {
                    for (byte i = 0; i < rule.GrowingStates.Count; i++)
                    {
                        var tmpI = i;
                        zone.Terrain.Plants.UpdateValue(x,y + i,pi =>
                        {
                            pi.type = rule.Type;
                            pi.state = tmpI;
                            return pi;
                        });

                        if (!rule.HasBlockingState) 
                            continue;

                        zone.Terrain.Blocks.UpdateValue(x,y + i,bi =>
                        {
                            bi.Height = rule.GetBlockingHeight(tmpI);
                            return bi;
                        });
                    }
                    x++;
                }
            }
        }

        /// <summary>
        /// Utility function to run an action within a circle
        /// </summary>
        private static void ProcessCircleHardEdge(IZone zone, Position origin, int radius, Action<int, int> action)
        {
            var width = zone.Size.Width;
            var height = zone.Size.Height;
            var area = Area.FromRadius(origin, radius);

            using (new TerrainUpdateMonitor(zone))
            {
                for (var y = area.Y1; y <= area.Y2; y++)
                {
                    for (var x = area.X1; x <= area.X2; x++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;

                        if (!origin.IsWithinRangeOf2D(x + 0.5, y + 0.5, radius)) continue;

                        action(x, y);
                    }
                }
            }
        }

        private static void UpdateControlInfoWithinRange(IZone zone, Position position, int radius,Func<TerrainControlInfo,TerrainControlInfo> updater)
        {
            ProcessCircleHardEdge(zone, position, radius, (x, y) =>
            {
                zone.Terrain.Controls.UpdateValue(x,y,updater);
            });
        }

        public static void SetTerrafomProtectionCircle(IZone zone,Position position, int radius, bool state = true)
        {
            UpdateControlInfoWithinRange(zone,position,radius, ci =>
            {
                ci.PBSTerraformProtected = state;
                return ci;
            });
        }

        public static void SetConcreteCircle(IZone zone, Position position, int radius)
        {
            ProcessCircleHardEdge(zone, position, radius, (x, y) =>
            {
                zone.Terrain.Controls.UpdateValue(x,y,ci =>
                {
                    if (FastRandom.NextDouble() > 0.5)
                    {
                        ci.ConcreteA = true;
                    }
                    else
                    {
                        ci.ConcreteB = true;
                    }

                    return ci;
                });
            });
        }

        public static void ClearPlantsCircle(IZone zone, Position position, int radius)
        {
            ProcessCircleHardEdge(zone, position, radius, (x, y) =>
            {
                zone.Terrain.Plants.UpdateValue(x,y,pi =>
                {
                    if (pi.type == PlantType.Wall) return pi;

                    var plantRule = zone.Configuration.PlantRules.GetPlantRule(pi.type);

                    if (plantRule != null && plantRule.IsBlocking(pi.state))
                    {
                        zone.Terrain.Blocks.UpdateValue(x,y, bi =>
                        {
                            if (!bi.Plant)
                                return bi;

                            bi.Plant = false;
                            bi.Height = 0;
                            return bi;
                        });
                    }

                    pi.Clear();
                    return pi;
                });
            });
        }


        public static void ClearConcreteCircle(IZone zone, Position position, int radius)
        {
            ProcessCircleHardEdge(zone, position, radius, (x, y) =>
            {
                zone.Terrain.Controls.UpdateValue(x,y,ci =>
                {
                    ci.ConcreteA = false;
                    ci.ConcreteB = false;
                    return ci;
                });
            });
        }
     
    }
}