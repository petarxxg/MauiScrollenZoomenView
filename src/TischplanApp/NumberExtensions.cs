using System;
using System.Collections.Generic;
using System.Linq;

namespace Orderlyze.Foundation.Helper.Extensions
{
    /// <summary>
    /// Extensions for numeric data types
    /// </summary>
    public static class NumberExtensions
    {
        public static double ToDouble(this decimal d)
        {
            return (double)d;
        }
        public static double? ToDouble(this decimal? d)
        {
            if (d.HasValue)
            {
                return (double)d;
            }
            return null;
        }

        /// <summary>
        /// Parses double to decimal
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this double d)
        {
            return (decimal)d;
        }

        /// <summary>
        /// Parses double to float
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static float ToFloat(this double d)
        {
            return (float)d;
        }

        public static uint FindNearestValue(this IEnumerable<uint> source, uint target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            uint nearestValue = source.FirstOrDefault(); // Initialize with the first value in the collection

            foreach (uint value in source)
            {
                if (Math.Abs(nearestValue - target) > Math.Abs(value - target))
                {
                    nearestValue = value;
                }
            }

            return nearestValue;
        }
    }
}
