using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Perpetuum.IO;
using Perpetuum.Services.EventServices;
using Perpetuum.Zones.Terrains.Materials.Minerals.Generators;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class GravelConfiguration : IMineralConfiguration
    {
        public GravelConfiguration(IZone zone)
        {
            ZoneId = zone.Id;
            Type = MaterialType.Gravel;
            ExtractionType = MineralExtractionType.Solid;
            MaxNodes = 1;
            TotalAmountPerNode = 1500;
        }

        public int ZoneId { get; private set; }
        public MaterialType Type { get; private set; }
        public MineralExtractionType ExtractionType { get; private set; }
        public int MaxNodes { get; private set; }
        public int MaxTilesPerNode { get; private set; }
        public int TotalAmountPerNode { get; private set; }
        public double MinThreshold { get; private set; }
    }

    public class GravelRepository : IMineralNodeRepository
    {
        private readonly IFileSystem _fileSystem;

        public GravelRepository(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void Insert(MineralNode node)
        {
        }

        public void Update(MineralNode node)
        {
        }

        public void Delete(MineralNode node)
        {
        }

        public List<MineralNode> GetAll()
        {
            var bmp = (Bitmap)Image.FromFile(_fileSystem.CreatePath( Path.Combine("layers",  "mineral_gravel.0045.png")));

            var minerals = new Dictionary<Point, uint>();

            int minx = int.MaxValue, miny = int.MaxValue, maxx = 0, maxy = 0;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);

                    var b = c.GetBrightness() * 350000;

                    if (b > 0)
                    {
                        minx = Math.Min(minx, x);
                        miny = Math.Min(miny, y);
                        maxx = Math.Max(maxx, x);
                        maxy = Math.Max(maxy, y);
                        
                        var point = new Point(x, y);
                        minerals.Add(point, (uint)b);
                    }
                }
            }

            var area = new Area(minx, miny, maxx, maxy);
            var node = new MineralNode(MaterialType.Gravel,area) { Expirable = false };

            foreach (var t in minerals)
            {
                node.SetValue(t.Key, t.Value);
            }

            return new List<MineralNode> { node };
        }
    }

    public class GravelLayer : OreLayer
    {
        public GravelLayer(int width, int height, IMineralConfiguration configuration, GravelRepository repository)
            : base(width, height, configuration, repository, MineralNodeGeneratorFactory.None, null)
        {
        }

        public override void AcceptVisitor(MineralLayerVisitor visitor)
        {
            visitor.VisitGravelLayer(this);
        }
    }
}