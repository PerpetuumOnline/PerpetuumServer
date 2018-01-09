using System;

namespace Perpetuum.Zones.Terrains
{
    [Serializable]
    public struct BlockingInfo : IEquatable<BlockingInfo>
    {
        public static readonly BlockingInfo None = new BlockingInfo();

        private BlockingFlags _blockingFlags;
        private byte _height;

        public BlockingInfo(BlockingFlags blockingFlags,int height)
        {
            _blockingFlags = blockingFlags;
            _height = (byte) height;
        }

        public BlockingFlags Flags
        {
            get { return _blockingFlags; }
        }

        public int Height
        {
            get { return _height; }
            set { _height = (byte) value; }
        }

        public bool Plant
        {
            get { return HasFlags(BlockingFlags.Plant); }
            set { SetFlags(BlockingFlags.Plant, value); }
        }

        public bool Obstacle
        {
            get { return HasFlags(BlockingFlags.Obstacle); }
            set { SetFlags(BlockingFlags.Obstacle, value); }
        }

        public bool Island
        {
            get { return HasFlags(BlockingFlags.Island); }
            set { SetFlags(BlockingFlags.Island, value); }
        }

        public bool Decor
        {
            get { return HasFlags(BlockingFlags.Decor); }
        }

        public bool NonNaturally
        {
            get { return HasFlags(BlockingFlags.NonNaturally); }
        }

        private bool HasFlags(BlockingFlags flags)
        {
            return ((int)_blockingFlags & ((int)flags)) > 0;
        }

        private void SetFlags(BlockingFlags flags, bool state)
        {
            _blockingFlags = state ? (BlockingFlags)((int)_blockingFlags | (int)flags) : (BlockingFlags)((int)_blockingFlags & ~((int)flags));
        }

        public bool Equals(BlockingInfo other)
        {
            return _blockingFlags == other._blockingFlags && Height == other.Height;
        }
    }
}