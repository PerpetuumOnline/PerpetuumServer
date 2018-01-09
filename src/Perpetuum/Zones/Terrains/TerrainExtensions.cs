using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Modules.Weapons;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.Zones.Terrains
{
    public static class TerrainExtensions
    {
        public static bool IsBlocked(this ITerrain terrain, Position position)
        {
            return terrain.IsBlocked((int) position.X, (int) position.Y);
        }

        public static bool IsBlocked(this ITerrain terrain,int x,int y)
        {
            return terrain.Blocks[x, y].Flags > 0;
        }

        public static void ClearPlantBlocking(this ITerrain terrain, Position position)
        {
            terrain.ClearPlantBlocking(position.intX,position.intY);
        }

        private static void ClearPlantBlocking(this ITerrain terrain, int x, int y)
        {
            terrain.Blocks.UpdateValue(x,y, bi =>
            {
                bi.Height = 0;
                bi.Plant = false;
                return bi;
            });
        }

        public static void PutPlant(this ITerrain terrain,int x, int y, byte state, PlantType plantType, PlantRule plantRule)
        {
            terrain.Plants.UpdateValue(x,y,pi =>
            {
                pi.SetPlant(state, plantType);
                pi.health = plantRule.Health[state];
                return pi;
            });

            if (plantRule.IsBlocking(state))
            {
                terrain.Blocks.UpdateValue(x,y,bi =>
                {
                    bi.Plant = true;
                    bi.Height = plantRule.GetBlockingHeight(state);
                    return bi;
                });
            }

            if (plantRule.PlacesConcrete)
            {
                terrain.Controls.UpdateValue(x,y,ci =>
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
            }
        }

        public static int CountPlantsInArea(this IZone zone, PlantType plantType, Area area)
        {
            var counter = 0;
            zone.ForEachAreaInclusive(area, (x, y) =>
            {
                var pi = zone.Terrain.Plants.GetValue(x, y);
                if (pi.type == plantType)
                {
                    counter++;
                }
            });
            return counter;
        }

        public static void ForEachAll(this IZone zone, Action<int, int> action)
        {
            for (var y = 0; y < zone.Size.Height; y++)
            {
                for (var x = 0; x < zone.Size.Width; x++)
                {
                    action(x, y);
                }
            }
        }

        // p1 <= p2
        public static void ForEachAreaInclusive(this IZone zone, Area area, Action<int, int> areaAction)
        {
            area = area.Clamp(zone.Size);

            for (var y = area.Y1; y <= area.Y2; y++)
            {
                for (var x = area.X1; x <= area.X2; x++)
                {
                    areaAction(x, y);
                }
            }
        }

        public static void DamageToPlantOnArea(this IZone zone,DamageInfo damageInfo)
        {
            var area = Area.FromRadius(damageInfo.sourcePosition, (int)damageInfo.Range);
            var damage = damageInfo.damages.Sum(d => d.value);
            var rangeFar = (int)damageInfo.Range;

            double originX = damageInfo.sourcePosition.intX;
            double originY = damageInfo.sourcePosition.intY;

            for (var y = area.Y1; y <= area.Y2; y++)
            {
                for (var x = area.X1; x <= area.X2; x++)
                {
                    var mult = MathHelper.DistanceFalloff(0, rangeFar, originX, originY, x, y);
                    var finalDamage = mult * damage;
                    if (finalDamage > 0)
                    {
                        zone.DamageToPlant(x, y, finalDamage);
                    }
                }
            }
        }

        public static void DamageToPlantOnArea(this IZone zone,Area area,double damage)
        {
            var w = area.Width/2;
            var h = area.Height/2;

            var maxd = w*w + h*h;

            var cx = area.Center.X;
            var cy = area.Center.Y;

            for (var y = area.Y1; y <= area.Y2; y++)
            {
                for (var x = area.X1; x <= area.X2; x++)
                {
                    var dx = cx - x;
                    var dy = cy - y;

                    var d = dx*dx + dy*dy;
                    var m = 1.0 - ((double)d/maxd);
                    
                    var dmg = damage*m;
                    zone.DamageToPlant(x, y, dmg);
                }
            }
        }

        public static void DamageToPlant(this IZone zone,int x, int y, double damage)
        {
            var currPlant = zone.Terrain.Plants[x, y];
            if (currPlant.type == PlantType.NotDefined || currPlant.health <= 0)
                return;

            var plantRule = zone.Configuration.PlantRules.GetPlantRule(currPlant.type);
            if (plantRule == null)
            {
                Logger.Error("plant rule was not found. plant type: " + currPlant.type + " zone:" + zone.Id);
                currPlant.Clear();
                zone.Terrain.Blocks[x,y] = new BlockingInfo();
                return;
            }

            if (FastRandom.NextDouble() < 0.3)
            {
                damage = damage/3;
            }

            var damageInt = (int)(damage * plantRule.DamageScale).Clamp(int.MinValue, int.MaxValue);
            if (damageInt <= 0)
            {

#if DEBUG
                //Console.WriteLine(plantRule.Type + " low damage");
#endif
                return;
            }

#if DEBUG
            //Console.WriteLine(plantRule.Type + " damage   " + damageInt);
#endif            

            int currentHealth = currPlant.health;
            currentHealth -= damageInt;

            if (currentHealth <= 0)
            {
                currPlant.Clear();
                currPlant.state = 1; //lehet t0bb fajta meghalasa most 1 van hasznalva

                zone.Terrain.Blocks[x,y] = new BlockingInfo();

                if (plantRule.PlacesConcrete)
                {
                    zone.Terrain.Controls.UpdateValue(x,y,ci =>
                    {
                        ci.ClearAllConcrete();
                        return ci;
                    });
                }
            }
            else
            {
                currPlant.health = (byte)(currentHealth.Clamp(0, 255));
            }

            zone.Terrain.Plants[x,y] = currPlant;
        }

        public static Position GetRandomPassablePosition(this IZone zone)
        {
            Position position;
            bool isPassable;
            do
            {
                position = zone.GetRandomIslandPosition();
                isPassable = zone.Terrain.IsPassable(position);
            } while (!isPassable);

            return position;
        }

        private static Position GetRandomIslandPosition(this IZone zone)
        {
            var counter = 0;
            while (true)
            {
                var xo = FastRandom.NextInt(0, zone.Size.Width - 1);
                var yo = FastRandom.NextInt(0, zone.Size.Height - 1);
                var p = new Position(xo, yo);

                var blockingInfo = zone.Terrain.Blocks.GetValue(xo, yo);
                if (!blockingInfo.Island)
                {
                    return p;
                }

                counter++;
                if (counter % 50 == 0)
                {
                    Thread.Sleep(1);
                }
            }
        }

        public static void UpdateAreaFromPacket(this ITerrain terrain, Packet packet)
        {
            var layerType = (LayerType)packet.ReadByte();
            packet.ReadByte(); // materialType
            packet.ReadByte(); // sizeOfElement;
            var x1 = packet.ReadInt();
            var y1 = packet.ReadInt();
            var x2 = packet.ReadInt();
            var y2 = packet.ReadInt();
            var area = new Area(x1, y1, x2, y2);

            var layer = terrain.GetLayerByType(layerType) as IUpdateableLayer;
            layer?.CopyFromStreamToArea(packet, area);
        }

        public static void UpdateNatureCube(this IZone zone,Area area,Action<NatureCube> updater)
        {
            var cube = new NatureCube(zone,area);
            cube.Check();
            
            var snapshot = cube.Clone();
            snapshot.Check();

            updater(snapshot);
            snapshot.Check();

            if ( cube.Equals(snapshot) )
                return;

            snapshot.Commit();

            var builder = Beam.NewBuilder().WithType(BeamType.nature_effect).WithDuration(8000);

            Task.Delay(40).ContinueWith(t => zone.CreateBeam(builder.WithPosition(area.GetRandomPosition())));
            Task.Delay(1500).ContinueWith(t => zone.CreateBeam(builder.WithPosition(area.GetRandomPosition())));
            Task.Delay(2500).ContinueWith(t => zone.CreateBeams(2,() => builder.WithPosition(area.GetRandomPosition())));
        }

        public static MineralLayer GetMineralLayerOrThrow(this ITerrain terrain, MaterialType type)
        {
            return terrain.GetMaterialLayer(type).ThrowIfNotType<MineralLayer>(ErrorCodes.NoSuchMineralOnZone);
        }

        private const int GZIP_THRESHOLD = 260;

        [CanBeNull]
        public static Packet BuildLayerUpdatePacket(this ITerrain terrain, LayerType layerType, Area area)
        {
            var layer = terrain.GetLayerByType(layerType) as IUpdateableLayer;
            if (layer == null)
                return null;

            var packet = new Packet(ZoneCommand.LayerUpdate);

            packet.AppendByte((byte)layerType);

            packet.AppendByte(0); // material
            packet.AppendByte((byte)layer.SizeInBytes);
            packet.AppendInt(area.X1);
            packet.AppendInt(area.Y1);
            packet.AppendInt(area.X2);
            packet.AppendInt(area.Y2);

            var compressed = false;
            var data = layer.CopyAreaToByteArray(area);
            if (data.Length > GZIP_THRESHOLD)
            {
                var compressedData = GZip.Compress(data);
                compressed = ((double)data.Length / compressedData.Length) > 1.0;

                if (compressed)
                {
                    data = compressedData;
                }
            }

            packet.AppendByte((byte)(compressed ? 1 : 0));
            packet.AppendInt(data.Length);
            packet.AppendByteArray(data);

            return packet;
        }

        public static bool IsPassable(this ITerrain terrain,Position position)
        {
            return terrain.IsPassable((int) position.X, (int) position.Y);
        }

        public static bool IsPassable(this ITerrain terrain,int x,int y)
        {
            if (terrain.Passable != null)
            {
                var isPassable = terrain.Passable.GetValue(x,y);
                if (!isPassable)
                    return false;
            }

            var bi = terrain.Blocks.GetValue(x,y);
            if (bi.Flags > 0)
                return false;

            return terrain.Slope.CheckSlope(x,y);
        }
    }
}
