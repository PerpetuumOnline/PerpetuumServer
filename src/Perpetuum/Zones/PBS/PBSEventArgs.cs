using System;
using Perpetuum.Units;

namespace Perpetuum.Zones.PBS
{
    public abstract class PBSEventArgs : EventArgs
    {
        public PBSEventType Type { get; private set; }

        protected PBSEventArgs(PBSEventType type)
        {
            Type = type;
        }
    }

    public class NodeAttackedEventArgs : PBSEventArgs
    {
        public Unit Attacker { get; private set; }

        public NodeAttackedEventArgs(Unit attacker) : base(PBSEventType.nodeAttacked)
        {
            Attacker = attacker;
        }
    }
}