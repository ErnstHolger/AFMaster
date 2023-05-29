#region using section

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace AFMaster.Util
{
    public static class Extensions
    {
        public static double Median<T>(this IEnumerable<T> source)
        {
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                source = source.Where(x => x != null);

            var count = source.Count();
            if (count == 0)
                return double.NaN;

            source = source.OrderBy(n => n);

            var midpoint = count / 2;
            if (count % 2 == 0)
                return (Convert.ToDouble(source.ElementAt(midpoint - 1)) +
                        Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
            return Convert.ToDouble(source.ElementAt(midpoint));
        }
    }
}