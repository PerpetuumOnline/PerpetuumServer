using System;
using System.Collections.Generic;

namespace Perpetuum
{
    public static class LinkedListExtensions
    {
        public static void RemoveAll<T>(this LinkedList<T> list, Func<T, bool> match)
        {
            var node = list.First;
            while (node != null)
            {
                var next = node.Next;

                if (match(node.Value))
                    list.Remove(node);

                node = next;
            }
        }
    }
}
