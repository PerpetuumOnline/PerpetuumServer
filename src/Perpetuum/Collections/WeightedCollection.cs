using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Collections
{
    /// <summary>
    /// Collection that provides random access to stored items where weights bias the probability of selection
    /// </summary>
    /// <typeparam name="T">Any data to be kept in the list</typeparam>
    public class WeightedCollection<T>
    {
        private readonly List<WeightedEntry> _list = new List<WeightedEntry>();
        private int _sumWeights = 0;
        public void Add(T item, int weight = 1)
        {
            _sumWeights += weight;
            _list.Add(new WeightedEntry(item, weight));
        }

        public void Clear()
        {
            _sumWeights = 0;
            _list.Clear();
        }

        public T GetRandom()
        {
            if (_sumWeights == 0)
                return default;

            if (_list.Count == 1)
                return _list.First().Item;

            var weightTarget = FastRandom.NextInt(_sumWeights - 1);
            var current = 0;
            var iterator = _list.GetEnumerator();
            while (iterator.MoveNext())
            {
                current += iterator.Current.Weight;
                if (current > weightTarget)
                    break;
            }
            if (iterator.Current != null)
            {
                return iterator.Current.Item;
            }
            return default;
        }

        private class WeightedEntry
        {
            public T Item { get; private set; }
            public int Weight { get; private set; }
            public WeightedEntry(T item, int weight)
            {
                Item = item;
                Weight = weight;
            }
        }
    }
}
