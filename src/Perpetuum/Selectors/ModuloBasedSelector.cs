using System.Collections.Generic;

namespace Perpetuum.Selectors
{
    public class ModuloBasedSelector<T> : ISelector<T>
    {
        private readonly int _maximumIndex;
        private int _currentIndex;
        private readonly List<T> _list; 

        public ModuloBasedSelector(IEnumerable<T> enumerable)
        {
            _list = new List<T>(enumerable);

            _maximumIndex = _list.Count;
            _currentIndex = 0;
        }

        public T GetNext()
        {
            var index = _currentIndex++ % _maximumIndex;
            return _list[index];
        }
    }
    

}
