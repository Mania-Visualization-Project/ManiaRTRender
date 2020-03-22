using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManiaRTRender
{
    static class Utils
    {

        public static void AddSome<T>(this ICollection<T> destination, IList<T> source, int start = 0)
        {
            int size = source.Count();
            for (int i = start; i < size; i++) {
                destination.Add(source[i]);
            }
        }

        public static void CopyFrom<T>(this ICollection<T> destination, IList<T> source)
        {
            destination.Clear();
            destination.AddSome(source);
        }
    }
}
