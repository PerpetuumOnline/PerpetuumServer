using System.Diagnostics;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Zones
{
    public class Packet : BinaryStream
    {
        private const int HEADERSIZE = sizeof(byte) + // command
                                       sizeof(int) + // error
                                       sizeof(int) + // counter
                                       sizeof(int); // work time

        public Packet(ZoneCommand command)
        {
            AppendByte((byte)command);
            AppendInt((int)ErrorCodes.NoError);
            AppendInt(-1);
            AppendInt(0);
        }

        public Packet(byte[] byteArray) : base(byteArray)
        {
            Debug.Assert(byteArray.Length >= HEADERSIZE,"invalid packet");
            Reset();
        }

        private void Reset()
        {
            Position = HEADERSIZE;
        }

        public ZoneCommand Command
        {
            [DebuggerStepThrough]
            get { return (ZoneCommand)PeekByte(0); }
        }

        public ErrorCodes Error
        {
            set
            {
                PutInt(1, (int)value);    
            }
        }

        public int WorkTime
        {
            set
            {
                PutInt(9,value);
            }
        }
    }

    //##################################################

    public class CombatLogPacket : Packet
    {
        private CombatLogPacket() : base(ZoneCommand.CombatLog) { }

        public CombatLogPacket(CombatLogType type, Unit target, Unit source = null, Module module = null)
            : this()
        {
            AppendInt((int)type);

            var srcEid = source?.Eid ?? 0L;
            var srcCharacter = source.GetCharacter();

            AppendLong(srcEid);
            AppendInt(srcCharacter.Id);

            AppendLong(target.Eid);
            var targetCharacter = target.GetCharacter();
            AppendInt(targetCharacter.Id);

            if (module != null)
            {
                AppendInt(module.Definition);
                AppendLong(module.Eid);
                AppendByte((byte) module.Slot);
            }
            else
            {
                AppendInt(0);
                AppendLong(0);
                AppendByte(0);
            }
        }

        public CombatLogPacket(ErrorCodes error, Module module, Lock target)
            : this()
        {
            CreatePacketHeader(error, module);
            AppendTarget(target);
        }

        private void AppendTarget(Lock @lock)
        {
            if (@lock == null)
            {
                AppendByte(0xff);
                return;
            }

            if (@lock is UnitLock u)
            {
                AppendByte((byte)LockType.Unit);
                AppendLong(u.Target.Eid);
                return;
            }

            if (@lock is TerrainLock t)
            {
                AppendByte((byte)LockType.Terrain);
                AppendInt(t.Location.intX);
                AppendInt(t.Location.intY);
                AppendInt(t.Location.intZ);
            }
        }

        private void CreatePacketHeader(ErrorCodes errorCode, Module module)
        {
            AppendInt((int)CombatLogType.Error);
            AppendLong(module.Eid);
            AppendByte((byte) module.Slot);
            AppendInt((int)errorCode);
        }

        public void Send(Unit target, Unit otherTarget = null)
        {
            var player = target as Player;
            player?.Session.SendPacket(this);

            if ( target == otherTarget )
                return;

            var otherPlayer = otherTarget as Player;
            otherPlayer?.Session.SendPacket(this);
        }
    }   
}