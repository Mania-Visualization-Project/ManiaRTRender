using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManiaRTRender.Utils
{
    static class CollectionUtils
    {

        public static void AddSome<T>(this ICollection<T> destination, IList<T> source, int start = 0)
        {
            var size = source.Count();
            for (var i = start; i < size; i++) {
                destination.Add(source[i]);
            }
        }

        public static void CopyFrom<T>(this ICollection<T> destination, IList<T> source)
        {
            destination.Clear();
            destination.AddSome(source);
        }

        public static int BinarySearch<E>(this List<E> data, long val, GetValue<E, long> getter)
        {
            var start = 0;
            var end = data.Count - 1;
            if (start > end)
            {
                return start - 1;
            }
            if (val < getter(data[start]))
            {
                return start - 1;
            }
            else if (val > getter(data[end]))
            {
                return end;
            }

            while (start <= end)
            {
                int mid = (start + end) / 2;
                if (getter(data[mid]) > val)
                {
                    end = mid - 1;
                }
                else if (getter(data[mid]) < val)
                {
                    start = mid + 1;
                }
                else
                {
                    return mid - 1;
                }
            }
            return end;
        }

        public delegate V GetValue<T, V>(T obj);

    }
}
