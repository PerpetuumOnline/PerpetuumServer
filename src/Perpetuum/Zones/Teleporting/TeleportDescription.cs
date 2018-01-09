using System;
using System.Collections.Generic;

namespace Perpetuum.Zones.Teleporting
{
    public class TeleportDescription
    {
        public int id;
        public string description;
        public TeleportDescriptionType descriptionType;

        public IZone SourceZone;
        public Teleport SourceTeleport;
        public int? sourceRange;

        public IZone TargetZone;
        public Teleport TargetTeleport;
        public int targetRange;

        public Position? landingSpot;
        public int _useTimeout;
        public bool listable;
        public bool active = true; //alapbol true

        public Position GetRandomTargetPosition()
        {
            var targetPosition = landingSpot ?? TargetTeleport?.CurrentPosition ?? Position.Empty;
            var targetColumSize = TargetTeleport?.ED.Options.Size * 2 ?? 0;
            var maxRange = Math.Max(targetRange, targetColumSize);
            return targetPosition.GetRandomPositionInRange2D(targetColumSize, maxRange);
        }

        public bool IsValid()
        {
            //turned on?
            if (!listable) 
                return false;

            //is source defined?
            if (SourceTeleport == null) 
                return false;

            //-- columns -- full position has to be defined

            //is source position defined?
            if (SourceZone == null || sourceRange == null) 
                return false;

            //is target position defined?
            if (TargetTeleport != null &&  TargetZone == null) 
                return false;

            if (TargetZone == null) 
                return false;
           
            return true;
        }
    
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.ID, id},
                    {k.description, description},
                    {k.sourceColumn, SourceTeleport?.Eid},
                    {k.targetColumn, TargetTeleport?.Eid},
                    {k.sourceZone,SourceZone?.Id},
                    {k.sourcePosition,SourceTeleport?.CurrentPosition},
                    {k.sourceRange, sourceRange},
                    {k.targetPosition,TargetTeleport?.CurrentPosition},
                    {k.landingSpot, landingSpot },
                    {k.targetRange, targetRange},
                    {k.targetZone,TargetZone?.Id},
                    {k.timeOut, _useTimeout},
                    {k.valid , IsValid()},
                    {k.listable, listable},
                    {k.sourceName, SourceTeleport?.Name},
                    {k.active, active},
                    {k.type,(int)descriptionType}
                };
        }
    }
}