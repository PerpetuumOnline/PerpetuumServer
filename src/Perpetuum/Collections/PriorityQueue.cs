using System;
using System.Collections.Generic;

namespace Perpetuum.Collections
{
    public class PriorityQueue<T>
    {
        private readonly int _capacity;
        private int _count;
        private T[] _items;
        private readonly IComparer<T> _comparer;

        public PriorityQueue(int capacity,IComparer<T> comparer = null)
        {
            _capacity = capacity;
            _items = new T[capacity + 1];
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public void Enqueue(T item)
        {
            if (_count >= _items.Length - 1)
            {
                Array.Resize(ref _items, _items.Length + (_capacity / 2));
            }

            _count++;
            _items[_count] = item;

            var m = _count;

            while (m > 1)
            {
                var parentIndex = m / 2;

                var parentItem = _items[parentIndex];
                var currentItem = _items[m];

                if (_comparer.Compare(currentItem, parentItem) >= 0)
                    break;

                _items[parentIndex] = currentItem;
                _items[m] = parentItem;
                m = parentIndex;
            }
        }

        public bool TryDequeue(out T item)
        {
            if ( _count < 1)
            {
                item = default(T);
                return false;
            }

            item = _items[1];
            _items[1] = _items[_count];

            _count--;

            if (_count == 0)
                return true;

            var v = 1;

            while (true)
            {
                var u = v;

                if ( (2 * u + 1) <= _count)
                {
                    if ( _comparer.Compare(_items[u],_items[2 * u]) >= 0 )
                    {
                        v = 2*u;
                    }

                    if ( _comparer.Compare(_items[v],_items[2 * u + 1]) >= 0 )
                    {
                        v = 2*u + 1;
                    }
                }
                else if ( 2 * u <= _count )
                {
                    if ( _comparer.Compare(_items[u],_items[2 * u]) >= 0 )
                    {
                        v = 2*u;
                    }
                }

                if (u == v)
                    break;

                var tmp = _items[u];
                _items[u] = _items[v];
                _items[v] = tmp;
            }
         
            return true;
        }
    }
}