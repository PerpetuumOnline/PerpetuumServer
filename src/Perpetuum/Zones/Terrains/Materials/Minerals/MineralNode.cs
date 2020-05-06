using System;
using System.Diagnostics;
using System.Drawing;
using Perpetuum.Threading;
using Perpetuum.Timers;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class MineralNode
    {
        public MaterialType Type { get; private set; }

        public delegate void MineralNodeEventHandler(MineralNode node);

        private readonly Area _area;
// ReSharper disable once FieldCanBeMadeReadOnly.Local
        private uint[] _values; // the cake is a lie!

        public MineralNode(MaterialType type,Area area)
        {
            Type = type;
            _area = area;
            _values = new uint[area.Ground];
            Expirable = true;
        }

        public MineralNode(MaterialType type,Area area,uint[] values) : this(type,area)
        {
            Debug.Assert(values.Length == area.Ground);
            InitValus(values);
        }

        public bool Expirable { private get; set; }

        private void InitValus(uint[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                _values[i] = values[i];
            }
        }

        public Area Area
        {
            get { return _area; }
        }

        public uint[] Values
        {
            get { return _values; }
        }

        public event MineralNodeEventHandler Decrease;

        private void OnDecrease()
        {
            Decrease?.Invoke(this);
        }

        public event MineralNodeEventHandler Updated;

        private void OnUpdated()
        {
            Updated?.Invoke(this);
        }

        public event MineralNodeEventHandler Expired;

        private void OnExpired()
        {
            if ( _expired )
                return;

            _expired = true;

            Expired?.Invoke(this);
        }

        private int GetOffset(int x, int y)
        {
            Debug.Assert(_area.Contains(x,y),"invalid xy");
            
            var vx = x - _area.X1;
            var vy = y - _area.Y1;
            var offset = vy * _area.Width + vx;
            return offset;
        }

        public bool HasValue(Point p)
        {
            return HasValue(p.X, p.Y);
        }

        public bool HasValue(int x, int y)
        {
            var v = GetValue(x, y);
            return v > 0;
        }

        public uint DecreaseValue(Point p, uint value)
        {
            OnDecrease();
            return DecreaseValue(p.X, p.Y, value);
        }

        private bool _updated;

        private uint DecreaseValue(int x, int y, uint value)
        {
            if (!_area.Contains(x, y))
                return 0;

            var offset = GetOffset(x, y);

            uint amount = 0;
            LockFree.Update(ref _values[offset], current =>
            {
                amount = Math.Min(current, value);
                return current - amount;
            });

            _updated = true;
            _expiry.Reset();

            var maxAmount = GetMaxAmount();
            if (maxAmount <= 0)
                OnExpired();

            return amount;
        }

        public uint GetMaxAmount()
        {
            uint max = 0;
            for (var i = 0; i < _values.Length; i++)
            {
                if (_values[i] > max)
                    max = _values[i];
            }
            return max;
        }

        public ulong GetTotalAmount()
        {
            ulong sum = 0;
            for (var i = 0; i < _values.Length; i++)
            {
                sum += _values[i];
            }
            return sum;
        }

        public uint GetValue(Point p)
        {
            return GetValue(p.X, p.Y);
        }

        public uint GetValue(int x, int y)
        {
            if (!_area.Contains(x, y))
                return 0;

            var offset = GetOffset(x,y);

            var value = _values[offset];
            return value;
        }

        public void SetValue(Point p,uint value)
        {
            SetValue(p.X,p.Y,value);
        }

        private void SetValue(int x, int y,uint value)
        {
            if (!_area.Contains(x, y))
                return;

            var offset = GetOffset(x,y);
            _values[offset] = value;
        }

        private bool _expired;

        private readonly TimeTracker _expiry = new TimeTracker(TimeSpan.FromDays(7));
        private readonly IntervalTimer _saveTimer = new IntervalTimer(TimeSpan.FromMinutes(30));

        public void Update(TimeSpan time)
        {
            if (Expirable)
            {
                if (_expired)
                    return;

                _expiry.Update(time);

                if (_expiry.Expired)
                    OnExpired();
            }

            if (!_updated)
                return;

            _saveTimer.Update(time);

            if (!_saveTimer.Passed)
                return;

            _saveTimer.Reset();

            _updated = false;
            OnUpdated();
        }

        public Point GetNearestMineralPosition(Point p)
        {
            var nearest = Point.Empty;
            var nearestDist = int.MaxValue;

            var offset = 0;
            for (var ay = _area.Y1; ay <= _area.Y2; ay++)
            {
                for (var ax = _area.X1; ax <= _area.X2; ax++)
                {
                    var v = _values[offset++];
                    if (v <= 0)
                        continue;

                    var d =  p.SqrDistance(ax,ay);
                    if (d >= nearestDist)
                        continue;

                    nearest = new Point(ax,ay);
                    nearestDist = d;
                }
            }

            return nearest;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MineralNode);
        }

        public bool Equals(MineralNode other)
        {
            return other.Type == Type && other.Area == _area;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + Type.GetHashCode();
                hash = hash * 31 + Area.GetHashCode();
                return hash;
            }
        }
    }
}