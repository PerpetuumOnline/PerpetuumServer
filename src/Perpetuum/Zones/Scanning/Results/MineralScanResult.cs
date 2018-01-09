using System;
using System.Collections.Generic;
using Perpetuum.Zones.Terrains.Materials;

namespace Perpetuum.Zones.Scanning.Results
{
    public class MineralScanResult
    {
        public int Id { private get; set; }
        public DateTime Creation { private get; set; }
        public int ZoneId { get; set; }
        public MaterialType MaterialType { get; set; }
        public Area Area { get; set; }

        public string Folder { private get; set; }
        public bool FoundAny { get; set; }
        public double ScanAccuracy { get; set; }

        private readonly uint[] _scanData;

        public long Quality { get; set; }

        public MineralScanResult()
        {
        }

        public MineralScanResult(uint[] scanData)
        {
            _scanData = scanData;
        }

        public override string ToString()
        {
            return $"Id: {Id}, MaterialType: {MaterialType}, Creation: {Creation}";
        }

        public IDictionary<string,object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                {k.ID,Id}, 
                {k.materialProbeType, (int) MaterialProbeType.Tile}, 
                {k.creation, Creation}, 
                {k.zoneID, ZoneId}, 
                {k.materialType, (int) MaterialType}, 
                {k.area, Area}, 
                {k.scanAccuracy,ScanAccuracy},
                {k.quality,Quality},
                {k.folder, Folder},
            };

            return dictionary;
        }

        public Packet ToPacket()
        {
            var packet = new Packet(ZoneCommand.ScanMineralTileResult);

            packet.AppendInt(ZoneId);
            packet.AppendByte((byte)MaterialType);
            packet.AppendArea(Area);
            packet.AppendDouble(ScanAccuracy);
            packet.AppendUInt64Array(_scanData);

            return packet;            
        }

        public MineralScanResultItem ToItem()
        {
            var item = MineralScanResultItem.Create();

            var probeInfo = new Dictionary<string, object>
            {
                {k.date, Creation},
                {k.area, Area},
                {k.zone, ZoneId},
                {k.mineral, MaterialType.GetName()},
                {k.scanAccuracy, ScanAccuracy},
                {k.type, 1}
            };

            item.DynamicProperties.Update(k.probeInfo, probeInfo);
            return item;
        }
    }
}