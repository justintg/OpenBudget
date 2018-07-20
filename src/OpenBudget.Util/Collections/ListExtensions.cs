using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Util.Collections
{
    public static class ListExtensions
    {
        public static int BinarySearch<T, TKey>(this List<T> list, Func<T, TKey> selector, TKey item, IComparer<TKey> comparer = null)
        {
            if (comparer == null)
            {
                comparer = Comparer<TKey>.Default;
            }

            int comparisonResult = 0;
            int min = 0;
            int max = list.Count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;

                TKey midKey = selector(list[mid]);
                comparisonResult = comparer.Compare(item, midKey);
                if (comparisonResult == 0)
                {
                    return mid;
                }
                else if (comparisonResult < 0)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }
            
            return ~min;
        }
    }
}
