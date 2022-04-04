using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureEncoder_WPF
{
    public static class LinqExtensions
    {
        /// <summary>
        /// 去重
        /// </summary>
        public static IEnumerable<TElement> Distinct<TElement, T2>(this IEnumerable<TElement> source, Func<TElement, T2> getter)
        {
            var hashSet = new HashSet<TElement>();
            foreach (var element in source)
            {
                if (hashSet.Add(element)) { yield return element; }
            }
        }
    }
}
