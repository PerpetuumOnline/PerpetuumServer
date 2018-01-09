using System;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.Scanning.Ammos;

namespace Perpetuum.Zones.Scanning.Scanners
{
    public partial class Scanner : IEntityVisitor<IntrusionScannerAmmo>
    {
        private const double SCAN_INTRUSION_RANDOM_FACTOR = 0.5;
        private const int REINFORCE_SCAN_RANGE = 100;

        public void Visit(IntrusionScannerAmmo ammo)
        {
            var packet = _zone.Configuration.Terraformable ? BuildReinforcePacket() : BuildIntrusionPacket(ammo.ScanRange + 10);
            _player.Session.SendPacket(packet);

            _zone.CreateBeam(BeamType.sap_scanner_beam, builder => builder.WithSource(_player).WithPosition(_player.CurrentPosition).WithDuration(48778));
        }

        private Packet BuildIntrusionPacket(int scanRange)
        {
            var intrusionPacket = new Packet(ZoneCommand.ScanIntrusionSiteResult);

            intrusionPacket.AppendPoint(_player.CurrentPosition);
            intrusionPacket.AppendLong(_module.Eid);

            if (_zone.Configuration.Protected)
            {
                //alfan ne
                intrusionPacket.AppendInt(0); //nulla darab lett talalva
                return intrusionPacket;
            }

            //intrusion
            var outposts = _zone.Units.WithinRange(_player.CurrentPosition, scanRange).OfType<Outpost>().ToArray();
            intrusionPacket.AppendInt(outposts.Length);

            foreach (var outpost in outposts)
            {
                intrusionPacket.AppendInt(outpost.Definition);
                intrusionPacket.AppendLong(outpost.Eid);

                var intrusionStartTime = outpost.GetIntrusionSiteInfo().IntrusionStartTime;
                if (intrusionStartTime == null)
                {
                    intrusionPacket.AppendInt(0);
                    intrusionPacket.AppendInt(0);
                }
                else
                {
                    intrusionPacket.AppendInt(1); //in intrusion
                    var nextIntrusionTime = CalculateTime((DateTime) intrusionStartTime);
                    intrusionPacket.AppendInt((int)nextIntrusionTime.TotalSeconds); // seconds
                }
            }

            return intrusionPacket;
        }

        private Packet BuildReinforcePacket()
        {
            var units = _zone.Units.WithinRange(_player.CurrentPosition, REINFORCE_SCAN_RANGE).ToArray();

            var stream = new BinaryStream();
            var counter = 0;
            foreach (var unit in units)
            {
                var pbsObject = unit as IPBSObject;
                if (pbsObject == null) 
                    continue;

                stream.AppendInt(unit.Definition);
                stream.AppendLong(unit.Eid);
                stream.AppendInt(pbsObject.ReinforceHandler.ReinforceCounter);

                var inReinforce = 0;
                var reinforceTime = TimeSpan.Zero;

                var reinforceEnd = pbsObject.ReinforceHandler.ReinforceEnd;
                if (reinforceEnd != null)
                {
                    inReinforce = 1;
                    reinforceTime = CalculateTime((DateTime) reinforceEnd);
                }

                stream.AppendInt(inReinforce);
                stream.AppendInt((int) reinforceTime.TotalSeconds);

                counter++;
            }

            var reinforcePacket = new Packet(ZoneCommand.ScanReinforceResult);
            reinforcePacket.AppendInt(counter);
            reinforcePacket.AppendStream(stream);

            return reinforcePacket;
        }

        private TimeSpan CalculateTime(DateTime dateTime)
        {
            var mod = GetRandomModifier();
            var result = dateTime - DateTime.Now;
            result = result.Max(TimeSpan.Zero).Multiply(mod);
            return result;
        }

        private double GetRandomModifier()
        {
            return 1.0 - (FastRandom.NextDouble(-0.5, 0.5) * SCAN_INTRUSION_RANDOM_FACTOR * (1.0 - _module.ScanAccuracy.Clamp()));
        }

    }
}
