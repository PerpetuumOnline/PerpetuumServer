using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Perpetuum.Data;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class MineralNodeRepository : IMineralNodeRepository
    {
        public static readonly IMineralNodeRepository None = new NullMineralNodeRepository();
        private readonly IZone _zone;
        private readonly MaterialType _materialType;

        public MineralNodeRepository(IZone zone,MaterialType materialType)
        {
            _zone = zone;
            _materialType = materialType;
        }

        public void Insert(MineralNode node)
        {
            var compressedValues = CompressNodeValues(node.Values);

            Db.Query().CommandText("insert into mineralnodes (zoneid,materialtype,x,y,width,height,data) values (@zoneId,@materialType,@x,@y,@width,@height,@data)")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@materialType", _materialType)
                .SetParameter("@x", node.Area.X1)
                .SetParameter("@y", node.Area.Y1)
                .SetParameter("@width", node.Area.Width)
                .SetParameter("@height", node.Area.Height)
                .SetParameter("@data", compressedValues)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Update(MineralNode node)
        {
            var compressedValues = CompressNodeValues(node.Values);

            Db.Query().CommandText("update mineralnodes set data = @data where zoneid = @zoneId and materialtype = @materialType and x = @x and y = @y and width = @width and height = @height")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@materialType", _materialType)
                .SetParameter("@x", node.Area.X1)
                .SetParameter("@y", node.Area.Y1)
                .SetParameter("@width", node.Area.Width)
                .SetParameter("@height", node.Area.Height)
                .SetParameter("@data", compressedValues)
#if RELEASE 
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
#else
                .ExecuteNonQuery();
#endif
        }

        public void Delete(MineralNode node)
        {
            Db.Query().CommandText("delete from mineralnodes where zoneid = @zoneId and materialtype = @materialType and x = @x and y = @y and width = @width and height = @height")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@materialType", _materialType)
                .SetParameter("@x", node.Area.X1)
                .SetParameter("@y", node.Area.Y1)
                .SetParameter("@width", node.Area.Width)
                .SetParameter("@height", node.Area.Height)
#if RELEASE 
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
#else
                .ExecuteNonQuery();
#endif
        }

        public List<MineralNode> GetAll()
        {
            var records = Db.Query().CommandText("select * from mineralnodes where zoneid = @zoneId and materialtype = @materialType")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@materialType", _materialType)
                .Execute();

            var result = new List<MineralNode>();

            foreach (var record in records)
            {
                var x = record.GetValue<int>("x");
                var y = record.GetValue<int>("y");
                var width = record.GetValue<int>("width");
                var height = record.GetValue<int>("height");
                var data = record.GetValue<byte[]>("data");

                var area = Area.FromRectangle(x, y, width, height);
                var decompressed = DecompressNodeValues(data);

                var node = new MineralNode(_materialType,area,decompressed);
                result.Add(node);
            }

            return result;
        }

        private static byte[] CompressNodeValues(uint[] values)
        {
            var buffer = new byte[values.Length*sizeof (float)];
            Buffer.BlockCopy(values,0,buffer,0,buffer.Length);
            
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms,CompressionMode.Compress))
                {
                    gzip.Write(buffer,0,buffer.Length);
                }

                return ms.ToArray();
            }
        }

        private static uint[] DecompressNodeValues(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
                {
                    gzip.CopyTo(ms);
                    var buffer = ms.ToArray();
                    var result = new uint[buffer.Length / sizeof(float)];
                    Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                    return result;
                }
            }
        }

        private class NullMineralNodeRepository : IMineralNodeRepository
        {
            public void Insert(MineralNode node) { }
            public void Update(MineralNode node) { }
            public void Delete(MineralNode node) { }

            public List<MineralNode> GetAll()
            {
                return new List<MineralNode>();
            }
        }
    }
}